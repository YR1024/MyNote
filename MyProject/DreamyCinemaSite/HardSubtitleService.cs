using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

public sealed class HardSubtitleService(
    CinemaDbContext db,
    VideoStorage storage,
    MediaTools mediaTools,
    HardSubtitleOptions options,
    MediaAnalyzer mediaAnalyzer,
    SubtitleReviewService subtitleReviewService,
    HardSubtitleTestHook testHook)
{
    public async Task<string> RunAsync(
        string jobId,
        CancellationToken cancellationToken,
        Func<JobProgress, Task> reportProgress)
    {
        var job = await db.MediaJobs
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == jobId, cancellationToken)
            ?? throw new InvalidOperationException("硬字幕任务不存在。");
        var input = JsonSerializer.Deserialize<HardSubtitleJobInput>(
            job.InputJson ?? "",
            JsonSerializerOptions.Web)
            ?? throw new InvalidOperationException("硬字幕任务参数无效。");
        if (string.IsNullOrWhiteSpace(job.VideoId)
            || !string.Equals(job.VideoId, input.VideoId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("硬字幕任务与视频不匹配。");
        }
        var subtitleStyle = HardSubtitleStyle.Normalize(input.Style, options.DefaultStyle);

        var video = await db.Videos
            .FirstOrDefaultAsync(item => item.Id == input.VideoId, cancellationToken)
            ?? throw new InvalidOperationException("视频不存在。");
        var track = await db.SubtitleTracks
            .AsNoTracking()
            .Include(item => item.Cues)
            .FirstOrDefaultAsync(
                item => item.Id == input.SubtitleTrackId && item.VideoId == input.VideoId,
                cancellationToken)
            ?? throw new InvalidOperationException("所选字幕轨道不存在。");
        if (track.CueCount <= 0 || track.Cues.Count == 0)
        {
            throw new InvalidOperationException("所选字幕轨道没有可烧录的内容。");
        }

        var sourcePath = storage.GetAbsolutePath(video.RelativePath);
        if (!video.OriginalAvailable || !File.Exists(sourcePath))
        {
            throw new InvalidOperationException("原视频文件不存在，无法生成新的硬字幕版本。");
        }

        var workRelativePath = Path.Combine("derived", ".work", jobId);
        var workPath = storage.GetAbsolutePath(workRelativePath);
        RecreateDirectory(workPath);
        var subtitlePath = Path.Combine(workPath, "captions.srt");
        var temporaryOutputPath = Path.Combine(workPath, "output.partial.mp4");
        var finalRelativePath = Path.Combine(
            "derived",
            "hard-subtitles",
            video.Id,
            $"{track.Id}-{jobId}.mp4");
        var finalPath = storage.GetAbsolutePath(finalRelativePath);

        try
        {
            await reportProgress(new JobProgress(2, "准备硬字幕", video.OriginalFileName));
            var export = await subtitleReviewService.ExportAsync(track.Id, "srt", cancellationToken)
                ?? throw new InvalidOperationException("无法导出所选字幕轨道。");
            await File.WriteAllBytesAsync(subtitlePath, export.Content, cancellationToken);

            var durationSeconds = video.DurationSeconds
                ?? track.Cues.Max(item => item.EndMilliseconds) / 1000d;
            await reportProgress(new JobProgress(4, "正在烧录字幕", video.OriginalFileName));
            var actualEncoder = options.VideoEncoder;
            var processResult = await RunFfmpegAsync(
                sourcePath,
                temporaryOutputPath,
                workPath,
                durationSeconds,
                video.OriginalFileName,
                actualEncoder,
                subtitleStyle,
                cancellationToken,
                reportProgress);
            if (ShouldFallbackToCpu(actualEncoder, processResult))
            {
                DeleteFileIfPresent(temporaryOutputPath);
                actualEncoder = "libx264";
                await reportProgress(new JobProgress(
                    5,
                    "NVIDIA 编码不可用，改用 CPU",
                    video.OriginalFileName));
                processResult = await RunFfmpegAsync(
                    sourcePath,
                    temporaryOutputPath,
                    workPath,
                    durationSeconds,
                    video.OriginalFileName,
                    actualEncoder,
                    subtitleStyle,
                    cancellationToken,
                    reportProgress);
            }
            if (processResult.ExitCode != 0)
            {
                throw new InvalidOperationException(BuildFfmpegError(processResult.Error));
            }
            if (!File.Exists(temporaryOutputPath) || new FileInfo(temporaryOutputPath).Length == 0)
            {
                throw new InvalidOperationException("FFmpeg 未生成有效的硬字幕视频。");
            }

            await reportProgress(new JobProgress(94, "验证硬字幕视频", video.OriginalFileName));
            var analysis = await mediaAnalyzer.AnalyzeAsync(temporaryOutputPath, cancellationToken);
            if (analysis.Warning is not null
                || !string.Equals(analysis.VideoCodec, "h264", StringComparison.OrdinalIgnoreCase)
                || analysis.Width is null
                || analysis.Height is null)
            {
                throw new InvalidOperationException(
                    analysis.Warning is null
                        ? "硬字幕视频验证失败：输出不是可识别的 H.264 视频。"
                        : $"硬字幕视频验证失败：{analysis.Warning}");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(finalPath)!);
            if (File.Exists(finalPath))
            {
                File.Delete(finalPath);
            }
            File.Move(temporaryOutputPath, finalPath);

            var previousRelativePath = video.HardSubtitleRelativePath;
            var previousValues = new HardSubtitleDatabaseValues(
                video.HardSubtitleRelativePath,
                video.HardSubtitleTrackId,
                video.HardSubtitleTrackUpdatedAt,
                video.HardSubtitleSize,
                video.HardSubtitleCreatedAt,
                video.HardSubtitleStyleJson);
            var now = DateTime.Now;
            video.HardSubtitleRelativePath = finalRelativePath;
            video.HardSubtitleTrackId = track.Id;
            video.HardSubtitleTrackUpdatedAt = track.UpdatedAt;
            video.HardSubtitleSize = new FileInfo(finalPath).Length;
            video.HardSubtitleCreatedAt = now;
            video.HardSubtitleStyleJson = JsonSerializer.Serialize(
                subtitleStyle,
                JsonSerializerOptions.Web);
            video.Status = VideoStatus.Available;
            video.UpdatedAt = now;
            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch
            {
                RestoreDatabaseValues(video, previousValues);
                DeleteFileIfPresent(finalPath);
                throw;
            }

            if (!string.IsNullOrWhiteSpace(previousRelativePath)
                && !string.Equals(previousRelativePath, finalRelativePath, StringComparison.OrdinalIgnoreCase))
            {
                DeleteFileIfPresent(storage.GetAbsolutePath(previousRelativePath));
            }

            var encoderLabel = actualEncoder == "h264_nvenc" ? "NVIDIA" : "CPU";
            await reportProgress(new JobProgress(
                99,
                $"硬字幕视频已生成（{encoderLabel}）",
                video.OriginalFileName));
            var result = new HardSubtitleJobResult(
                video.Id,
                track.Id,
                track.Label,
                finalRelativePath,
                video.HardSubtitleSize.Value,
                now,
                actualEncoder,
                subtitleStyle);
            return JsonSerializer.Serialize(result, JsonSerializerOptions.Web);
        }
        finally
        {
            DeleteDirectoryIfPresent(workPath);
        }
    }

    private async Task<HardSubtitleProcessResult> RunFfmpegAsync(
        string sourcePath,
        string outputPath,
        string workingDirectory,
        double durationSeconds,
        string currentItem,
        string videoEncoder,
        HardSubtitleStyle subtitleStyle,
        CancellationToken cancellationToken,
        Func<JobProgress, Task> reportProgress)
    {
        if (testHook.ConsumeNvencUnavailable(videoEncoder))
        {
            return new HardSubtitleProcessResult(
                1,
                "[h264_nvenc] Cannot load nvcuda.dll\nError while opening encoder.");
        }

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = mediaTools.FfmpegPath,
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }
        };
        foreach (var argument in BuildArguments(sourcePath, outputPath, videoEncoder, subtitleStyle))
        {
            process.StartInfo.ArgumentList.Add(argument);
        }
        if (!process.Start())
        {
            throw new InvalidOperationException("无法启动 FFmpeg。");
        }

        var errorTask = process.StandardError.ReadToEndAsync();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(options.Timeout);
        var lastPercent = 4;
        try
        {
            while (true)
            {
                var line = await process.StandardOutput.ReadLineAsync(timeout.Token);
                if (line is null)
                {
                    break;
                }
                if (!TryGetProgressSeconds(line, out var processedSeconds) || durationSeconds <= 0)
                {
                    continue;
                }

                var percent = Math.Clamp(
                    4 + (int)Math.Floor(processedSeconds / durationSeconds * 88),
                    4,
                    92);
                if (percent <= lastPercent)
                {
                    continue;
                }
                lastPercent = percent;
                await reportProgress(new JobProgress(percent, "正在烧录字幕", currentItem));
            }
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            KillProcess(process);
            throw;
        }
        catch (OperationCanceledException)
        {
            KillProcess(process);
            throw new InvalidOperationException($"硬字幕生成超过 {options.Timeout.TotalHours:0.#} 小时，已终止。");
        }

        return new HardSubtitleProcessResult(process.ExitCode, await errorTask);
    }

    private IReadOnlyList<string> BuildArguments(
        string sourcePath,
        string outputPath,
        string videoEncoder,
        HardSubtitleStyle subtitleStyle)
    {
        var fontName = subtitleStyle.FontName
            .Replace("'", "", StringComparison.Ordinal)
            .Replace(",", " ", StringComparison.Ordinal)
            .Trim();
        var style = string.Join(",",
        [
            $"FontName={fontName}",
            $"FontSize={subtitleStyle.FontSize}",
            $"PrimaryColour={HardSubtitleStyle.ToAssColor(subtitleStyle.TextColor, subtitleStyle.TextOpacity)}",
            $"OutlineColour={HardSubtitleStyle.ToAssColor(subtitleStyle.OutlineColor, 100)}",
            "BorderStyle=1",
            $"Outline={subtitleStyle.Outline}",
            "Shadow=0",
            $"MarginV={subtitleStyle.MarginV}",
            "Alignment=2"
        ]);
        var arguments = new List<string>
        {
            "-hide_banner", "-nostdin", "-y",
            "-i", sourcePath,
            "-map", "0:v:0",
            "-map", "0:a?",
            "-vf", $"subtitles=filename='captions.srt':charenc=UTF-8:force_style='{style}'",
            "-c:v", videoEncoder
        };
        if (videoEncoder == "h264_nvenc")
        {
            arguments.AddRange(
            [
                "-preset", options.Preset,
                "-tune", "hq",
                "-rc", "vbr",
                "-cq", options.Quality.ToString(CultureInfo.InvariantCulture),
                "-b:v", "0"
            ]);
        }
        else
        {
            arguments.AddRange(
            [
                "-preset", options.CpuPreset,
                "-crf", options.Quality.ToString(CultureInfo.InvariantCulture)
            ]);
        }
        arguments.AddRange(
        [
            "-profile:v", "high",
            "-pix_fmt", "yuv420p",
            "-c:a", "aac",
            "-b:a", $"{options.AudioBitrateKbps}k",
            "-movflags", "+faststart",
            "-max_muxing_queue_size", "1024",
            "-progress", "pipe:1",
            "-stats_period", "1",
            "-nostats",
            outputPath
        ]);
        return arguments;
    }

    private static bool ShouldFallbackToCpu(
        string videoEncoder,
        HardSubtitleProcessResult processResult)
    {
        if (videoEncoder != "h264_nvenc" || processResult.ExitCode == 0)
        {
            return false;
        }

        var error = processResult.Error;
        return error.Contains("Cannot load nvcuda", StringComparison.OrdinalIgnoreCase)
            || error.Contains("Cannot init CUDA", StringComparison.OrdinalIgnoreCase)
            || error.Contains("CUDA_ERROR_NO_DEVICE", StringComparison.OrdinalIgnoreCase)
            || error.Contains("No capable devices found", StringComparison.OrdinalIgnoreCase)
            || error.Contains("OpenEncodeSessionEx failed", StringComparison.OrdinalIgnoreCase)
            || error.Contains("Failed to initialize NVENC", StringComparison.OrdinalIgnoreCase)
            || error.Contains("Driver does not support the required nvenc API version", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryGetProgressSeconds(string line, out double seconds)
    {
        seconds = 0;
        var separator = line.IndexOf('=');
        if (separator <= 0)
        {
            return false;
        }
        var key = line[..separator];
        if (key is not ("out_time_us" or "out_time_ms"))
        {
            return false;
        }
        return long.TryParse(
            line[(separator + 1)..],
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var microseconds)
            && (seconds = microseconds / 1_000_000d) >= 0;
    }

    private static string BuildFfmpegError(string error)
    {
        var detail = string.IsNullOrWhiteSpace(error) ? "没有返回错误详情。" : error.Trim();
        if (detail.Length > 1200)
        {
            detail = detail[^1200..];
        }
        return $"FFmpeg 硬字幕生成失败：{detail}";
    }

    private static void KillProcess(Process process)
    {
        if (process.HasExited)
        {
            return;
        }
        process.Kill(entireProcessTree: true);
        process.WaitForExit();
    }

    private static void RecreateDirectory(string path)
    {
        DeleteDirectoryIfPresent(path);
        Directory.CreateDirectory(path);
    }

    private static void DeleteDirectoryIfPresent(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static void DeleteFileIfPresent(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static void RestoreDatabaseValues(Video video, HardSubtitleDatabaseValues values)
    {
        video.HardSubtitleRelativePath = values.RelativePath;
        video.HardSubtitleTrackId = values.TrackId;
        video.HardSubtitleTrackUpdatedAt = values.TrackUpdatedAt;
        video.HardSubtitleSize = values.Size;
        video.HardSubtitleCreatedAt = values.CreatedAt;
        video.HardSubtitleStyleJson = values.StyleJson;
    }

    private sealed record HardSubtitleDatabaseValues(
        string? RelativePath,
        string? TrackId,
        DateTime? TrackUpdatedAt,
        long? Size,
        DateTime? CreatedAt,
        string? StyleJson);
    private sealed record HardSubtitleProcessResult(int ExitCode, string Error);
}

public sealed record HardSubtitleOptions(
    string VideoEncoder,
    string Preset,
    string CpuPreset,
    int Quality,
    int AudioBitrateKbps,
    HardSubtitleStyle DefaultStyle,
    TimeSpan Timeout)
{
    public static HardSubtitleOptions FromConfiguration(IConfiguration configuration)
    {
        var encoder = configuration["HardSubtitle:VideoEncoder"]?.Trim().ToLowerInvariant();
        encoder = encoder is "libx264" ? "libx264" : "h264_nvenc";
        var configuredPreset = configuration["HardSubtitle:Preset"]?.Trim();
        var preset = string.IsNullOrWhiteSpace(configuredPreset)
            ? encoder == "h264_nvenc" ? "p6" : "medium"
            : configuredPreset;
        var configuredCpuPreset = configuration["HardSubtitle:CpuPreset"]?.Trim();
        var cpuPreset = string.IsNullOrWhiteSpace(configuredCpuPreset)
            ? "fast"
            : configuredCpuPreset;
        var fontName = configuration["HardSubtitle:FontName"]?
            .Replace("'", "", StringComparison.Ordinal)
            .Replace(",", " ", StringComparison.Ordinal)
            .Trim();
        if (fontName?.Length > 80)
        {
            fontName = fontName[..80];
        }
        var defaultStyle = new HardSubtitleStyle(
            string.IsNullOrWhiteSpace(fontName) ? "Microsoft YaHei" : fontName,
            Math.Clamp(configuration.GetValue("HardSubtitle:FontSize", 18), 12, 48),
            HardSubtitleStyle.NormalizeColor(
                configuration["HardSubtitle:TextColor"],
                "#FFFFFF"),
            Math.Clamp(configuration.GetValue("HardSubtitle:TextOpacity", 92), 20, 100),
            HardSubtitleStyle.NormalizeColor(
                configuration["HardSubtitle:OutlineColor"],
                "#000000"),
            Math.Clamp(configuration.GetValue("HardSubtitle:Outline", 2), 0, 6),
            Math.Clamp(configuration.GetValue("HardSubtitle:MarginV", 24), 0, 160));
        return new HardSubtitleOptions(
            encoder,
            preset,
            cpuPreset,
            Math.Clamp(configuration.GetValue("HardSubtitle:Quality", 20), 0, 51),
            Math.Clamp(configuration.GetValue("HardSubtitle:AudioBitrateKbps", 192), 64, 512),
            defaultStyle,
            TimeSpan.FromHours(Math.Clamp(configuration.GetValue("HardSubtitle:TimeoutHours", 24), 1, 168)));
    }
}

public sealed record HardSubtitleStyle(
    string FontName,
    int FontSize,
    string TextColor,
    int TextOpacity,
    string OutlineColor,
    int Outline,
    int MarginV)
{
    public static HardSubtitleStyle Normalize(
        HardSubtitleStyle? value,
        HardSubtitleStyle defaults)
    {
        if (value is null)
        {
            return defaults;
        }

        var fontName = value.FontName?.Trim() ?? "";
        if (fontName.Length is < 1 or > 80
            || fontName.Contains(',', StringComparison.Ordinal)
            || fontName.Contains('\'', StringComparison.Ordinal))
        {
            throw new ArgumentException("字体名称需为 1 到 80 个字符，且不能包含逗号或单引号。");
        }
        if (value.FontSize is < 12 or > 48)
        {
            throw new ArgumentException("字体大小需在 12 到 48 之间。");
        }
        if (value.TextOpacity is < 20 or > 100)
        {
            throw new ArgumentException("字体不透明度需在 20% 到 100% 之间。");
        }
        if (value.Outline is < 0 or > 6)
        {
            throw new ArgumentException("描边宽度需在 0 到 6 之间。");
        }
        if (value.MarginV is < 0 or > 160)
        {
            throw new ArgumentException("底部间距需在 0 到 160 之间。");
        }

        return new HardSubtitleStyle(
            fontName,
            value.FontSize,
            NormalizeColor(value.TextColor, null),
            value.TextOpacity,
            NormalizeColor(value.OutlineColor, null),
            value.Outline,
            value.MarginV);
    }

    public static string NormalizeColor(string? value, string? fallback)
    {
        var color = value?.Trim() ?? fallback ?? "";
        if (color.Length != 7
            || color[0] != '#'
            || !int.TryParse(color.AsSpan(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _))
        {
            if (fallback is not null)
            {
                return fallback;
            }
            throw new ArgumentException("颜色必须使用 #RRGGBB 格式。");
        }
        return color.ToUpperInvariant();
    }

    public static string ToAssColor(string color, int opacity)
    {
        var normalized = NormalizeColor(color, null);
        var alpha = (int)Math.Round((100 - Math.Clamp(opacity, 0, 100)) * 255d / 100d);
        var red = normalized.Substring(1, 2);
        var green = normalized.Substring(3, 2);
        var blue = normalized.Substring(5, 2);
        return $"&H{alpha:X2}{blue}{green}{red}";
    }
}

public sealed class HardSubtitleTestHook(
    IHostEnvironment environment,
    IConfiguration configuration)
{
    private int nvencUnavailableOnce =
        environment.IsEnvironment("Testing")
        && configuration.GetValue("Testing:HardSubtitleNvencUnavailableOnce", false)
            ? 1
            : 0;

    public bool ConsumeNvencUnavailable(string videoEncoder)
    {
        return videoEncoder == "h264_nvenc"
            && Interlocked.Exchange(ref nvencUnavailableOnce, 0) == 1;
    }
}

public sealed record HardSubtitleRequest(
    string? SubtitleTrackId,
    HardSubtitleStyle? Style = null);
public sealed record DeleteOriginalRequest(string? ConfirmNumber);
public sealed record HardSubtitleJobInput(
    string VideoId,
    string SubtitleTrackId,
    HardSubtitleStyle? Style = null);
public sealed record HardSubtitleJobResult(
    string VideoId,
    string SubtitleTrackId,
    string SubtitleLabel,
    string RelativePath,
    long Size,
    DateTime CreatedAt,
    string Encoder,
    HardSubtitleStyle Style);

public sealed record HardSubtitleOptionsItem(HardSubtitleStyle DefaultStyle);
