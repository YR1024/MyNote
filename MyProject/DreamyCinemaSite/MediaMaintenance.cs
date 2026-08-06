using System.Text.Json;
using Microsoft.EntityFrameworkCore;

public sealed class MediaMaintenanceService(
    CinemaDbContext db,
    VideoStorage storage,
    MediaAnalyzer mediaAnalyzer,
    MediaMaintenanceTestHook testHook)
{
    public async Task<string> RunAsync(
        string jobId,
        CancellationToken cancellationToken,
        Func<JobProgress, Task> reportProgress)
    {
        var job = await db.MediaJobs
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == jobId, cancellationToken)
            ?? throw new InvalidOperationException("媒体维护任务不存在。");
        var input = JsonSerializer.Deserialize<MediaMaintenanceJobInput>(
            job.InputJson ?? "",
            JsonSerializerOptions.Web)
            ?? throw new InvalidOperationException("媒体维护任务参数无效。");

        if (!MediaMaintenanceAction.IsSupported(input.Action))
        {
            throw new InvalidOperationException("媒体维护任务操作无效。");
        }

        var requestedIds = input.VideoIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        var videos = await db.Videos
            .Where(video => requestedIds.Contains(video.Id))
            .ToDictionaryAsync(video => video.Id, cancellationToken);
        var issues = new List<MediaMaintenanceIssue>();
        var processedCount = 0;
        var updatedCount = 0;

        await reportProgress(new JobProgress(
            1,
            input.Action == MediaMaintenanceAction.Reanalyze ? "准备重新分析" : "准备重新生成封面",
            null));

        for (var index = 0; index < requestedIds.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var videoId = requestedIds[index];
            if (!videos.TryGetValue(videoId, out var video) || video.Status != VideoStatus.Available)
            {
                issues.Add(new MediaMaintenanceIssue(videoId, null, "视频不存在或当前不可用。"));
                processedCount++;
                await ReportItemProgressAsync(input.Action, requestedIds.Count, processedCount, null, reportProgress);
                continue;
            }

            var videoPath = storage.GetAbsolutePath(video.RelativePath);
            if (!File.Exists(videoPath))
            {
                video.OriginalAvailable = false;
                var hardSubtitleExists = !string.IsNullOrWhiteSpace(video.HardSubtitleRelativePath)
                    && File.Exists(storage.GetAbsolutePath(video.HardSubtitleRelativePath));
                video.Status = hardSubtitleExists ? VideoStatus.Available : VideoStatus.Missing;
                video.UpdatedAt = DateTime.Now;
                await db.SaveChangesAsync(cancellationToken);
                issues.Add(new MediaMaintenanceIssue(video.Id, video.OriginalFileName, "原视频文件不存在。"));
                processedCount++;
                await ReportItemProgressAsync(input.Action, requestedIds.Count, processedCount, video.OriginalFileName, reportProgress);
                continue;
            }

            if (input.Action == MediaMaintenanceAction.Reanalyze)
            {
                testHook.ThrowOnceDuringReanalysis();
                var analysis = await mediaAnalyzer.AnalyzeAsync(videoPath, cancellationToken);
                if (analysis.Warning is null)
                {
                    video.Size = new FileInfo(videoPath).Length;
                    video.DurationSeconds = analysis.DurationSeconds;
                    video.Width = analysis.Width;
                    video.Height = analysis.Height;
                    video.VideoCodec = analysis.VideoCodec;
                    video.UpdatedAt = DateTime.Now;
                    await db.SaveChangesAsync(cancellationToken);
                    updatedCount++;
                }
                else
                {
                    issues.Add(new MediaMaintenanceIssue(video.Id, video.OriginalFileName, analysis.Warning));
                }
            }
            else
            {
                var now = DateTime.Now;
                var replacementRelativePath = Path.Combine(
                    "covers",
                    now.Year.ToString("0000"),
                    now.Month.ToString("00"),
                    $"{video.Id}-auto-{Guid.NewGuid():N}.jpg");
                var replacementPath = storage.GetAbsolutePath(replacementRelativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(replacementPath)!);

                var warning = await mediaAnalyzer.GenerateCoverAsync(
                    videoPath,
                    replacementPath,
                    video.DurationSeconds,
                    cancellationToken);
                if (warning is not null)
                {
                    DeleteFileIfPresent(replacementPath);
                    issues.Add(new MediaMaintenanceIssue(video.Id, video.OriginalFileName, warning));
                }
                else
                {
                    var previousRelativePath = video.CoverRelativePath;
                    video.CoverRelativePath = replacementRelativePath;
                    video.UpdatedAt = now;
                    try
                    {
                        await db.SaveChangesAsync(cancellationToken);
                    }
                    catch
                    {
                        video.CoverRelativePath = previousRelativePath;
                        DeleteFileIfPresent(replacementPath);
                        throw;
                    }

                    if (!string.IsNullOrWhiteSpace(previousRelativePath)
                        && !string.Equals(previousRelativePath, replacementRelativePath, StringComparison.OrdinalIgnoreCase))
                    {
                        DeleteFileIfPresent(storage.GetAbsolutePath(previousRelativePath));
                    }
                    updatedCount++;
                }
            }

            processedCount++;
            await ReportItemProgressAsync(
                input.Action,
                requestedIds.Count,
                processedCount,
                video.OriginalFileName,
                reportProgress);
        }

        var result = new MediaMaintenanceResult(
            input.Action,
            requestedIds.Count,
            processedCount,
            updatedCount,
            issues);
        return JsonSerializer.Serialize(result, JsonSerializerOptions.Web);
    }

    private static Task ReportItemProgressAsync(
        string action,
        int totalCount,
        int processedCount,
        string? currentItem,
        Func<JobProgress, Task> reportProgress)
    {
        var percent = totalCount == 0
            ? 99
            : Math.Min(99, Math.Max(1, (int)Math.Round(processedCount * 99d / totalCount)));
        var stage = action == MediaMaintenanceAction.Reanalyze ? "正在重新分析" : "正在重新生成封面";
        return reportProgress(new JobProgress(percent, stage, currentItem));
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
}

public sealed class MediaMaintenanceTestHook(IHostEnvironment environment, IConfiguration configuration)
{
    private int failOnceDuringReanalysis =
        environment.IsEnvironment("Testing")
        && configuration.GetValue("Testing:MediaMaintenanceFailOnceDuringReanalysis", false)
            ? 1
            : 0;

    public void ThrowOnceDuringReanalysis()
    {
        if (Interlocked.Exchange(ref failOnceDuringReanalysis, 0) == 1)
        {
            throw new InvalidOperationException("测试注入：模拟媒体重新分析失败。");
        }
    }
}
