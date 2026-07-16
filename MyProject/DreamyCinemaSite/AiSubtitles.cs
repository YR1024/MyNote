using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using static AiHttp;

public sealed class AiSubtitleOptions
{
    public bool Enabled { get; init; }
    public string TargetLanguage { get; init; } = "zh-CN";
    public int TranslationChunkSize { get; init; } = 12;
    public int SpeechChunkSeconds { get; init; } = 300;
    public int ContextCueCount { get; init; } = 8;
    public int ProviderMaxAttempts { get; init; } = 3;
    public int MaxInputCharacters { get; init; } = 300_000;
    public int MaxTotalTokens { get; init; } = 200_000;
    public int MaxOutputTokensPerChunk { get; init; } = 1_536;
    public bool SemanticReviewEnabled { get; init; } = true;
    public int SemanticReviewMaxTokens { get; init; } = 384;
    public double TranslationTemperature { get; init; }
    public bool PreserveExplicitLanguage { get; init; } = true;
    public string TranslationStyle { get; init; } = "忠实、自然、结合上下文，不删减或弱化原文表达";
    public string AudioWorkingPath { get; init; } = "";
    public IReadOnlyList<string> CharacterNames { get; init; } = [];
    public IReadOnlyDictionary<string, string> Glossary { get; init; } = new Dictionary<string, string>();
    public AiProviderOptions Speech { get; init; } = new();
    public AiProviderOptions Translation { get; init; } = new();

    public static AiSubtitleOptions FromConfiguration(IConfiguration configuration, string contentRootPath)
    {
        var section = configuration.GetSection("AiSubtitles");
        var configuredWorkingPath = section["AudioWorkingPath"]?.Trim();
        var audioWorkingPath = string.IsNullOrWhiteSpace(configuredWorkingPath)
            ? Path.Combine(Path.GetTempPath(), "DreamyCinema", "ai-audio")
            : Path.IsPathRooted(configuredWorkingPath)
                ? Path.GetFullPath(configuredWorkingPath)
                : Path.GetFullPath(Path.Combine(contentRootPath, configuredWorkingPath));

        var characterNames = section.GetSection("CharacterNames").Get<string[]>() ?? [];
        var glossary = section.GetSection("Glossary").Get<Dictionary<string, string>>() ?? new();
        return new AiSubtitleOptions
        {
            Enabled = section.GetValue("Enabled", false),
            TargetLanguage = section["TargetLanguage"]?.Trim() is { Length: > 0 } target ? target : "zh-CN",
            TranslationChunkSize = Math.Clamp(section.GetValue("TranslationChunkSize", 12), 5, 100),
            SpeechChunkSeconds = Math.Clamp(section.GetValue("SpeechChunkSeconds", 300), 30, 1_800),
            ContextCueCount = Math.Clamp(section.GetValue("ContextCueCount", 8), 1, 30),
            ProviderMaxAttempts = Math.Clamp(section.GetValue("ProviderMaxAttempts", 3), 1, 6),
            MaxInputCharacters = Math.Clamp(section.GetValue("MaxInputCharacters", 300_000), 1_000, 2_000_000),
            MaxTotalTokens = Math.Clamp(section.GetValue("MaxTotalTokens", 200_000), 1_000, 2_000_000),
            MaxOutputTokensPerChunk = Math.Clamp(section.GetValue("MaxOutputTokensPerChunk", 1_536), 256, 8_192),
            SemanticReviewEnabled = section.GetValue("SemanticReviewEnabled", true),
            SemanticReviewMaxTokens = Math.Clamp(section.GetValue("SemanticReviewMaxTokens", 384), 128, 2_048),
            TranslationTemperature = Math.Clamp(section.GetValue("TranslationTemperature", 0d), 0d, 1d),
            PreserveExplicitLanguage = section.GetValue("PreserveExplicitLanguage", true),
            TranslationStyle = section["TranslationStyle"]?.Trim() is { Length: > 0 } style
                ? style
                : "忠实、自然、结合上下文，不删减或弱化原文表达",
            AudioWorkingPath = audioWorkingPath,
            CharacterNames = characterNames
                .Select(value => value.Trim())
                .Where(value => value.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(200)
                .ToArray(),
            Glossary = glossary
                .Where(pair => !string.IsNullOrWhiteSpace(pair.Key) && !string.IsNullOrWhiteSpace(pair.Value))
                .Take(500)
                .ToDictionary(pair => pair.Key.Trim(), pair => pair.Value.Trim(), StringComparer.OrdinalIgnoreCase),
            Speech = AiProviderOptions.FromConfiguration(section.GetSection("Speech"), defaultTimeoutSeconds: 1_800),
            Translation = AiProviderOptions.FromConfiguration(section.GetSection("Translation"), defaultTimeoutSeconds: 600)
        };
    }

    public TranslationProfile CreateTranslationProfile() => new(
        TranslationStyle,
        PreserveExplicitLanguage,
        CharacterNames,
        Glossary);
}

public sealed class AiProviderOptions
{
    public string Provider { get; init; } = "Disabled";
    public string BaseUrl { get; init; } = "";
    public string Model { get; init; } = "";
    public string ApiKey { get; init; } = "";
    public int TimeoutSeconds { get; init; } = 600;

    public static AiProviderOptions FromConfiguration(IConfiguration section, int defaultTimeoutSeconds) => new()
    {
        Provider = section["Provider"]?.Trim() is { Length: > 0 } provider ? provider : "Disabled",
        BaseUrl = section["BaseUrl"]?.Trim() ?? "",
        Model = section["Model"]?.Trim() ?? "",
        ApiKey = section["ApiKey"]?.Trim() ?? "",
        TimeoutSeconds = Math.Clamp(section.GetValue("TimeoutSeconds", defaultTimeoutSeconds), 10, 7_200)
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
        return new SpeechRecognitionOutput("en", cues, Math.Max(1, (int)Math.Ceiling(request.DurationSeconds ?? 8)));
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

public sealed class FasterWhisperSpeechRecognitionProvider : ISpeechRecognitionProvider
{
    private readonly AiProviderOptions options;
    private readonly HttpClient client;
    private readonly Uri? endpoint;

    public FasterWhisperSpeechRecognitionProvider(AiProviderOptions options)
    {
        this.options = options;
        client = CreateHttpClient(options);
        endpoint = TryBuildLoopbackEndpoint(options.BaseUrl, "audio/transcriptions");
    }

    public string Name => "FasterWhisper";
    public string Model => options.Model;
    public bool IsAvailable => endpoint is not null && !string.IsNullOrWhiteSpace(Model);
    public bool IsMock => false;

    public async Task<SpeechRecognitionOutput> TranscribeAsync(
        SpeechRecognitionRequest request,
        CancellationToken cancellationToken)
    {
        if (!IsAvailable)
        {
            throw new InvalidOperationException("faster-whisper 配置无效；地址必须是 loopback HTTP 地址且模型不能为空。");
        }

        await using var file = new FileStream(
            request.AudioPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            128 * 1024,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        using var form = new MultipartFormDataContent();
        using var fileContent = new StreamContent(file);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
        form.Add(fileContent, "file", Path.GetFileName(request.AudioPath));
        form.Add(new StringContent(Model), "model");
        form.Add(new StringContent("verbose_json"), "response_format");
        form.Add(new StringContent("segment"), "timestamp_granularities[]");
        form.Add(new StringContent(request.ReleaseModelAfterRequest ? "true" : "false"), "release_model");
        if (!string.IsNullOrWhiteSpace(request.Language))
        {
            form.Add(new StringContent(request.Language), "language");
        }

        using var message = new HttpRequestMessage(HttpMethod.Post, endpoint) { Content = form };
        AddAuthorization(message, options.ApiKey);
        using var response = await SendAsync(client, message, options.TimeoutSeconds, "faster-whisper", cancellationToken);
        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken);
        var root = document.RootElement;
        var language = root.TryGetProperty("language", out var languageElement)
            ? languageElement.GetString()?.Trim() ?? request.Language ?? "und"
            : request.Language ?? "und";
        if (!root.TryGetProperty("segments", out var segments) || segments.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("faster-whisper 返回缺少 segments 数组。");
        }

        var cues = new List<RecognizedCue>();
        foreach (var segment in segments.EnumerateArray())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!TryReadFiniteDouble(segment, "start", out var start)
                || !TryReadFiniteDouble(segment, "end", out var end))
            {
                throw new InvalidOperationException("faster-whisper 返回了无效的分段时间戳。");
            }
            var text = segment.TryGetProperty("text", out var textElement) ? textElement.GetString()?.Trim() : null;
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }
            cues.Add(new RecognizedCue(
                cues.Count,
                (long)Math.Round(start * 1000),
                (long)Math.Round(end * 1000),
                text));
        }

        return new SpeechRecognitionOutput(
            language,
            cues,
            Math.Max(1, (int)Math.Ceiling(request.DurationSeconds ?? 1)));
    }

    private static bool TryReadFiniteDouble(JsonElement element, string name, out double value)
    {
        value = 0;
        return element.TryGetProperty(name, out var property)
            && property.TryGetDouble(out value)
            && double.IsFinite(value);
    }
}

public sealed class LlamaCppSubtitleTranslationProvider : ISubtitleTranslationProvider
{
    private static readonly JsonSerializerOptions PromptJsonOptions = new(JsonSerializerOptions.Web)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
    private readonly AiProviderOptions options;
    private readonly AiSubtitleOptions aiOptions;
    private readonly HttpClient client;
    private readonly Uri? endpoint;

    public LlamaCppSubtitleTranslationProvider(AiProviderOptions options, AiSubtitleOptions aiOptions)
    {
        this.options = options;
        this.aiOptions = aiOptions;
        client = CreateHttpClient(options);
        endpoint = TryBuildLoopbackEndpoint(options.BaseUrl, "chat/completions");
    }

    public string Name => "LlamaCpp";
    public string Model => options.Model;
    public bool IsAvailable => endpoint is not null && !string.IsNullOrWhiteSpace(Model);
    public bool IsMock => false;

    public async Task<TranslationOutput> TranslateAsync(
        TranslationRequest request,
        CancellationToken cancellationToken)
    {
        if (!IsAvailable)
        {
            throw new InvalidOperationException("llama.cpp 配置无效；地址必须是 loopback HTTP 地址且模型不能为空。");
        }

        var expectedIds = request.Cues.Select(cue => cue.CueId).ToArray();
        var schema = new
        {
            type = "object",
            additionalProperties = false,
            required = new[] { "cues" },
            properties = new
            {
                cues = new
                {
                    type = "array",
                    minItems = request.Cues.Count,
                    maxItems = request.Cues.Count,
                    items = new
                    {
                        type = "object",
                        additionalProperties = false,
                        required = new[] { "cueId", "text" },
                        properties = new
                        {
                            cueId = new { type = "integer", @enum = expectedIds },
                            text = new { type = "string", minLength = 1 }
                        }
                    }
                }
            }
        };
        var userPayload = new
        {
            sourceLanguage = request.SourceLanguage,
            targetLanguage = request.TargetLanguage,
            style = request.Profile.Style,
            preserveExplicitLanguage = request.Profile.PreserveExplicitLanguage,
            previousSourceEnding = request.PreviousSourceContext,
            previousTranslationEnding = request.PreviousTranslationContext,
            characterNames = request.Profile.CharacterNames,
            glossary = request.Profile.Glossary,
            cues = request.Cues.Select(cue => new { cueId = cue.CueId, index = cue.Index, text = cue.Text })
        };
        var systemPrompt = """
            你是专业影视字幕译者。只返回符合指定 JSON Schema 的对象，不要返回 Markdown、解释、时间戳或思考过程。
            必须逐条保留输入 Cue ID，不能缺失、重复、合并、新增或修改 ID。只翻译 text，时间轴由应用维护。
            结合连续对白、上一批结尾、人物名和词汇表保持称呼、专名、语气和上下文一致。
            原文中的脏话、性、暴力、歧视或其他敏感表达必须按原强度忠实翻译；不得审查、说教、回避、删减、美化或委婉替换。
            输出简洁自然的中文字幕，不添加原文没有的信息。/no_think
            """;
        var userPrompt = """
            /no_think
            任务：把 input.cues 中每一条 text 从 sourceLanguage 翻译为 targetLanguage。
            输出必须是 {"cues":[{"cueId":输入ID,"text":"译文"}]}。text 必须是译文，不得照抄源文。
            输出只能包含 cues；每一项只能包含 cueId 和 text，禁止回显 input 的其他字段。
            input=
            """ + JsonSerializer.Serialize(userPayload, PromptJsonOptions);
        var payload = new
        {
            model = Model,
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            temperature = aiOptions.TranslationTemperature,
            max_tokens = aiOptions.MaxOutputTokensPerChunk,
            stream = false,
            response_format = new { type = "json_schema", schema },
            chat_template_kwargs = new { enable_thinking = false }
        };

        using var message = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload, JsonSerializerOptions.Web), Encoding.UTF8, "application/json")
        };
        AddAuthorization(message, options.ApiKey);
        using var response = await SendAsync(client, message, options.TimeoutSeconds, "llama.cpp", cancellationToken);
        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken);
        var root = document.RootElement;
        if (!root.TryGetProperty("choices", out var choices)
            || choices.ValueKind != JsonValueKind.Array
            || choices.GetArrayLength() == 0
            || !choices[0].TryGetProperty("message", out var responseMessage)
            || !responseMessage.TryGetProperty("content", out var contentElement)
            || contentElement.ValueKind != JsonValueKind.String)
        {
            throw new InvalidOperationException("llama.cpp 返回缺少 choices[0].message.content。");
        }

        var content = contentElement.GetString()?.Trim() ?? "";
        if (content.Contains("<think>", StringComparison.OrdinalIgnoreCase)
            || content.Contains("```", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("llama.cpp 返回了思考过程或 Markdown，而不是纯结构化结果。");
        }

        TranslationEnvelope? envelope;
        try
        {
            envelope = JsonSerializer.Deserialize<TranslationEnvelope>(content, JsonSerializerOptions.Web);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("llama.cpp 返回的结构化字幕 JSON 无效。", ex);
        }
        if (envelope?.Cues is null)
        {
            throw new InvalidOperationException("llama.cpp 返回的结构化字幕缺少 cues。");
        }

        var promptTokens = 0;
        var completionTokens = 0;
        if (root.TryGetProperty("usage", out var usage))
        {
            promptTokens = TryReadInt32(usage, "prompt_tokens");
            completionTokens = TryReadInt32(usage, "completion_tokens");
        }
        if (aiOptions.SemanticReviewEnabled)
        {
            var reviewUsage = await ReviewTranslationAsync(request, envelope.Cues, cancellationToken);
            promptTokens = checked(promptTokens + reviewUsage.PromptTokens);
            completionTokens = checked(completionTokens + reviewUsage.CompletionTokens);
        }
        return new TranslationOutput(
            envelope.Cues,
            promptTokens > 0 ? promptTokens : request.Cues.Sum(cue => cue.Text.Length),
            completionTokens > 0 ? completionTokens : envelope.Cues.Sum(cue => cue.Text.Length));
    }

    private async Task<ProviderUsage> ReviewTranslationAsync(
        TranslationRequest request,
        IReadOnlyList<TranslatedCue> translated,
        CancellationToken cancellationToken)
    {
        var expectedIds = request.Cues.Select(cue => cue.CueId).Order().ToArray();
        var actualIds = translated.Select(cue => cue.CueId).Order().ToArray();
        if (!expectedIds.SequenceEqual(actualIds)
            || translated.Select(cue => cue.CueId).Distinct().Count() != translated.Count)
        {
            throw new InvalidOperationException("语义审查前发现翻译 Cue ID 缺失、重复或未知。");
        }

        var translatedById = translated.ToDictionary(cue => cue.CueId);
        var reviewInput = request.Cues.Select(cue => new
        {
            cueId = cue.CueId,
            sourceText = cue.Text,
            translatedText = translatedById[cue.CueId].Text
        });
        var systemPrompt = """
            你是严格的双语字幕质量审查员。只检查原文与中文译文的语义忠实度、遗漏、凭空添加、人物名、数字、语气及粗俗/敏感表达强度。
            不得以内容敏感为由判错，不得要求弱化、审查或美化原文。只返回 JSON，不要翻译、改写或输出思考过程。/no_think
            """;
        var userPrompt = """
            /no_think
            逐条审查 pairs。仅当译文存在实质性错译、漏译、幻觉或强度改变时，才把 Cue ID 放入 invalidCueIds。
            返回且只返回 {"invalidCueIds":[],"reason":""}；全部忠实时 invalidCueIds 必须为空。reason 只写简短原因，不要回显字幕全文。
            pairs=
            """ + JsonSerializer.Serialize(reviewInput, PromptJsonOptions);
        var payload = new
        {
            model = Model,
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            temperature = 0,
            max_tokens = aiOptions.SemanticReviewMaxTokens,
            stream = false,
            response_format = new { type = "json_object" },
            chat_template_kwargs = new { enable_thinking = false }
        };

        using var message = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload, JsonSerializerOptions.Web), Encoding.UTF8, "application/json")
        };
        AddAuthorization(message, options.ApiKey);
        using var response = await SendAsync(client, message, options.TimeoutSeconds, "llama.cpp 语义审查", cancellationToken);
        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken);
        var root = document.RootElement;
        if (!root.TryGetProperty("choices", out var choices)
            || choices.ValueKind != JsonValueKind.Array
            || choices.GetArrayLength() == 0
            || !choices[0].TryGetProperty("message", out var responseMessage)
            || !responseMessage.TryGetProperty("content", out var contentElement)
            || contentElement.ValueKind != JsonValueKind.String)
        {
            throw new InvalidOperationException("llama.cpp 语义审查返回缺少 choices[0].message.content。");
        }

        var content = contentElement.GetString()?.Trim() ?? "";
        if (content.Contains("<think>", StringComparison.OrdinalIgnoreCase)
            || content.Contains("```", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("llama.cpp 语义审查返回了思考过程或 Markdown。");
        }

        SemanticReviewEnvelope? review;
        try
        {
            review = JsonSerializer.Deserialize<SemanticReviewEnvelope>(content, JsonSerializerOptions.Web);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("llama.cpp 语义审查 JSON 无效。", ex);
        }
        if (review?.InvalidCueIds is null)
        {
            throw new InvalidOperationException("llama.cpp 语义审查缺少 invalidCueIds。");
        }
        var invalidIds = review.InvalidCueIds.Distinct().Order().ToArray();
        if (invalidIds.Any(id => !expectedIds.Contains(id)))
        {
            throw new InvalidOperationException("llama.cpp 语义审查返回了未知 Cue ID。");
        }
        if (invalidIds.Length > 0)
        {
            throw new InvalidOperationException($"语义审查拒绝本批译文：{invalidIds.Length} 条 Cue 存在实质性错译或幻觉（Cue ID: {string.Join(", ", invalidIds.Take(12))}）。");
        }

        var promptTokens = 0;
        var completionTokens = 0;
        if (root.TryGetProperty("usage", out var usage))
        {
            promptTokens = TryReadInt32(usage, "prompt_tokens");
            completionTokens = TryReadInt32(usage, "completion_tokens");
        }
        return new ProviderUsage(promptTokens, completionTokens);
    }

    private static int TryReadInt32(JsonElement element, string name) =>
        element.TryGetProperty(name, out var property) && property.TryGetInt32(out var value) ? value : 0;

    private sealed record TranslationEnvelope(IReadOnlyList<TranslatedCue> Cues);
    private sealed record SemanticReviewEnvelope(IReadOnlyList<long> InvalidCueIds, string? Reason);
    private sealed record ProviderUsage(int PromptTokens, int CompletionTokens);
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
        options.Speech.Provider.ToLowerInvariant() switch
        {
            "mock" => new MockSpeechRecognitionProvider(options),
            "fasterwhisper" or "faster-whisper" => new FasterWhisperSpeechRecognitionProvider(options.Speech),
            _ => new UnavailableSpeechRecognitionProvider(options.Speech)
        };

    public static ISubtitleTranslationProvider CreateTranslation(AiSubtitleOptions options) =>
        options.Translation.Provider.ToLowerInvariant() switch
        {
            "mock" => new MockSubtitleTranslationProvider(options),
            "llamacpp" or "llama.cpp" => new LlamaCppSubtitleTranslationProvider(options.Translation, options),
            _ => new UnavailableSubtitleTranslationProvider(options.Translation)
        };
}

public sealed class AudioChunkExtractor(MediaTools mediaTools, AiSubtitleOptions options)
{
    public async Task<string> ExtractAsync(
        string videoPath,
        string jobId,
        int chunkIndex,
        double startSeconds,
        double durationSeconds,
        CancellationToken cancellationToken)
    {
        if (jobId.Length != 32 || jobId.Any(character => !char.IsAsciiHexDigit(character)))
        {
            throw new InvalidOperationException("AI 任务 ID 无效。");
        }

        var jobPath = Path.GetFullPath(Path.Combine(options.AudioWorkingPath, jobId));
        var rootPath = Path.GetFullPath(options.AudioWorkingPath).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!jobPath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("AI 音频工作目录无效。");
        }
        Directory.CreateDirectory(jobPath);
        var outputPath = Path.Combine(jobPath, $"{chunkIndex:D5}.wav");
        if (File.Exists(outputPath))
        {
            File.Delete(outputPath);
        }

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = mediaTools.FfmpegPath,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }
        };
        var arguments = new[]
        {
            "-hide_banner", "-loglevel", "error", "-y",
            "-ss", startSeconds.ToString("0.###", CultureInfo.InvariantCulture),
            "-i", videoPath,
            "-t", durationSeconds.ToString("0.###", CultureInfo.InvariantCulture),
            "-map", "0:a:0", "-vn", "-ac", "1", "-ar", "16000",
            "-c:a", "pcm_s16le", "-f", "wav", outputPath
        };
        foreach (var argument in arguments)
        {
            process.StartInfo.ArgumentList.Add(argument);
        }

        if (!process.Start())
        {
            throw new InvalidOperationException("无法启动 FFmpeg 音频分块。");
        }
        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(options.Speech.TimeoutSeconds));
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync(CancellationToken.None);
            }
            cancellationToken.ThrowIfCancellationRequested();
            throw new InvalidOperationException($"FFmpeg 音频分块超过 {options.Speech.TimeoutSeconds} 秒。");
        }

        _ = await outputTask;
        var error = await errorTask;
        if (process.ExitCode != 0 || !File.Exists(outputPath) || new FileInfo(outputPath).Length <= 44)
        {
            TryDelete(outputPath);
            throw new InvalidOperationException($"FFmpeg 音频分块失败：{SanitizeProviderError(error)}");
        }
        return outputPath;
    }

    public void DeleteJobDirectory(string jobId)
    {
        if (jobId.Length != 32 || jobId.Any(character => !char.IsAsciiHexDigit(character)))
        {
            return;
        }
        var path = Path.GetFullPath(Path.Combine(options.AudioWorkingPath, jobId));
        var root = Path.GetFullPath(options.AudioWorkingPath).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (path.StartsWith(root, StringComparison.OrdinalIgnoreCase) && Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }

    private static void TryDelete(string path)
    {
        try { File.Delete(path); } catch (IOException) { } catch (UnauthorizedAccessException) { }
    }
}

public sealed class AiSubtitlePipeline(
    CinemaDbContext db,
    VideoStorage storage,
    AiSubtitleOptions options,
    ISpeechRecognitionProvider speechProvider,
    ISubtitleTranslationProvider translationProvider,
    AudioChunkExtractor audioChunkExtractor)
{
    private const string TranslationProtocolVersion = "unicode-review-v2";
    public async Task<string> RunSpeechRecognitionAsync(
        string jobId,
        CancellationToken cancellationToken,
        Func<JobProgress, Task> reportProgress)
    {
        EnsureEnabled(speechProvider, "语音识别");
        var job = await db.MediaJobs.AsNoTracking().FirstAsync(item => item.Id == jobId, cancellationToken);
        var video = await db.Videos.AsNoTracking().FirstAsync(item => item.Id == job.VideoId, cancellationToken);
        var requestedLanguage = GetSpeechLanguage(job.InputJson);
        var sourceKey = BuildSourceKey("speech", video.Id, requestedLanguage ?? "auto", speechProvider);
        var existingTrack = await FindGeneratedTrackAsync(video.Id, sourceKey, cancellationToken);
        if (existingTrack is not null)
        {
            return SerializeResult(existingTrack, speechProvider, false);
        }

        if (speechProvider.IsMock)
        {
            return await RunMockSpeechRecognitionAsync(jobId, video, requestedLanguage, sourceKey, cancellationToken, reportProgress);
        }
        if (video.DurationSeconds is null or <= 0)
        {
            throw new InvalidOperationException("视频缺少有效时长，请先执行一次视频同步/分析。");
        }

        var durationSeconds = video.DurationSeconds.Value;
        var chunkCount = (int)Math.Ceiling(durationSeconds / options.SpeechChunkSeconds);
        var videoPath = storage.GetAbsolutePath(video.RelativePath);
        var outputs = new List<SpeechRecognitionOutput>(chunkCount);
        try
        {
            for (var index = 0; index < chunkCount; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var offsetSeconds = index * (double)options.SpeechChunkSeconds;
                var chunkDuration = Math.Min(options.SpeechChunkSeconds, durationSeconds - offsetSeconds);
                var checkpoint = await ReadCheckpointAsync<SpeechRecognitionOutput>(jobId, index, cancellationToken);
                if (checkpoint is null)
                {
                    await reportProgress(new JobProgress(
                        3 + (int)Math.Round(12d * index / Math.Max(1, chunkCount)),
                        "提取识别音频块",
                        $"第 {index + 1} / {chunkCount} 块"));
                    var audioPath = await audioChunkExtractor.ExtractAsync(
                        videoPath, jobId, index, offsetSeconds, chunkDuration, cancellationToken);
                    try
                    {
                        var localOutput = await ExecuteSpeechWithRetryAsync(
                            jobId,
                            index,
                            new SpeechRecognitionRequest(
                                audioPath,
                                chunkDuration,
                                requestedLanguage,
                                ReleaseModelAfterRequest: index == chunkCount - 1),
                            cancellationToken);
                        checkpoint = new SpeechRecognitionOutput(
                            localOutput.Language,
                            localOutput.Cues.Select(cue => new RecognizedCue(
                                cue.Index,
                                cue.StartMilliseconds + (long)Math.Round(offsetSeconds * 1000),
                                cue.EndMilliseconds + (long)Math.Round(offsetSeconds * 1000),
                                cue.Text)).ToList(),
                            localOutput.InputUnits);
                        ValidateRecognizedCues(checkpoint.Cues, allowEmpty: true);
                        await SaveCheckpointAsync(
                            jobId,
                            index,
                            "SpeechRecognition",
                            checkpoint,
                            checkpoint.InputUnits,
                            checkpoint.Cues.Sum(cue => cue.Text.Length),
                            cancellationToken);
                    }
                    finally
                    {
                        TryDeleteFile(audioPath);
                    }
                }
                else
                {
                    ValidateRecognizedCues(checkpoint.Cues, allowEmpty: true);
                }
                outputs.Add(checkpoint);
                var percent = 15 + (int)Math.Round(65d * (index + 1) / Math.Max(1, chunkCount));
                await reportProgress(new JobProgress(percent, "语音识别并保存检查点", $"第 {index + 1} / {chunkCount} 块"));
            }
        }
        finally
        {
            try { audioChunkExtractor.DeleteJobDirectory(jobId); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        }

        var recognizedCues = outputs
            .SelectMany(output => output.Cues)
            .OrderBy(cue => cue.StartMilliseconds)
            .ThenBy(cue => cue.EndMilliseconds)
            .Select((cue, index) => cue with { Index = index })
            .ToList();
        ValidateRecognizedCues(recognizedCues, allowEmpty: false);
        var language = requestedLanguage
            ?? outputs.Select(output => output.Language)
                .Where(value => !string.IsNullOrWhiteSpace(value) && value != "und")
                .GroupBy(value => value, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(group => group.Count())
                .Select(group => group.Key)
                .FirstOrDefault()
            ?? "und";
        await reportProgress(new JobProgress(85, "保存原始识别稿", $"{recognizedCues.Count} 条"));
        var track = await CreateTrackAsync(
            video.Id,
            sourceKey,
            "AI 原始识别稿",
            language,
            SubtitleKind.Original,
            SubtitleSource.SpeechRecognition,
            SubtitleRevisionStage.RawRecognition,
            recognizedCues.Select(cue => new NewCue(cue.Index, cue.StartMilliseconds, cue.EndMilliseconds, cue.Text)).ToList(),
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
        var profile = input.Profile ?? options.CreateTranslationProfile();
        var sourceTrack = await db.SubtitleTracks
            .AsNoTracking()
            .Include(track => track.Cues)
            .FirstAsync(track => track.Id == input.SourceTrackId && track.VideoId == job.VideoId, cancellationToken);
        var sourceKey = BuildSourceKey(
            "translation",
            sourceTrack.Id,
            input.TargetLanguage,
            translationProvider,
            Fingerprint(new
            {
                profile,
                protocol = TranslationProtocolVersion,
                options.TranslationChunkSize,
                options.SemanticReviewEnabled
            }),
            input.RetranslationId);
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
        var previousSourceContext = "";
        var previousTranslationContext = "";
        var totalUsage = 0;
        for (var index = 0; index < chunks.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var chunk = chunks[index];
            var checkpoint = await ReadCheckpointAsync<TranslationOutput>(jobId, index, cancellationToken);
            if (checkpoint is null)
            {
                var request = new TranslationRequest(
                    sourceTrack.Language,
                    input.TargetLanguage,
                    profile,
                    previousSourceContext,
                    previousTranslationContext,
                    chunk.Select(cue => new TranslationCue(cue.Id, cue.Index, cue.Text)).ToList());
                checkpoint = await ExecuteTranslationWithRetryAsync(jobId, index, chunk, request, totalUsage, cancellationToken);
                await SaveCheckpointAsync(
                    jobId,
                    index,
                    "Translation",
                    checkpoint,
                    checkpoint.InputUnits,
                    checkpoint.OutputUnits,
                    cancellationToken);
            }
            else
            {
                ValidateTranslation(chunk, checkpoint.Cues);
            }

            totalUsage = checked(totalUsage + checkpoint.InputUnits + checkpoint.OutputUnits);
            if (totalUsage > options.MaxTotalTokens)
            {
                throw new InvalidOperationException($"翻译任务累计用量 {totalUsage} 超过上限 {options.MaxTotalTokens} tokens/units，已保留此前检查点。");
            }
            foreach (var cue in checkpoint.Cues)
            {
                translated[cue.CueId] = cue.Text;
            }
            previousSourceContext = string.Join('\n', chunk.TakeLast(options.ContextCueCount).Select(cue => cue.Text));
            previousTranslationContext = string.Join('\n', checkpoint.Cues.TakeLast(options.ContextCueCount).Select(cue => cue.Text));
            var percent = 10 + (int)Math.Round(75d * (index + 1) / Math.Max(1, chunks.Count));
            await reportProgress(new JobProgress(percent, "翻译并严格校验字幕", $"第 {index + 1} / {chunks.Count} 块"));
        }

        var newCues = sourceCues.Select(cue => new NewCue(
            cue.Index,
            cue.StartMilliseconds,
            cue.EndMilliseconds,
            translated[cue.Id])).ToList();
        await reportProgress(new JobProgress(90, "保存中文初译稿", $"{newCues.Count} 条"));
        var track = await CreateTrackAsync(
            sourceTrack.VideoId,
            sourceKey,
            input.RetranslationId is null ? "中文初译稿（AI）" : "中文初译稿（AI，重译）",
            input.TargetLanguage,
            SubtitleKind.Translated,
            SubtitleSource.AiTranslation,
            SubtitleRevisionStage.ChineseDraft,
            newCues,
            cancellationToken);
        return SerializeResult(track, translationProvider, true);
    }

    private async Task<string> RunMockSpeechRecognitionAsync(
        string jobId,
        Video video,
        string? requestedLanguage,
        string sourceKey,
        CancellationToken cancellationToken,
        Func<JobProgress, Task> reportProgress)
    {
        await reportProgress(new JobProgress(5, "准备模拟语音识别", video.OriginalFileName));
        var checkpoint = await ReadCheckpointAsync<SpeechRecognitionOutput>(jobId, 0, cancellationToken);
        if (checkpoint is null)
        {
            checkpoint = await speechProvider.TranscribeAsync(
                new SpeechRecognitionRequest(storage.GetAbsolutePath(video.RelativePath), video.DurationSeconds, requestedLanguage),
                cancellationToken);
            ValidateRecognizedCues(checkpoint.Cues, allowEmpty: false);
            await SaveCheckpointAsync(jobId, 0, "SpeechRecognition", checkpoint, checkpoint.InputUnits, checkpoint.Cues.Sum(cue => cue.Text.Length), cancellationToken);
        }
        await reportProgress(new JobProgress(80, "保存模拟原文字幕", $"{checkpoint.Cues.Count} 条"));
        var track = await CreateTrackAsync(
            video.Id,
            sourceKey,
            "AI 原文（模拟）",
            checkpoint.Language,
            SubtitleKind.Original,
            SubtitleSource.SpeechRecognition,
            SubtitleRevisionStage.RawRecognition,
            checkpoint.Cues.Select(cue => new NewCue(cue.Index, cue.StartMilliseconds, cue.EndMilliseconds, cue.Text)).ToList(),
            cancellationToken);
        return SerializeResult(track, speechProvider, false);
    }

    private async Task<SpeechRecognitionOutput> ExecuteSpeechWithRetryAsync(
        string jobId,
        int index,
        SpeechRecognitionRequest request,
        CancellationToken cancellationToken)
    {
        Exception? lastError = null;
        for (var attempt = 1; attempt <= options.ProviderMaxAttempts; attempt++)
        {
            try
            {
                var output = await speechProvider.TranscribeAsync(request, cancellationToken);
                ValidateRecognizedCues(output.Cues, allowEmpty: true);
                return output;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException or JsonException)
            {
                lastError = ex;
                await MarkChunkFailureAsync(jobId, index, "SpeechRecognition", ex.Message, cancellationToken);
                if (attempt < options.ProviderMaxAttempts)
                {
                    await Task.Delay(TimeSpan.FromSeconds(attempt), cancellationToken);
                }
            }
        }
        throw new InvalidOperationException($"第 {index + 1} 个语音块在 {options.ProviderMaxAttempts} 次尝试后仍失败：{SanitizeProviderError(lastError?.Message)}");
    }

    private async Task<TranslationOutput> ExecuteTranslationWithRetryAsync(
        string jobId,
        int index,
        IReadOnlyList<SubtitleCue> source,
        TranslationRequest request,
        int previousUsage,
        CancellationToken cancellationToken)
    {
        Exception? lastError = null;
        for (var attempt = 1; attempt <= options.ProviderMaxAttempts; attempt++)
        {
            try
            {
                var output = await translationProvider.TranslateAsync(request, cancellationToken);
                ValidateTranslation(source, output.Cues);
                var projectedUsage = checked(previousUsage + output.InputUnits + output.OutputUnits);
                if (projectedUsage > options.MaxTotalTokens)
                {
                    throw new AiUsageLimitException($"本块完成后累计用量将达到 {projectedUsage}，超过上限 {options.MaxTotalTokens} tokens/units。");
                }
                return output;
            }
            catch (OperationCanceledException) { throw; }
            catch (AiUsageLimitException) { throw; }
            catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException or JsonException)
            {
                lastError = ex;
                await MarkChunkFailureAsync(jobId, index, "Translation", ex.Message, cancellationToken);
                if (attempt < options.ProviderMaxAttempts)
                {
                    await Task.Delay(TimeSpan.FromSeconds(attempt), cancellationToken);
                }
            }
        }
        throw new InvalidOperationException($"第 {index + 1} 个翻译块在 {options.ProviderMaxAttempts} 次尝试后仍无效：{SanitizeProviderError(lastError?.Message)} 已保留此前检查点。");
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
        string revisionStage,
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
            RevisionStage = revisionStage,
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
        var chunk = await GetOrCreateChunkAsync(jobId, index, kind, cancellationToken);
        var now = DateTime.Now;
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

    private async Task MarkChunkFailureAsync(
        string jobId,
        int index,
        string kind,
        string error,
        CancellationToken cancellationToken)
    {
        var chunk = await GetOrCreateChunkAsync(jobId, index, kind, cancellationToken);
        chunk.Status = AiJobChunkStatus.Failed;
        chunk.OutputJson = null;
        chunk.AttemptCount++;
        chunk.Error = SanitizeProviderError(error);
        chunk.UpdatedAt = DateTime.Now;
        chunk.CompletedAt = null;
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<AiJobChunk> GetOrCreateChunkAsync(
        string jobId,
        int index,
        string kind,
        CancellationToken cancellationToken)
    {
        var chunk = await db.AiJobChunks.FirstOrDefaultAsync(
            item => item.JobId == jobId && item.Index == index,
            cancellationToken);
        if (chunk is not null)
        {
            return chunk;
        }
        var now = DateTime.Now;
        chunk = new AiJobChunk
        {
            Id = Guid.NewGuid().ToString("N"),
            JobId = jobId,
            Index = index,
            Kind = kind,
            Status = AiJobChunkStatus.Queued,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.AiJobChunks.Add(chunk);
        return chunk;
    }

    private static void ValidateRecognizedCues(IReadOnlyList<RecognizedCue> cues, bool allowEmpty)
    {
        if ((!allowEmpty && cues.Count == 0)
            || cues.Any(cue => cue.StartMilliseconds < 0
                || cue.EndMilliseconds <= cue.StartMilliseconds
                || string.IsNullOrWhiteSpace(cue.Text)))
        {
            throw new InvalidOperationException("语音识别 Provider 返回了无效的分段时间轴或空文本。");
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
            throw new InvalidOperationException("翻译结果存在 Cue ID 缺失、重复、未知或空译文，已拒绝保存。");
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

    private static string BuildSourceKey(
        string kind,
        string sourceId,
        string language,
        IAiProvider provider,
        string? profile = null,
        string? retranslationId = null)
    {
        var raw = $"{kind}|{sourceId}|{language}|{provider.Name}|{provider.Model}|{profile}|{retranslationId}";
        return $"ai:{kind}:{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw))).ToLowerInvariant()}";
    }

    private static string Fingerprint<T>(T value) =>
        Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(value, JsonSerializerOptions.Web))).ToLowerInvariant();

    private static void TryDeleteFile(string path)
    {
        try { File.Delete(path); } catch (IOException) { } catch (UnauthorizedAccessException) { }
    }

    private sealed record NewCue(int Index, long StartMilliseconds, long EndMilliseconds, string Text);
}

public sealed class AiUsageLimitException(string message) : InvalidOperationException(message);

public sealed record SpeechRecognitionRequest(
    string AudioPath,
    double? DurationSeconds,
    string? Language,
    bool ReleaseModelAfterRequest = false);
public sealed record RecognizedCue(int Index, long StartMilliseconds, long EndMilliseconds, string Text);
public sealed record SpeechRecognitionOutput(string Language, IReadOnlyList<RecognizedCue> Cues, int InputUnits);
public sealed record TranslationCue(long CueId, int Index, string Text);
public sealed record TranslationProfile(
    string Style,
    bool PreserveExplicitLanguage,
    IReadOnlyList<string> CharacterNames,
    IReadOnlyDictionary<string, string> Glossary);
public sealed record TranslationRequest(
    string SourceLanguage,
    string TargetLanguage,
    TranslationProfile Profile,
    string PreviousSourceContext,
    string PreviousTranslationContext,
    IReadOnlyList<TranslationCue> Cues);
public sealed record TranslatedCue(long CueId, string Text);
public sealed record TranslationOutput(IReadOnlyList<TranslatedCue> Cues, int InputUnits, int OutputUnits);
public sealed record SpeechJobInput(string? Language);
public sealed record TranslationJobInput(
    string SourceTrackId,
    string TargetLanguage,
    TranslationProfile? Profile = null,
    string? RetranslationId = null);
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

internal static class AiHttp
{
public static HttpClient CreateHttpClient(AiProviderOptions options)
{
    var client = new HttpClient(new SocketsHttpHandler
    {
        ConnectTimeout = TimeSpan.FromSeconds(Math.Min(30, options.TimeoutSeconds)),
        PooledConnectionLifetime = TimeSpan.FromMinutes(10)
    })
    {
        Timeout = Timeout.InfiniteTimeSpan
    };
    return client;
}

public static Uri? TryBuildLoopbackEndpoint(string baseUrl, string relativePath)
{
    if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri)
        || baseUri.Scheme != Uri.UriSchemeHttp
        || !IsLoopbackHost(baseUri.Host))
    {
        return null;
    }
    var normalized = baseUri.AbsoluteUri.EndsWith("/", StringComparison.Ordinal)
        ? baseUri
        : new Uri(baseUri.AbsoluteUri + '/', UriKind.Absolute);
    return new Uri(normalized, relativePath);
}

private static bool IsLoopbackHost(string host)
{
    if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
    {
        return true;
    }
    return IPAddress.TryParse(host, out var address) && IPAddress.IsLoopback(address);
}

public static void AddAuthorization(HttpRequestMessage message, string apiKey)
{
    if (!string.IsNullOrWhiteSpace(apiKey))
    {
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
    }
}

public static async Task<HttpResponseMessage> SendAsync(
    HttpClient client,
    HttpRequestMessage message,
    int timeoutSeconds,
    string providerName,
    CancellationToken cancellationToken)
{
    using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    timeout.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));
    HttpResponseMessage response;
    try
    {
        response = await client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
    }
    catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
    {
        throw new InvalidOperationException($"{providerName} 请求超过 {timeoutSeconds} 秒。");
    }
    catch (HttpRequestException ex)
    {
        throw new InvalidOperationException($"无法连接 {providerName} loopback 服务：{SanitizeProviderError(ex.Message)}", ex);
    }
    if (!response.IsSuccessStatusCode)
    {
        var status = $"{(int)response.StatusCode} {response.ReasonPhrase}".Trim();
        response.Dispose();
        throw new InvalidOperationException($"{providerName} 返回 HTTP {status}。");
    }
    return response;
}

public static string SanitizeProviderError(string? value)
{
    var text = string.IsNullOrWhiteSpace(value) ? "没有错误详情。" : value.Replace('\r', ' ').Replace('\n', ' ').Trim();
    return text.Length <= 500 ? text : text[..500];
}
}
