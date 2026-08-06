using Microsoft.Data.Sqlite;
using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

public sealed class BackupService(
    BackupStorage backupStorage,
    DatabaseLocation databaseLocation,
    VideoStorage videoStorage,
    AiSubtitleOptions aiOptions,
    BackupOperationGate operationGate,
    ILogger<BackupService> logger)
{
    public const int CurrentFormatVersion = 1;
    private const string ManifestEntryName = "manifest.json";
    private const string DatabaseEntryName = "database/dreamy-cinema.db";
    private const string PortableSettingsEntryName = "configuration/portable-settings.json";
    private static readonly Regex BackupIdPattern = new("^[0-9a-f]{32}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerOptions.Web)
    {
        WriteIndented = true
    };
    public async Task<IReadOnlyList<BackupPackageItem>> ListAsync(CancellationToken cancellationToken)
    {
        var files = Directory
            .EnumerateFiles(backupStorage.RootPath, "dreamy-cinema-*.zip", SearchOption.TopDirectoryOnly)
            .OrderByDescending(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var results = new List<BackupPackageItem>(files.Length);
        foreach (var path in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            results.Add(await ReadPackageItemAsync(path, verifyEntries: false, cancellationToken));
        }

        return results
            .OrderByDescending(item => item.CreatedAt)
            .ToArray();
    }

    public async Task<BackupPackageItem> CreateAsync(
        CancellationToken cancellationToken,
        string purpose = BackupPurpose.Manual)
    {
        if (!await operationGate.Gate.WaitAsync(0, cancellationToken))
        {
            throw new BackupBusyException("已有备份正在创建，请稍后再试。");
        }

        var backupId = Guid.NewGuid().ToString("N");
        var createdAt = DateTimeOffset.UtcNow;
        var timestamp = createdAt.ToString("yyyyMMdd'T'HHmmss'Z'");
        var filePrefix = purpose == BackupPurpose.PreRestore ? "dreamy-cinema-prerestore" : "dreamy-cinema";
        var fileName = $"{filePrefix}-{timestamp}-{backupId}.zip";
        var finalPath = Path.Combine(backupStorage.RootPath, fileName);
        var partialPath = finalPath + ".partial";
        var stagingPath = Path.Combine(backupStorage.RootPath, $".working-{backupId}");

        try
        {
            Directory.CreateDirectory(stagingPath);
            var warnings = new List<string>();
            var copiedAssets = new List<CopiedAsset>();
            var snapshotPath = Path.Combine(stagingPath, DatabaseEntryName.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(snapshotPath)!);

            await using (var source = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = databaseLocation.Path,
                Mode = SqliteOpenMode.ReadOnly,
                Pooling = false
            }.ToString()))
            {
                await source.OpenAsync(cancellationToken);
                await using var readTransaction = await source.BeginTransactionAsync(cancellationToken);
                var assetReferences = await ReadAssetReferencesAsync(source, (SqliteTransaction)readTransaction, cancellationToken);

                foreach (var asset in assetReferences)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var copied = await CopyAssetAsync(stagingPath, asset, warnings, cancellationToken);
                    if (copied is not null)
                    {
                        copiedAssets.Add(copied);
                    }
                }

                await using var destination = new SqliteConnection(new SqliteConnectionStringBuilder
                {
                    DataSource = snapshotPath,
                    Mode = SqliteOpenMode.ReadWriteCreate,
                    Pooling = false
                }.ToString());
                await destination.OpenAsync(cancellationToken);
                source.BackupDatabase(destination);
                await using (var journalMode = destination.CreateCommand())
                {
                    journalMode.CommandText = "PRAGMA journal_mode = DELETE;";
                    var mode = Convert.ToString(await journalMode.ExecuteScalarAsync(cancellationToken));
                    if (!string.Equals(mode, "delete", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidDataException($"无法将备份快照切换为单文件日志模式：{mode}");
                    }
                }
                await readTransaction.CommitAsync(cancellationToken);
            }

            var databaseSummary = await ValidateAndSummarizeDatabaseAsync(snapshotPath, cancellationToken);
            DeleteSnapshotSidecars(snapshotPath);
            var portableSettings = PortableBackupSettings.From(aiOptions);
            var portableSettingsPath = Path.Combine(
                stagingPath,
                PortableSettingsEntryName.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(portableSettingsPath)!);
            await File.WriteAllTextAsync(
                portableSettingsPath,
                JsonSerializer.Serialize(portableSettings, JsonOptions),
                cancellationToken);

            var fileEntries = await BuildFileEntriesAsync(stagingPath, cancellationToken);
            var manifest = new BackupManifest(
                CurrentFormatVersion,
                backupId,
                createdAt,
                GetApplicationVersion(),
                IncludesOriginalVideos: false,
                DatabaseEntryName,
                PortableSettingsEntryName,
                databaseSummary,
                new BackupAssetSummary(
                    copiedAssets.Count(asset => asset.Kind == BackupAssetKind.Cover),
                    copiedAssets.Count(asset => asset.Kind == BackupAssetKind.Subtitle)),
                fileEntries,
                warnings,
                purpose);
            await File.WriteAllTextAsync(
                Path.Combine(stagingPath, ManifestEntryName),
                JsonSerializer.Serialize(manifest, JsonOptions),
                cancellationToken);

            ZipFile.CreateFromDirectory(stagingPath, partialPath, CompressionLevel.Optimal, includeBaseDirectory: false);
            File.Move(partialPath, finalPath);

            var created = await ReadPackageItemAsync(finalPath, verifyEntries: true, cancellationToken);
            if (!created.IsValid)
            {
                throw new InvalidDataException(created.ValidationError ?? "备份包校验失败。");
            }

            logger.LogInformation(
                "Created backup {BackupId} with {VideoCount} videos, {CoverCount} covers, and {SubtitleCount} subtitle files.",
                backupId,
                databaseSummary.VideoCount,
                manifest.Assets.CoverCount,
                manifest.Assets.SubtitleFileCount);
            return created;
        }
        catch
        {
            TryDeleteFile(partialPath);
            TryDeleteFile(finalPath);
            throw;
        }
        finally
        {
            TryDeleteDirectory(stagingPath);
            operationGate.Gate.Release();
        }
    }

    public string? FindPackagePath(string backupId)
    {
        if (!BackupIdPattern.IsMatch(backupId))
        {
            return null;
        }

        return Directory
            .EnumerateFiles(backupStorage.RootPath, $"dreamy-cinema-*-{backupId}.zip", SearchOption.TopDirectoryOnly)
            .SingleOrDefault();
    }

    public async Task<bool> DeleteAsync(string backupId, CancellationToken cancellationToken)
    {
        if (!await operationGate.Gate.WaitAsync(0, cancellationToken))
        {
            throw new BackupBusyException("备份正在创建，暂时不能删除。");
        }

        try
        {
            var path = FindPackagePath(backupId);
            if (path is null)
            {
                return false;
            }

            File.Delete(path);
            logger.LogInformation("Deleted backup {BackupId}.", backupId);
            return true;
        }
        finally
        {
            operationGate.Gate.Release();
        }
    }

    private async Task<BackupPackageItem> ReadPackageItemAsync(
        string packagePath,
        bool verifyEntries,
        CancellationToken cancellationToken)
    {
        var file = new FileInfo(packagePath);
        try
        {
            using var archive = ZipFile.OpenRead(packagePath);
            var manifestEntry = archive.GetEntry(ManifestEntryName)
                ?? throw new InvalidDataException("备份包缺少 manifest.json。");
            await using var stream = manifestEntry.Open();
            var manifest = await JsonSerializer.DeserializeAsync<BackupManifest>(stream, JsonOptions, cancellationToken)
                ?? throw new InvalidDataException("备份清单无法读取。");
            if (manifest.FormatVersion != CurrentFormatVersion)
            {
                throw new InvalidDataException($"不支持的备份格式版本：{manifest.FormatVersion}。");
            }

            if (verifyEntries)
            {
                await VerifyEntriesAsync(archive, manifest, cancellationToken);
            }

            return new BackupPackageItem(
                manifest.BackupId,
                file.Name,
                manifest.CreatedAt,
                file.Length,
                await ComputeFileHashAsync(packagePath, cancellationToken),
                manifest.FormatVersion,
                manifest.Database.VideoCount,
                manifest.Assets.CoverCount,
                manifest.Assets.SubtitleFileCount,
                manifest.IncludesOriginalVideos,
                manifest.Warnings,
                file.Name.StartsWith("dreamy-cinema-imported-", StringComparison.OrdinalIgnoreCase)
                    ? BackupPurpose.Imported
                    : manifest.Purpose ?? BackupPurpose.Manual,
                IsValid: true,
                ValidationError: null);
        }
        catch (Exception ex) when (ex is InvalidDataException or IOException or JsonException or CryptographicException)
        {
            logger.LogWarning(ex, "Could not read backup package {BackupFile}.", file.Name);
            return new BackupPackageItem(
                ExtractBackupId(file.Name),
                file.Name,
                file.CreationTimeUtc,
                file.Exists ? file.Length : 0,
                "",
                0,
                0,
                0,
                0,
                IncludesOriginalVideos: false,
                [],
                BackupPurpose.Unknown,
                IsValid: false,
                ValidationError: ex.Message);
        }
    }

    private static async Task VerifyEntriesAsync(
        ZipArchive archive,
        BackupManifest manifest,
        CancellationToken cancellationToken)
    {
        foreach (var expected in manifest.Files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var entry = archive.GetEntry(expected.Path)
                ?? throw new InvalidDataException($"备份包缺少文件：{expected.Path}。");
            if (entry.Length != expected.Size)
            {
                throw new InvalidDataException($"备份文件大小不匹配：{expected.Path}。");
            }

            await using var stream = entry.Open();
            var hash = await ComputeHashAsync(stream, cancellationToken);
            if (!hash.Equals(expected.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException($"备份文件校验失败：{expected.Path}。");
            }
        }

        if (archive.GetEntry(manifest.DatabasePath) is null)
        {
            throw new InvalidDataException("备份包缺少数据库快照。");
        }

        if (archive.Entries.Any(entry =>
                entry.FullName.StartsWith("assets/originals/", StringComparison.OrdinalIgnoreCase)
                || entry.FullName.StartsWith("secrets/", StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidDataException("备份包包含禁止打包的原视频或敏感文件。");
        }
    }

    private static async Task<IReadOnlyList<BackupAssetReference>> ReadAssetReferencesAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        CancellationToken cancellationToken)
    {
        var results = new List<BackupAssetReference>();
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT 'cover' AS Kind, "CoverRelativePath" AS RelativePath
            FROM "Videos"
            WHERE "CoverRelativePath" IS NOT NULL AND trim("CoverRelativePath") <> ''
            UNION
            SELECT 'subtitle' AS Kind, "OriginalRelativePath" AS RelativePath
            FROM "SubtitleTracks"
            WHERE "OriginalRelativePath" IS NOT NULL AND trim("OriginalRelativePath") <> ''
            ORDER BY Kind, RelativePath;
            """;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new BackupAssetReference(
                reader.GetString(0) == "cover" ? BackupAssetKind.Cover : BackupAssetKind.Subtitle,
                reader.GetString(1)));
        }

        return results;
    }

    private async Task<CopiedAsset?> CopyAssetAsync(
        string stagingPath,
        BackupAssetReference asset,
        List<string> warnings,
        CancellationToken cancellationToken)
    {
        var normalized = NormalizeManagedAssetPath(asset);
        if (normalized is null)
        {
            warnings.Add($"已跳过不安全的{AssetLabel(asset.Kind)}路径：{asset.RelativePath}");
            return null;
        }

        var sourcePath = videoStorage.GetAbsolutePath(normalized);
        if (!File.Exists(sourcePath))
        {
            warnings.Add($"{AssetLabel(asset.Kind)}文件缺失：{normalized.Replace('\\', '/')}");
            return null;
        }

        var entryPath = $"assets/{normalized.Replace('\\', '/')}";
        var destinationPath = Path.Combine(stagingPath, entryPath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
        await using var source = new FileStream(
            sourcePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 1024 * 128,
            useAsync: true);
        await using var destination = new FileStream(
            destinationPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 1024 * 128,
            useAsync: true);
        await source.CopyToAsync(destination, cancellationToken);
        return new CopiedAsset(asset.Kind, entryPath);
    }

    private static string? NormalizeManagedAssetPath(BackupAssetReference asset)
    {
        if (Path.IsPathRooted(asset.RelativePath))
        {
            return null;
        }

        var normalized = asset.RelativePath
            .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar)
            .TrimStart(Path.DirectorySeparatorChar);
        var expectedPrefix = asset.Kind == BackupAssetKind.Cover ? "covers" : "subtitles";
        var firstSegment = normalized.Split(Path.DirectorySeparatorChar, 2)[0];
        if (!firstSegment.Equals(expectedPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var segments = normalized.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
        return segments.Any(segment => segment is "." or "..") ? null : Path.Combine(segments);
    }

    private static async Task<BackupDatabaseSummary> ValidateAndSummarizeDatabaseAsync(
        string snapshotPath,
        CancellationToken cancellationToken)
    {
        await using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = snapshotPath,
            Mode = SqliteOpenMode.ReadOnly,
            Pooling = false
        }.ToString());
        await connection.OpenAsync(cancellationToken);

        await using (var integrity = connection.CreateCommand())
        {
            integrity.CommandText = "PRAGMA integrity_check;";
            var result = Convert.ToString(await integrity.ExecuteScalarAsync(cancellationToken));
            if (!string.Equals(result, "ok", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException($"SQLite 快照完整性检查失败：{result}");
            }
        }

        return new BackupDatabaseSummary(
            await CountRowsAsync(connection, "Videos", cancellationToken),
            await CountRowsAsync(connection, "TagCategories", cancellationToken),
            await CountRowsAsync(connection, "Tags", cancellationToken),
            await CountRowsAsync(connection, "SubtitleTracks", cancellationToken),
            await CountRowsAsync(connection, "SubtitleCues", cancellationToken));
    }

    private static async Task<long> CountRowsAsync(
        SqliteConnection connection,
        string table,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM \"{table}\";";
        return Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken));
    }

    private static async Task<IReadOnlyList<BackupFileEntry>> BuildFileEntriesAsync(
        string stagingPath,
        CancellationToken cancellationToken)
    {
        var results = new List<BackupFileEntry>();
        foreach (var path in Directory.EnumerateFiles(stagingPath, "*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var relativePath = Path.GetRelativePath(stagingPath, path).Replace('\\', '/');
            if (relativePath.Equals(ManifestEntryName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var file = new FileInfo(path);
            results.Add(new BackupFileEntry(
                relativePath,
                file.Length,
                await ComputeFileHashAsync(path, cancellationToken)));
        }

        return results.OrderBy(item => item.Path, StringComparer.Ordinal).ToArray();
    }

    private static async Task<string> ComputeFileHashAsync(string path, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 1024 * 128,
            useAsync: true);
        return await ComputeHashAsync(stream, cancellationToken);
    }

    private static async Task<string> ComputeHashAsync(Stream stream, CancellationToken cancellationToken)
    {
        var hash = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string GetApplicationVersion() =>
        typeof(BackupService).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion
        ?? typeof(BackupService).Assembly.GetName().Version?.ToString()
        ?? "unknown";

    private static string ExtractBackupId(string fileName)
    {
        var withoutExtension = Path.GetFileNameWithoutExtension(fileName);
        var candidate = withoutExtension.Split('-').LastOrDefault() ?? "";
        return BackupIdPattern.IsMatch(candidate) ? candidate : "";
    }

    private static string AssetLabel(BackupAssetKind kind) =>
        kind == BackupAssetKind.Cover ? "封面" : "字幕";

    private static void DeleteSnapshotSidecars(string snapshotPath)
    {
        foreach (var suffix in new[] { "-wal", "-shm", "-journal" })
        {
            var path = snapshotPath + suffix;
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // A failed cleanup must not hide the original backup error.
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch
        {
            // A failed cleanup must not hide the original backup error.
        }
    }
}

public sealed record BackupStorage(string RootPath, long MaxUploadBytes, long MaxExpandedBytes)
{
    public static BackupStorage FromConfiguration(IConfiguration configuration, string contentRootPath)
    {
        var configuredPath = configuration["Backup:Path"];
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            configuredPath = Path.Combine("Data", "backups");
        }

        var rootPath = Path.IsPathRooted(configuredPath)
            ? Path.GetFullPath(configuredPath)
            : Path.GetFullPath(Path.Combine(contentRootPath, configuredPath));
        var maxUploadBytes = Math.Clamp(
            configuration.GetValue("Backup:MaxUploadBytes", 2L * 1024 * 1024 * 1024),
            1024 * 1024,
            4L * 1024 * 1024 * 1024);
        var maxExpandedBytes = Math.Clamp(
            configuration.GetValue("Backup:MaxExpandedBytes", 4L * 1024 * 1024 * 1024),
            maxUploadBytes,
            8L * 1024 * 1024 * 1024);
        return new BackupStorage(rootPath, maxUploadBytes, maxExpandedBytes);
    }
}

public sealed record PortableSettingsLocation(string Path);
public sealed record DatabaseLocation(string Path);
public sealed record BackupPackageItem(
    string Id,
    string FileName,
    DateTimeOffset CreatedAt,
    long Size,
    string Sha256,
    int FormatVersion,
    long VideoCount,
    int CoverCount,
    int SubtitleFileCount,
    bool IncludesOriginalVideos,
    IReadOnlyList<string> Warnings,
    string Purpose,
    bool IsValid,
    string? ValidationError);
public sealed record BackupManifest(
    int FormatVersion,
    string BackupId,
    DateTimeOffset CreatedAt,
    string ApplicationVersion,
    bool IncludesOriginalVideos,
    string DatabasePath,
    string PortableSettingsPath,
    BackupDatabaseSummary Database,
    BackupAssetSummary Assets,
    IReadOnlyList<BackupFileEntry> Files,
    IReadOnlyList<string> Warnings,
    string? Purpose);
public sealed record BackupDatabaseSummary(
    long VideoCount,
    long TagCategoryCount,
    long TagCount,
    long SubtitleTrackCount,
    long SubtitleCueCount);
public sealed record BackupAssetSummary(int CoverCount, int SubtitleFileCount);
public sealed record BackupFileEntry(string Path, long Size, string Sha256);
public sealed record PortableBackupSettings(
    int FormatVersion,
    PortableAiSubtitleSettings AiSubtitles)
{
    public static PortableBackupSettings From(AiSubtitleOptions options) => new(
        BackupService.CurrentFormatVersion,
        new PortableAiSubtitleSettings(
            options.TargetLanguage,
            options.TranslationChunkSize,
            options.ContextCueCount,
            options.ProviderMaxAttempts,
            options.MaxInputCharacters,
            options.MaxTotalTokens,
            options.MaxOutputTokensPerChunk,
            options.SemanticReviewEnabled,
            options.SemanticReviewMaxTokens,
            options.TranslationTemperature,
            options.PreserveExplicitLanguage,
            options.TranslationStyle,
            options.CharacterNames,
            options.Glossary));
}
public sealed record PortableAiSubtitleSettings(
    string TargetLanguage,
    int TranslationChunkSize,
    int ContextCueCount,
    int ProviderMaxAttempts,
    int MaxInputCharacters,
    int MaxTotalTokens,
    int MaxOutputTokensPerChunk,
    bool SemanticReviewEnabled,
    int SemanticReviewMaxTokens,
    double TranslationTemperature,
    bool PreserveExplicitLanguage,
    string TranslationStyle,
    IReadOnlyList<string> CharacterNames,
    IReadOnlyDictionary<string, string> Glossary);
public sealed record BackupAssetReference(BackupAssetKind Kind, string RelativePath);
public sealed record CopiedAsset(BackupAssetKind Kind, string EntryPath);
public enum BackupAssetKind
{
    Cover,
    Subtitle
}
public sealed class BackupBusyException(string message) : InvalidOperationException(message);
public static class BackupPurpose
{
    public const string Manual = "Manual";
    public const string PreRestore = "PreRestore";
    public const string Imported = "Imported";
    public const string Unknown = "Unknown";
}
