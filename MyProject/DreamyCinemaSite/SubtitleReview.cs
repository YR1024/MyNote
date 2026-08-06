using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;

public sealed class SubtitleReviewService(CinemaDbContext db)
{
    public async Task<SubtitleReviewDocument?> GetDocumentAsync(
        string videoId,
        string? sourceTrackId,
        string? targetTrackId,
        CancellationToken cancellationToken = default)
    {
        var video = await db.Videos.AsNoTracking().FirstOrDefaultAsync(item => item.Id == videoId, cancellationToken);
        if (video is null)
        {
            return null;
        }

        var tracks = await db.SubtitleTracks
            .AsNoTracking()
            .Include(track => track.Cues)
            .Where(track => track.VideoId == videoId)
            .OrderByDescending(track => track.UpdatedAt)
            .ToListAsync(cancellationToken);
        var sourceTracks = tracks.Where(IsSourceTrack).OrderByDescending(SourcePriority).ThenByDescending(track => track.UpdatedAt).ToList();
        var targetTracks = tracks.Where(IsReviewTarget).OrderByDescending(TargetPriority).ThenByDescending(track => track.UpdatedAt).ToList();
        var source = sourceTracks.FirstOrDefault(track => track.Id == sourceTrackId) ?? sourceTracks.FirstOrDefault();
        var target = targetTracks.FirstOrDefault(track => track.Id == targetTrackId) ?? targetTracks.FirstOrDefault();
        if (source is null)
        {
            return new SubtitleReviewDocument(
                video.Id, video.Number, $"/api/videos/{Uri.EscapeDataString(video.Id)}/stream",
                sourceTracks.Select(ToSummary).ToList(), targetTracks.Select(ToSummary).ToList(),
                null, target?.Id, [], 0, 0, 0);
        }

        var targetByIndex = target?.Cues.ToDictionary(cue => cue.Index) ?? new Dictionary<int, SubtitleCue>();
        var reviewCues = new List<SubtitleReviewCue>();
        foreach (var sourceCue in source.Cues.OrderBy(cue => cue.Index))
        {
            targetByIndex.TryGetValue(sourceCue.Index, out var targetCue);
            var warnings = BuildWarnings(sourceCue, targetCue);
            reviewCues.Add(new SubtitleReviewCue(
                sourceCue.Index,
                sourceCue.Id,
                targetCue?.Id,
                sourceCue.StartMilliseconds,
                sourceCue.EndMilliseconds,
                sourceCue.Text,
                targetCue?.Text ?? "",
                sourceCue.IsManuallyEdited,
                targetCue?.IsManuallyEdited ?? false,
                warnings));
        }

        return new SubtitleReviewDocument(
            video.Id,
            video.Number,
            $"/api/videos/{Uri.EscapeDataString(video.Id)}/stream",
            sourceTracks.Select(ToSummary).ToList(),
            targetTracks.Select(ToSummary).ToList(),
            source.Id,
            target?.Id,
            reviewCues,
            reviewCues.Count(cue => cue.Warnings.Count > 0),
            reviewCues.Count(cue => cue.IsSourceManuallyEdited),
            reviewCues.Count(cue => cue.IsManuallyEdited));
    }

    public async Task<SubtitleReviewEditResult?> EditSourceCueAsync(
        string videoId,
        SubtitleSourceCueEditRequest request,
        CancellationToken cancellationToken = default)
    {
        var text = request.Text?.Trim() ?? "";
        if (text.Length is 0 or > 4000)
        {
            throw new SubtitleReviewValidationException("原文字幕不能为空且不能超过 4000 个字符。");
        }

        var source = await db.SubtitleTracks.Include(track => track.Cues)
            .FirstOrDefaultAsync(track => track.Id == request.SourceTrackId && track.VideoId == videoId, cancellationToken);
        if (source is null || !IsSourceTrack(source))
        {
            return null;
        }
        var correctedTrack = await EnsureSourceCorrectedTrackAsync(source, cancellationToken);
        var cue = correctedTrack.Cues.FirstOrDefault(item => item.Index == request.Index);
        if (cue is null)
        {
            return null;
        }
        cue.Text = text;
        cue.IsManuallyEdited = true;
        cue.UpdatedAt = DateTime.Now;
        correctedTrack.UpdatedAt = DateTime.Now;
        await db.SaveChangesAsync(cancellationToken);
        return new SubtitleReviewEditResult(ToSummary(correctedTrack), cue.Id, cue.Index, cue.Text, cue.IsManuallyEdited);
    }

    public async Task<SubtitleReviewEditResult?> EditCueAsync(
        string videoId,
        SubtitleCueEditRequest request,
        CancellationToken cancellationToken = default)
    {
        var text = request.Text?.Trim() ?? "";
        if (text.Length is 0 or > 4000)
        {
            throw new SubtitleReviewValidationException("字幕文本不能为空且不能超过 4000 个字符。");
        }

        var target = await db.SubtitleTracks
            .Include(track => track.Cues)
            .FirstOrDefaultAsync(track => track.Id == request.TargetTrackId && track.VideoId == videoId, cancellationToken);
        if (target is null || !IsReviewTarget(target))
        {
            return null;
        }

        var reviewTrack = await EnsureFinalPolishedTrackAsync(target, cancellationToken);
        var cue = reviewTrack.Cues.FirstOrDefault(item => item.Index == request.Index);
        if (cue is null)
        {
            return null;
        }

        cue.Text = text;
        cue.IsManuallyEdited = true;
        cue.UpdatedAt = DateTime.Now;
        reviewTrack.UpdatedAt = DateTime.Now;
        await db.SaveChangesAsync(cancellationToken);
        return new SubtitleReviewEditResult(ToSummary(reviewTrack), cue.Id, cue.Index, cue.Text, cue.IsManuallyEdited);
    }

    public async Task<SubtitleTrack?> ApplyPartialRetranslationAsync(
        string videoId,
        string targetTrackId,
        IReadOnlyDictionary<int, string> translatedByIndex,
        CancellationToken cancellationToken)
    {
        var target = await db.SubtitleTracks
            .Include(track => track.Cues)
            .FirstOrDefaultAsync(track => track.Id == targetTrackId && track.VideoId == videoId, cancellationToken);
        if (target is null || !IsReviewTarget(target))
        {
            return null;
        }

        var reviewTrack = await EnsureFinalPolishedTrackAsync(target, cancellationToken);
        foreach (var cue in reviewTrack.Cues)
        {
            if (!cue.IsManuallyEdited && translatedByIndex.TryGetValue(cue.Index, out var text) && !string.IsNullOrWhiteSpace(text))
            {
                cue.Text = text.Trim();
                cue.UpdatedAt = DateTime.Now;
            }
        }
        reviewTrack.UpdatedAt = DateTime.Now;
        await db.SaveChangesAsync(cancellationToken);
        return reviewTrack;
    }

    public async Task<SubtitleTrack?> GenerateBilingualTrackAsync(
        string videoId,
        string sourceTrackId,
        string targetTrackId,
        CancellationToken cancellationToken = default)
    {
        var tracks = await db.SubtitleTracks
            .Include(track => track.Cues)
            .Where(track => track.VideoId == videoId && (track.Id == sourceTrackId || track.Id == targetTrackId))
            .ToListAsync(cancellationToken);
        var source = tracks.FirstOrDefault(track => track.Id == sourceTrackId && IsSourceTrack(track));
        var target = tracks.FirstOrDefault(track => track.Id == targetTrackId && IsReviewTarget(track));
        if (source is null || target is null)
        {
            return null;
        }

        var sourceKey = $"review:bilingual:{source.Id}:{target.Id}:v1";
        var bilingual = await db.SubtitleTracks
            .Include(track => track.Cues)
            .FirstOrDefaultAsync(track => track.VideoId == videoId && track.SourceKey == sourceKey, cancellationToken);
        var now = DateTime.Now;
        if (bilingual is null)
        {
            bilingual = new SubtitleTrack
            {
                Id = Guid.NewGuid().ToString("N"),
                VideoId = videoId,
                Label = "双语字幕（原文 / 中文）",
                Language = "mul",
                Kind = SubtitleKind.Bilingual,
                Source = SubtitleSource.Review,
                RevisionStage = SubtitleRevisionStage.FinalPolished,
                SourceKey = sourceKey,
                Format = "generated",
                OriginalRelativePath = "",
                IsDefault = false,
                CreatedAt = now
            };
            db.SubtitleTracks.Add(bilingual);
        }
        else
        {
            db.SubtitleCues.RemoveRange(bilingual.Cues);
            bilingual.Cues.Clear();
        }

        var targetByIndex = target.Cues.ToDictionary(cue => cue.Index);
        bilingual.Cues = source.Cues.OrderBy(cue => cue.Index)
            .Where(sourceCue => targetByIndex.ContainsKey(sourceCue.Index))
            .Select(sourceCue => new SubtitleCue
            {
                TrackId = bilingual.Id,
                Index = sourceCue.Index,
                StartMilliseconds = sourceCue.StartMilliseconds,
                EndMilliseconds = sourceCue.EndMilliseconds,
                Text = $"{sourceCue.Text.Trim()}\n{targetByIndex[sourceCue.Index].Text.Trim()}",
                UpdatedAt = now
            }).ToList();
        bilingual.CueCount = bilingual.Cues.Count;
        bilingual.UpdatedAt = now;
        await db.SaveChangesAsync(cancellationToken);
        return bilingual;
    }

    public async Task<SubtitleExport?> ExportAsync(string trackId, string format, CancellationToken cancellationToken = default)
    {
        var track = await db.SubtitleTracks.AsNoTracking().Include(item => item.Cues)
            .FirstOrDefaultAsync(item => item.Id == trackId, cancellationToken);
        if (track is null)
        {
            return null;
        }
        var normalized = format.Trim().ToLowerInvariant();
        var content = normalized switch
        {
            "srt" => BuildSrt(track.Cues),
            "vtt" => BuildVtt(track.Cues),
            _ => throw new SubtitleReviewValidationException("仅支持导出 SRT 或 WebVTT。")
        };
        var safeLabel = string.Concat(track.Label.Select(character => Path.GetInvalidFileNameChars().Contains(character) ? '_' : character));
        return new SubtitleExport(Encoding.UTF8.GetBytes(content), normalized == "srt" ? "application/x-subrip" : "text/vtt", $"{safeLabel}.{normalized}");
    }

    public static bool IsSourceTrack(SubtitleTrack track) =>
        track.Kind == SubtitleKind.Original
        && track.RevisionStage is SubtitleRevisionStage.SourceOriginal or SubtitleRevisionStage.RawRecognition or SubtitleRevisionStage.SourceCorrected;

    public static bool IsReviewTarget(SubtitleTrack track) =>
        track.Kind == SubtitleKind.Translated
        && track.RevisionStage is SubtitleRevisionStage.ChineseDraft or SubtitleRevisionStage.FinalPolished;

    private async Task<SubtitleTrack> EnsureFinalPolishedTrackAsync(SubtitleTrack target, CancellationToken cancellationToken)
    {
        if (target.RevisionStage == SubtitleRevisionStage.FinalPolished)
        {
            return target;
        }

        var sourceKey = $"review:final:{target.Id}";
        var existing = await db.SubtitleTracks.Include(track => track.Cues)
            .FirstOrDefaultAsync(track => track.VideoId == target.VideoId && track.SourceKey == sourceKey, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var now = DateTime.Now;
        var review = new SubtitleTrack
        {
            Id = Guid.NewGuid().ToString("N"),
            VideoId = target.VideoId,
            Label = "中文校对稿",
            Language = target.Language,
            Kind = SubtitleKind.Translated,
            Source = SubtitleSource.Review,
            RevisionStage = SubtitleRevisionStage.FinalPolished,
            SourceKey = sourceKey,
            Format = "generated",
            OriginalRelativePath = "",
            IsDefault = false,
            CueCount = target.Cues.Count,
            CreatedAt = now,
            UpdatedAt = now,
            Cues = target.Cues.OrderBy(cue => cue.Index).Select(cue => new SubtitleCue
            {
                Index = cue.Index,
                StartMilliseconds = cue.StartMilliseconds,
                EndMilliseconds = cue.EndMilliseconds,
                Text = cue.Text,
                IsManuallyEdited = false,
                UpdatedAt = now
            }).ToList()
        };
        foreach (var cue in review.Cues)
        {
            cue.TrackId = review.Id;
        }
        db.SubtitleTracks.Add(review);
        await db.SaveChangesAsync(cancellationToken);
        return review;
    }

    private async Task<SubtitleTrack> EnsureSourceCorrectedTrackAsync(SubtitleTrack source, CancellationToken cancellationToken)
    {
        if (source.RevisionStage == SubtitleRevisionStage.SourceCorrected)
        {
            return source;
        }
        var sourceKey = $"review:source:{source.Id}";
        var existing = await db.SubtitleTracks.Include(track => track.Cues)
            .FirstOrDefaultAsync(track => track.VideoId == source.VideoId && track.SourceKey == sourceKey, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }
        var now = DateTime.Now;
        var corrected = new SubtitleTrack
        {
            Id = Guid.NewGuid().ToString("N"),
            VideoId = source.VideoId,
            Label = "原文校对稿",
            Language = source.Language,
            Kind = SubtitleKind.Original,
            Source = SubtitleSource.Review,
            RevisionStage = SubtitleRevisionStage.SourceCorrected,
            SourceKey = sourceKey,
            Format = "generated",
            OriginalRelativePath = "",
            IsDefault = false,
            CueCount = source.Cues.Count,
            CreatedAt = now,
            UpdatedAt = now,
            Cues = source.Cues.OrderBy(cue => cue.Index).Select(cue => new SubtitleCue
            {
                Index = cue.Index,
                StartMilliseconds = cue.StartMilliseconds,
                EndMilliseconds = cue.EndMilliseconds,
                Text = cue.Text,
                IsManuallyEdited = false,
                UpdatedAt = now
            }).ToList()
        };
        foreach (var cue in corrected.Cues)
        {
            cue.TrackId = corrected.Id;
        }
        db.SubtitleTracks.Add(corrected);
        await db.SaveChangesAsync(cancellationToken);
        return corrected;
    }

    private static IReadOnlyList<SubtitleQualityWarning> BuildWarnings(SubtitleCue source, SubtitleCue? target)
    {
        var warnings = new List<SubtitleQualityWarning>();
        if (target is null || string.IsNullOrWhiteSpace(target.Text))
        {
            warnings.Add(new("MissingTranslation", "缺少中文译文"));
            return warnings;
        }
        if (target.StartMilliseconds != source.StartMilliseconds || target.EndMilliseconds != source.EndMilliseconds)
        {
            warnings.Add(new("TimingMismatch", "中文与原文时间轴不一致"));
        }
        var textLength = new StringInfo(target.Text.Trim()).LengthInTextElements;
        var durationSeconds = Math.Max(0.1, (source.EndMilliseconds - source.StartMilliseconds) / 1000d);
        if (textLength > 32)
        {
            warnings.Add(new("LongText", $"单条中文较长（{textLength} 字）"));
        }
        var charactersPerSecond = textLength / durationSeconds;
        if (charactersPerSecond > 15)
        {
            warnings.Add(new("FastReading", $"阅读速度较快（{charactersPerSecond:0.0} 字/秒）"));
        }
        return warnings;
    }

    private static int SourcePriority(SubtitleTrack track) => track.RevisionStage switch
    {
        SubtitleRevisionStage.SourceCorrected => 3,
        SubtitleRevisionStage.SourceOriginal => 2,
        SubtitleRevisionStage.RawRecognition => 1,
        _ => 0
    };

    private static int TargetPriority(SubtitleTrack track) => track.RevisionStage switch
    {
        SubtitleRevisionStage.FinalPolished => 2,
        SubtitleRevisionStage.ChineseDraft => 1,
        _ => 0
    };

    private static SubtitleReviewTrack ToSummary(SubtitleTrack track) => new(
        track.Id, track.Label, track.Language, track.Kind, track.RevisionStage, track.CueCount,
        $"/api/subtitles/{Uri.EscapeDataString(track.Id)}/vtt");

    private static string BuildSrt(IEnumerable<SubtitleCue> cues)
    {
        var builder = new StringBuilder();
        foreach (var cue in cues.OrderBy(item => item.Index))
        {
            builder.Append(cue.Index + 1).AppendLine();
            builder.Append(FormatTime(cue.StartMilliseconds, ',')).Append(" --> ").Append(FormatTime(cue.EndMilliseconds, ',')).AppendLine();
            builder.AppendLine(cue.Text.Replace("\r", "", StringComparison.Ordinal).Trim()).AppendLine();
        }
        return builder.ToString();
    }

    private static string BuildVtt(IEnumerable<SubtitleCue> cues)
    {
        var builder = new StringBuilder("WEBVTT\n\n");
        foreach (var cue in cues.OrderBy(item => item.Index))
        {
            builder.Append(cue.Index + 1).Append('\n');
            builder.Append(FormatTime(cue.StartMilliseconds, '.')).Append(" --> ").Append(FormatTime(cue.EndMilliseconds, '.')).Append('\n');
            builder.Append(cue.Text.Replace("\r", "", StringComparison.Ordinal).Trim()).Append("\n\n");
        }
        return builder.ToString();
    }

    private static string FormatTime(long milliseconds, char separator)
    {
        var time = TimeSpan.FromMilliseconds(Math.Max(0, milliseconds));
        return $"{(int)time.TotalHours:00}:{time.Minutes:00}:{time.Seconds:00}{separator}{time.Milliseconds:000}";
    }
}

public sealed class SubtitleReviewValidationException(string message) : InvalidOperationException(message);
public sealed record SubtitleReviewTrack(string Id, string Label, string Language, string Kind, string RevisionStage, int CueCount, string VttUrl);
public sealed record SubtitleQualityWarning(string Code, string Message);
public sealed record SubtitleReviewCue(
    int Index, long SourceCueId, long? TargetCueId, long StartMilliseconds, long EndMilliseconds,
    string SourceText, string TargetText, bool IsSourceManuallyEdited, bool IsManuallyEdited, IReadOnlyList<SubtitleQualityWarning> Warnings);
public sealed record SubtitleReviewDocument(
    string VideoId, string VideoNumber, string StreamUrl,
    IReadOnlyList<SubtitleReviewTrack> SourceTracks, IReadOnlyList<SubtitleReviewTrack> TargetTracks,
    string? SourceTrackId, string? TargetTrackId, IReadOnlyList<SubtitleReviewCue> Cues,
    int WarningCount, int SourceManualEditCount, int ManualEditCount);
public sealed record SubtitleCueEditRequest(string TargetTrackId, int Index, string? Text);
public sealed record SubtitleSourceCueEditRequest(string SourceTrackId, int Index, string? Text);
public sealed record SubtitleReviewEditResult(SubtitleReviewTrack Track, long CueId, int Index, string Text, bool IsManuallyEdited);
public sealed record PartialRetranslationRequest(string SourceTrackId, string TargetTrackId, IReadOnlyList<int>? CueIndexes);
public sealed record BilingualTrackRequest(string SourceTrackId, string TargetTrackId);
public sealed record SubtitleExport(byte[] Content, string ContentType, string FileName);
