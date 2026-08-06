using System.Text.Json;

public sealed class SecurityAuditStore(string path, ILogger<SecurityAuditStore> logger)
{
    private const long MaximumFileBytes = 2L * 1024 * 1024;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerOptions.Web);

    public async Task RecordWriteAsync(HttpContext context, int statusCode)
    {
        var request = context.Request;
        var entry = new SecurityAuditEntry(
            Guid.NewGuid().ToString("N"),
            DateTimeOffset.UtcNow,
            ClassifyAction(request.Path),
            request.Method,
            request.Path.Value ?? "/api",
            statusCode,
            ClassifyOutcome(statusCode),
            context.Connection.RemoteIpAddress?.ToString());

        await _gate.WaitAsync();
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            RotateIfNeeded();
            await File.AppendAllTextAsync(
                path,
                JsonSerializer.Serialize(entry, _jsonOptions) + Environment.NewLine);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            logger.LogError(ex, "Unable to append the security audit log.");
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<SecurityAuditEntry>> ReadLatestAsync(
        int take,
        CancellationToken cancellationToken)
    {
        take = Math.Clamp(take, 1, 100);
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var entries = new List<SecurityAuditEntry>();
            var previousPath = path + ".previous";
            if (File.Exists(previousPath))
            {
                await ReadEntriesAsync(previousPath, entries, cancellationToken);
            }
            if (File.Exists(path))
            {
                await ReadEntriesAsync(path, entries, cancellationToken);
            }

            return entries
                .OrderByDescending(entry => entry.CreatedAt)
                .Take(take)
                .ToArray();
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task ReadEntriesAsync(
        string sourcePath,
        ICollection<SecurityAuditEntry> entries,
        CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(
            sourcePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite,
            bufferSize: 16 * 1024,
            useAsync: true);
        using var reader = new StreamReader(stream);
        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            try
            {
                var entry = JsonSerializer.Deserialize<SecurityAuditEntry>(line, _jsonOptions);
                if (entry is not null)
                {
                    entries.Add(entry);
                }
            }
            catch (JsonException ex)
            {
                logger.LogWarning(ex, "Skipped an invalid security audit entry.");
            }
        }
    }

    private void RotateIfNeeded()
    {
        if (!File.Exists(path) || new FileInfo(path).Length < MaximumFileBytes)
        {
            return;
        }

        File.Move(path, path + ".previous", overwrite: true);
    }

    private static string ClassifyAction(PathString path)
    {
        var value = path.Value ?? "";
        if (value.Equals("/api/auth/setup", StringComparison.OrdinalIgnoreCase))
        {
            return "auth.setup";
        }
        if (value.Equals("/api/auth/login", StringComparison.OrdinalIgnoreCase))
        {
            return "auth.login";
        }
        if (value.Equals("/api/auth/logout", StringComparison.OrdinalIgnoreCase))
        {
            return "auth.logout";
        }
        if (value.Equals("/api/auth/change-password", StringComparison.OrdinalIgnoreCase))
        {
            return "auth.password.change";
        }
        if (value.StartsWith("/api/backups", StringComparison.OrdinalIgnoreCase))
        {
            return value.EndsWith("/restore", StringComparison.OrdinalIgnoreCase)
                ? "backup.restore"
                : "backup.manage";
        }
        if (value.Equals("/api/videos/sync", StringComparison.OrdinalIgnoreCase))
        {
            return "library.sync";
        }
        if (value.StartsWith("/api/tags", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("/api/tag-categories", StringComparison.OrdinalIgnoreCase))
        {
            return "tags.manage";
        }
        if (value.StartsWith("/api/jobs", StringComparison.OrdinalIgnoreCase))
        {
            return "jobs.manage";
        }
        if (value.StartsWith("/api/videos", StringComparison.OrdinalIgnoreCase))
        {
            return "videos.manage";
        }

        return "api.write";
    }

    private static string ClassifyOutcome(int statusCode) => statusCode switch
    {
        >= 200 and < 300 => "Succeeded",
        401 or 403 => "Denied",
        >= 400 and < 500 => "Rejected",
        _ => "Failed"
    };
}

public sealed record SecurityAuditEntry(
    string Id,
    DateTimeOffset CreatedAt,
    string Action,
    string Method,
    string Path,
    int StatusCode,
    string Outcome,
    string? RemoteAddress);
