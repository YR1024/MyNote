using Microsoft.EntityFrameworkCore;
using System.Text.Json;

public sealed class AiSubtitleOptions
{
    public bool Enabled { get; init; }
    public string TargetLanguage { get; init; } = "zh-CN";
    public int TranslationChunkSize { get; init; } = 30;
    public int MaxInputCharacters { get; init; } = 300_000;
    public bool PreserveExplicitLanguage { get; init; } = true;
    public string TranslationStyle { get; init; } = "忠实、自然、结合上下文，不删减或弱化原文表达";
    public AiProviderOptions Speech { get; init; } = new();
    public AiProviderOptions Translation { get; init; } = new();

    public static AiSubtitleOptions FromConfiguration(IConfiguration configuration)
    {
        var section = configuration.GetSection("AiSubtitles");
        return new AiSubtitleOptions
        {
            Enabled = section.GetValue("Enabled", false),
            TargetLanguage = section["TargetLanguage"]?.Trim() is { Length: > 0 } target ? target : "zh-CN",
            TranslationChunkSize = Math.Clamp(section.GetValue("TranslationChunkSize", 30), 5, 100),
            MaxInputCharacters = Math.Clamp(section.GetValue("MaxInputCharacters", 300_000), 1_000, 2_000_000),
            PreserveExplicitLanguage = section.GetValue("PreserveExplicitLanguage", true),
            TranslationStyle = section["TranslationStyle"]?.Trim() is { Length: > 0 } style
                ? style
                : "忠实、自然、结合上下文，不删减或弱化原文表达",
            Speech = AiProviderOptions.FromConfiguration(section.GetSection("Speech")),
            Translation = AiProviderOptions.FromConfiguration(section.GetSection("Translation"))
        };
    }
}

public sealed class AiProviderOptions
{
    public string Provider { get; init; } = "Disabled";
    public string BaseUrl { get; init; } = "";
    public string Model { get; init; } = "";

    public static AiProviderOptions FromConfiguration(IConfiguration section) => new()
    {
        Provider = section["Provider"]?.Trim() is { Length: > 0 } provider ? provider : "Disabled",
        BaseUrl = section["BaseUrl"]?.Trim() ?? "",
        Model = section["Model"]?.Trim() ?? ""
    };
}

public interface IAiProvider
{
    string Name { get; }
    string Model { get; }
    bool IsAvailable { get; }
    bool IsMock { get; }
}

public interface ISpeechRecognitionProvider : IAiProvider
{
    Task<SpeechRecognitionOutput> TranscribeAsync(SpeechRecognitionRequest request, CancellationToken cancellationToken);
}

public interface ISubtitleTranslationProvider : IAiProvider
{
    Task<TranslationOutput> TranslateAsync(TranslationRequest request, CancellationToken cancellationToken);
}

public sealed class MockSpeechRecognitionProvider(AiSubtitleOptions options) : ISpeechRecognitionProvider
{
    public string Name => "Mock";
    public string Model => options.Speech.Model;
    public bool IsAvailable => true;
    public bool IsMock => true;

    public async Task<SpeechRecognitionOutput> TranscribeAsync(
        SpeechRecognitionRequest request,
        CancellationToken cancellationToken)
    {
        await Task.Delay(250, cancellationToken);
        var durationMilliseconds = Math.Max(6_000L, (long)Math.Round((request.DurationSeconds ?? 8) * 1000));
        var texts = new[]
        {
            "This is a simulated speech recognition result.",
            "It verifies the background subtitle workflow.",
            "The real provider will run on the home computer."
        };
        var cues = texts.Select((text, index) =>
        {
            var start = durationMilliseconds * index / texts.Length;
            var end = durationMilliseconds * (index + 1) / texts.Length;
            return new RecognizedCue(index, start, Math.Max(start + 500, end), text);
        }).ToList();
        return new SpeechRecognitionOutput("en", cues, cues.Sum(cue => cue.Text.Length));
    }
}

public sealed class MockSubtitleTranslationProvider(AiSubtitleOptions options) : ISubtitleTranslationProvider
{
    public string Name => "Mock";
    public string Model => options.Translation.Model;
    public bool IsAvailable => true;
    public bool IsMock => true;

    public async Task<TranslationOutput> TranslateAsync(
        TranslationRequest request,
        CancellationToken cancellationToken)
    {
        await Task.Delay(150, cancellationToken);
        var cues = request.Cues.Select(cue => new TranslatedCue(cue.CueId, TranslateFixture(cue.Text))).ToList();
        return new TranslationOutput(cues, request.Cues.Sum(cue => cue.Text.Length), cues.Sum(cue => cue.Text.Length));
    }

    private static string TranslateFixture(string text) => text.Trim() switch
    {
        "Hello from Dreamy Cinema." => "你好，这里是 Dreamy Cinema。",
        "Subtitle import is working." => "字幕导入功能运行正常。",
        _ => $"【模拟译文】{text.Trim()}"
    };
}

public sealed class UnavailableSpeechRecognitionProvider(AiProviderOptions options) : ISpeechRecognitionProvider
{
    public string Name => options.Provider;
    public string Model => options.Model;
    public bool IsAvailable => false;
    public bool IsMock => false;
    public Task<SpeechRecognitionOutput> TranscribeAsync(SpeechRecognitionRequest request, CancellationToken cancellationToken) =>
        throw new InvalidOperationException($"语音识别 Provider '{Name}' 尚未接入。");
}

public sealed class UnavailableSubtitleTranslationProvider(AiProviderOptions options) : ISubtitleTranslationProvider
{
    public string Name => options.Provider;
    public string Model => options.Model;
    public bool IsAvailable => false;
    public bool IsMock => false;
    public Task<TranslationOutput> TranslateAsync(TranslationRequest request, CancellationToken cancellationToken) =>
        throw new InvalidOperationException($"字幕翻译 Provider '{Name}' 尚未接入。");
}

public static class AiSubtitleProviderFactory
{
    public static ISpeechRecognitionProvider CreateSpeech(AiSubtitleOptions options) =>
        options.Speech.Provider.Equals("Mock", StringComparison.OrdinalIgnoreCase)
            ? new MockSpeechRecognitionProvider(options)
            : new UnavailableSpeechRecognitionProvider(options.Speech);

    public static ISubtitleTranslationProvider CreateTranslation(AiSubtitleOptions options) =>
        options.Translation.Provider.Equals("Mock", StringComparison.OrdinalIgnoreCase)
            ? new MockSubtitleTranslationProvider(options)
            : new UnavailableSubtitleTranslationProvider(options.Translation);
}

public sealed class AiSubtitlePipeline(
    CinemaDbContext db,
    VideoStorage storage,
    AiSubtitleOptions options,
    ISpeechRecognitionProvider speechProvider,
    ISubtitleTranslationProvider translationProvider)
{
    public async Task<string> RunSpeechRecognitionAsync(
        string jobId,
        CancellationToken cancellationToken,
        Func<JobProgress, Task> reportProgress)
    {
        EnsureEnabled(speechProvider, "语音识别");
        var job = await db.MediaJobs.AsNoTracking().FirstAsync(item => item.Id == jobId, cancellationToken);
        var video = await db.Videos.AsNoTracking().FirstAsync(item => item.Id == job.VideoId, cancellationToken);
        var requestedLanguage = GetSpeechLanguage(job.InputJson);
        var sourceKey = $"ai:speech:{video.Id}:{requestedLanguage ?? "auto"}";
        var existingTrack = await FindGeneratedTrackAsync(video.Id, sourceKey, cancellationToken);
        if (existingTrack is not null)
        {
            return SerializeResult(existingTrack, speechProvider, false);
        }

        await reportProgress(new JobProgress(5, "准备语音识别", video.OriginalFileName));
        var checkpoint = await ReadCheckpointAsync<SpeechRecognitionOutput>(jobId, 0, cancellationToken);
        if (checkpoint is null)
        {
            var videoPath = storage.GetAbsolutePath(video.RelativePath);
            checkpoint = await speechProvider.TranscribeAsync(
                new SpeechRecognitionRequest(videoPath, video.DurationSeconds, requestedLanguage),
                cancellationToken);
            ValidateRecognizedCues(checkpoint.Cues);
            await SaveCheckpointAsync(jobId, 0, "SpeechRecognition", checkpoint, checkpoint.InputUnits, checkpoint.Cues.Sum(cue => cue.Text.Length), cancellationToken);
        }

        await reportProgress(new JobProgress(80, "保存原文字幕", $"{checkpoint.Cues.Count} 条"));
        var track = await CreateTrackAsync(
            video.Id,
            sourceKey,
            "AI 原文",
            checkpoint.Language,
            SubtitleKind.Original,
            SubtitleSource.SpeechRecognition,
            checkpoint.Cues.Select(cue => new NewCue(cue.Index, cue.StartMilliseconds, cue.EndMilliseconds, cue.Text)).ToList(),
            cancellationToken);
        return SerializeResult(track, speechProvider, false);
    }

    public async Task<string> RunSubtitleTranslationAsync(
        string jobId,
        CancellationToken cancellationToken,
        Func<JobProgress, Task> reportProgress)
    {
        EnsureEnabled(translationProvider, "字幕翻译");
        var job = await db.MediaJobs.AsNoTracking().FirstAsync(item => item.Id == jobId, cancellationToken);
        var input = DeserializeJobInput<TranslationJobInput>(job.InputJson);
        var sourceTrack = await db.SubtitleTracks
            .AsNoTracking()
            .Include(track => track.Cues)
            .FirstAsync(track => track.Id == input.SourceTrackId && track.VideoId == job.VideoId, cancellationToken);
        var sourceKey = $"ai:translation:{sourceTrack.Id}:{input.TargetLanguage}";
        var existingTrack = await FindGeneratedTrackAsync(sourceTrack.VideoId, sourceKey, cancellationToken);
        if (existingTrack is not null)
        {
            return SerializeResult(existingTrack, translationProvider, true);
        }

        var sourceCues = sourceTrack.Cues.OrderBy(cue => cue.Index).ToList();
        var inputCharacters = sourceCues.Sum(cue => cue.Text.Length);
        if (inputCharacters > options.MaxInputCharacters)
        {
            throw new InvalidOperationException($"字幕共 {inputCharacters} 个字符，超过单任务上限 {options.MaxInputCharacters}。");
        }

        var chunks = sourceCues.Chunk(options.TranslationChunkSize).ToList();
        var translated = new Dictionary<long, string>();
        var previousContext = "";
        for (var index = 0; index < chunks.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var chunk = chunks[index];
            var checkpoint = await ReadCheckpointAsync<TranslationOutput>(jobId, index, cancellationToken);
            if (checkpoint is null)
            {
                var request = new TranslationRequest(
                    sourceTrack.Language,
                    options.TargetLanguage,
                    options.TranslationStyle,
                    options.PreserveExplicitLanguage,
                    previousContext,
                    chunk.Select(cue => new TranslationCue(cue.Id, cue.Index, cue.Text)).ToList());
                checkpoint = await translationProvider.TranslateAsync(request, cancellationToken);
                ValidateTranslation(chunk, checkpoint.Cues);
                await SaveCheckpointAsync(jobId, index, "Translation", checkpoint, checkpoint.InputUnits, checkpoint.OutputUnits, cancellationToken);
            }
            else
            {
                ValidateTranslation(chunk, checkpoint.Cues);
            }

            foreach (var cue in checkpoint.Cues)
            {
                translated[cue.CueId] = cue.Text;
            }
            previousContext = string.Join('\n', checkpoint.Cues.TakeLast(8).Select(cue => cue.Text));
            var percent = 10 + (int)Math.Round(75d * (index + 1) / Math.Max(1, chunks.Count));
            await reportProgress(new JobProgress(percent, "翻译并校验字幕", $"第 {index + 1} / {chunks.Count} 块"));
        }

        var newCues = sourceCues.Select(cue => new NewCue(
            cue.Index,
            cue.StartMilliseconds,
            cue.EndMilliseconds,
            translated[cue.Id])).ToList();
        await reportProgress(new JobProgress(90, "保存中文字幕", $"{newCues.Count} 条"));
        var track = await CreateTrackAsync(
            sourceTrack.VideoId,
            sourceKey,
            "中文（AI）",
            options.TargetLanguage,
            SubtitleKind.Translated,
            SubtitleSource.AiTranslation,
            newCues,
            cancellationToken);
        return SerializeResult(track, translationProvider, true);
    }

    private void EnsureEnabled(IAiProvider provider, string operation)
    {
        if (!options.Enabled)
        {
            throw new InvalidOperationException("AI 字幕功能尚未启用。");
        }
        if (!provider.IsAvailable)
        {
            throw new InvalidOperationException($"{operation} Provider '{provider.Name}' 不可用。");
        }
    }

    private async Task<SubtitleTrack?> FindGeneratedTrackAsync(string videoId, string sourceKey, CancellationToken cancellationToken) =>
        await db.SubtitleTracks.AsNoTracking().FirstOrDefaultAsync(
            track => track.VideoId == videoId && track.SourceKey == sourceKey,
            cancellationToken);

    private async Task<SubtitleTrack> CreateTrackAsync(
        string videoId,
        string sourceKey,
        string label,
        string language,
        string kind,
        string source,
        IReadOnlyList<NewCue> cues,
        CancellationToken cancellationToken)
    {
        var now = DateTime.Now;
        var track = new SubtitleTrack
        {
            Id = Guid.NewGuid().ToString("N"),
            VideoId = videoId,
            Label = label,
            Language = language,
            Kind = kind,
            Source = source,
            SourceKey = sourceKey,
            Format = "generated",
            OriginalRelativePath = "",
            IsDefault = !await db.SubtitleTracks.AnyAsync(item => item.VideoId == videoId, cancellationToken),
            CueCount = cues.Count,
            CreatedAt = now,
            UpdatedAt = now
        };
        track.Cues = cues.Select(cue => new SubtitleCue
        {
            TrackId = track.Id,
            Index = cue.Index,
            StartMilliseconds = cue.StartMilliseconds,
            EndMilliseconds = cue.EndMilliseconds,
            Text = cue.Text
        }).ToList();
        db.SubtitleTracks.Add(track);
        await db.SaveChangesAsync(cancellationToken);
        return track;
    }

    private async Task<T?> ReadCheckpointAsync<T>(string jobId, int index, CancellationToken cancellationToken)
    {
        var chunk = await db.AiJobChunks.AsNoTracking().FirstOrDefaultAsync(
            item => item.JobId == jobId && item.Index == index && item.Status == AiJobChunkStatus.Completed,
            cancellationToken);
        return chunk?.OutputJson is { Length: > 0 } json
            ? JsonSerializer.Deserialize<T>(json, JsonSerializerOptions.Web)
            : default;
    }

    private async Task SaveCheckpointAsync<T>(
        string jobId,
        int index,
        string kind,
        T output,
        int inputUnits,
        int outputUnits,
        CancellationToken cancellationToken)
    {
        var chunk = await db.AiJobChunks.FirstOrDefaultAsync(
            item => item.JobId == jobId && item.Index == index,
            cancellationToken);
        var now = DateTime.Now;
        if (chunk is null)
        {
            chunk = new AiJobChunk
            {
                Id = Guid.NewGuid().ToString("N"),
                JobId = jobId,
                Index = index,
                Kind = kind,
                CreatedAt = now
            };
            db.AiJobChunks.Add(chunk);
        }
        chunk.Status = AiJobChunkStatus.Completed;
        chunk.OutputJson = JsonSerializer.Serialize(output, JsonSerializerOptions.Web);
        chunk.InputUnits = inputUnits;
        chunk.OutputUnits = outputUnits;
        chunk.AttemptCount++;
        chunk.Error = null;
        chunk.UpdatedAt = now;
        chunk.CompletedAt = now;
        await db.SaveChangesAsync(cancellationToken);
    }

    private static void ValidateRecognizedCues(IReadOnlyList<RecognizedCue> cues)
    {
        if (cues.Count == 0 || cues.Any(cue => cue.EndMilliseconds <= cue.StartMilliseconds || string.IsNullOrWhiteSpace(cue.Text)))
        {
            throw new InvalidOperationException("语音识别 Provider 返回了无效的字幕时间轴或空文本。");
        }
    }

    private static void ValidateTranslation(IReadOnlyList<SubtitleCue> source, IReadOnlyList<TranslatedCue> translated)
    {
        var expectedIds = source.Select(cue => cue.Id).Order().ToArray();
        var actualIds = translated.Select(cue => cue.CueId).Order().ToArray();
        if (!expectedIds.SequenceEqual(actualIds)
            || translated.Select(cue => cue.CueId).Distinct().Count() != translated.Count
            || translated.Any(cue => string.IsNullOrWhiteSpace(cue.Text)))
        {
            throw new InvalidOperationException("翻译结果存在 Cue ID 缺失、重复、错位或空译文，已拒绝保存。");
        }
    }

    private static string? GetSpeechLanguage(string? inputJson) =>
        string.IsNullOrWhiteSpace(inputJson) ? null : DeserializeJobInput<SpeechJobInput>(inputJson).Language;

    private static T DeserializeJobInput<T>(string? json) where T : class =>
        string.IsNullOrWhiteSpace(json)
            ? throw new InvalidOperationException("AI 任务缺少输入参数。")
            : JsonSerializer.Deserialize<T>(json, JsonSerializerOptions.Web)
                ?? throw new InvalidOperationException("AI 任务输入参数无效。");

    private static string SerializeResult(SubtitleTrack track, IAiProvider provider, bool translated) =>
        JsonSerializer.Serialize(new AiSubtitleJobResult(
            track.Id,
            track.VideoId,
            track.Language,
            track.CueCount,
            provider.Name,
            provider.Model,
            provider.IsMock,
            translated), JsonSerializerOptions.Web);

    private sealed record NewCue(int Index, long StartMilliseconds, long EndMilliseconds, string Text);
}

public sealed record SpeechRecognitionRequest(string VideoPath, double? DurationSeconds, string? Language);
public sealed record RecognizedCue(int Index, long StartMilliseconds, long EndMilliseconds, string Text);
public sealed record SpeechRecognitionOutput(string Language, IReadOnlyList<RecognizedCue> Cues, int InputUnits);
public sealed record TranslationCue(long CueId, int Index, string Text);
public sealed record TranslationRequest(
    string SourceLanguage,
    string TargetLanguage,
    string Style,
    bool PreserveExplicitLanguage,
    string PreviousContext,
    IReadOnlyList<TranslationCue> Cues);
public sealed record TranslatedCue(long CueId, string Text);
public sealed record TranslationOutput(IReadOnlyList<TranslatedCue> Cues, int InputUnits, int OutputUnits);
public sealed record SpeechJobInput(string? Language);
public sealed record TranslationJobInput(string SourceTrackId, string TargetLanguage);
public sealed record AiSubtitleJobResult(
    string TrackId,
    string VideoId,
    string Language,
    int CueCount,
    string Provider,
    string Model,
    bool Mock,
    bool Translated);
public sealed record AiSubtitleStatus(
    bool Enabled,
    string TargetLanguage,
    AiProviderStatus Speech,
    AiProviderStatus Translation,
    bool PreserveExplicitLanguage,
    string TranslationStyle);
public sealed record AiProviderStatus(string Provider, string Model, bool Available, bool Mock);
