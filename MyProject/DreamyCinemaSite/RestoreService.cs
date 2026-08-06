using Microsoft.Data.Sqlite;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

public sealed class RestoreService(
    BackupService backupService,
    BackupStorage backupStorage,
    BackupOperationGate operationGate,
    DatabaseLocation databaseLocation,
    PortableSettingsLocation portableSettingsLocation,
    VideoStorage videoStorage,
    RestoreCoordinator coordinator,
    RestoreTestHook testHook,
    ILogger<RestoreService> logger)
{
    private const string ManifestEntryName = "manifest.json";
    private const string DatabaseEntryName = "database/dreamy-cinema.db";
    private const string PortableSettingsEntryName = "configuration/portable-settings.json";
    private const int MaximumEntryCount = 50_000;
    private const long MaximumManifestBytes = 16L * 1024 * 1024;
    private const long MaximumPortableSettingsBytes = 4L * 1024 * 1024;
    private static readonly Regex BackupIdPattern = new(
        "^[0-9a-f]{32}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerOptions.Web);
    private static readonly IReadOnlyDictionary<string, string[]> RequiredSchema =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["Videos"] =
            [
                "Id", "Number", "Title", "Description", "OriginalFileName", "StoredFileName",
                "RelativePath", "CoverRelativePath", "Size", "Status", "OriginalAvailable",
                "OriginalDeletedAt", "HardSubtitleRelativePath", "HardSubtitleTrackId",
                "HardSubtitleTrackUpdatedAt", "HardSubtitleSize", "HardSubtitleCreatedAt",
                "HardSubtitleStyleJson",
                "ImportedAt", "CreatedAt", "UpdatedAt"
            ],
            ["TagCategories"] = ["Id", "Name", "SortOrder"],
            ["Tags"] = ["Id", "CategoryId", "Name", "SortOrder"],
            ["VideoTags"] = ["VideoId", "TagId"],
            ["SubtitleTracks"] =
            [
                "Id", "VideoId", "Label", "Language", "Kind", "Source", "RevisionStage",
                "SourceKey", "Format", "OriginalRelativePath", "CueCount"
            ],
            ["SubtitleCues"] =
            [
                "Id", "TrackId", "Index", "StartMilliseconds", "EndMilliseconds",
                "Text", "IsManuallyEdited", "UpdatedAt"
            ],
            ["MediaJobs"] = ["Id", "Type", "Status", "CreatedAt", "UpdatedAt"],
            ["AiJobChunks"] = ["Id", "JobId", "Index", "Kind", "Status"]
        };

    public async Task<BackupPackageItem> ImportAsync(
        Stream upload,
        long declaredLength,
        CancellationToken cancellationToken)
    {
        if (declaredLength <= 0 || declaredLength > backupStorage.MaxUploadBytes)
        {
            throw new BackupValidationException(
                $"备份包大小必须在 1 B 到 {FormatBytes(backupStorage.MaxUploadBytes)} 之间。");
        }

        if (!await operationGate.Gate.WaitAsync(0, cancellationToken))
        {
            throw new BackupBusyException("其他备份操作正在执行，请稍后再试。");
        }

        var operationId = Guid.NewGuid().ToString("N");
        var partialPath = Path.Combine(backupStorage.RootPath, $".upload-{operationId}.partial");
        var validationRoot = Path.Combine(backupStorage.RootPath, $".validate-{operationId}");
        try
        {
            await CopyUploadAsync(upload, partialPath, backupStorage.MaxUploadBytes, cancellationToken);
            Directory.CreateDirectory(validationRoot);
            var validated = await ValidatePackageAsync(
                partialPath,
                validationRoot,
                incomingAssetsRoot: null,
                cancellationToken);

            var timestamp = validated.Manifest.CreatedAt.ToUniversalTime().ToString("yyyyMMdd'T'HHmmss'Z'");
            var finalPath = Path.Combine(
                backupStorage.RootPath,
                $"dreamy-cinema-imported-{timestamp}-{validated.Manifest.BackupId}.zip");
            var existingPath = backupService.FindPackagePath(validated.Manifest.BackupId);
            if (existingPath is not null)
            {
                var existingHash = await ComputeFileHashAsync(existingPath, cancellationToken);
                var uploadedHash = await ComputeFileHashAsync(partialPath, cancellationToken);
                if (!existingHash.Equals(uploadedHash, StringComparison.OrdinalIgnoreCase))
                {
                    throw new BackupConflictException("服务器上已存在相同 ID 但内容不同的备份包。");
                }

                return (await backupService.ListAsync(cancellationToken))
                    .First(item => item.Id == validated.Manifest.BackupId);
            }

            File.Move(partialPath, finalPath);
            logger.LogInformation(
                "Imported backup {BackupId} with {VideoCount} video records.",
                validated.Manifest.BackupId,
                validated.Database.VideoCount);
            return (await backupService.ListAsync(cancellationToken))
                .First(item => item.Id == validated.Manifest.BackupId);
        }
        finally
        {
            TryDeleteFile(partialPath);
            TryDeleteDirectory(validationRoot);
            operationGate.Gate.Release();
        }
    }

    public async Task<RestoreResult> RestoreAsync(string backupId, CancellationToken cancellationToken)
    {
        if (!BackupIdPattern.IsMatch(backupId))
        {
            throw new BackupValidationException("备份 ID 格式无效。");
        }

        using var restoreLease = coordinator.TryBeginRestore()
            ?? throw new RestoreBusyException("当前有写操作、同步或 AI 任务正在执行，请等待完成或取消后再恢复。");
        var packagePath = backupService.FindPackagePath(backupId)
            ?? throw new FileNotFoundException("备份包不存在。");
        var operationId = Guid.NewGuid().ToString("N");
        var operationRoot = Path.Combine(backupStorage.RootPath, $".restore-{operationId}");
        var targetValidationRoot = Path.Combine(operationRoot, "target");
        var rollbackValidationRoot = Path.Combine(operationRoot, "rollback");
        var incomingAssetsRoot = Path.Combine(videoStorage.RootPath, $".restore-incoming-{operationId}");
        var rollbackAssetsRoot = Path.Combine(videoStorage.RootPath, $".restore-rollback-{operationId}");
        Directory.CreateDirectory(operationRoot);
        Directory.CreateDirectory(incomingAssetsRoot);
        Directory.CreateDirectory(Path.Combine(incomingAssetsRoot, "covers"));
        Directory.CreateDirectory(Path.Combine(incomingAssetsRoot, "subtitles"));

        BackupPackageItem? safetyBackup = null;
        AssetSwapState? assetSwap = null;
        var databaseApplied = false;
        var portableSettingsApplied = false;
        var previousSettingsPath = Path.Combine(operationRoot, "portable-settings.previous.json");
        var previousSettingsExisted = File.Exists(portableSettingsLocation.Path);

        try
        {
            var target = await ValidatePackageAsync(
                packagePath,
                targetValidationRoot,
                incomingAssetsRoot,
                cancellationToken);

            safetyBackup = await backupService.CreateAsync(cancellationToken, BackupPurpose.PreRestore);
            var safetyPath = backupService.FindPackagePath(safetyBackup.Id)
                ?? throw new InvalidDataException("恢复前自动备份创建后无法定位。");
            var rollback = await ValidatePackageAsync(
                safetyPath,
                rollbackValidationRoot,
                incomingAssetsRoot: null,
                cancellationToken);

            if (previousSettingsExisted)
            {
                File.Copy(portableSettingsLocation.Path, previousSettingsPath, overwrite: false);
            }

            assetSwap = SwapAssetDirectories(incomingAssetsRoot, rollbackAssetsRoot);
            await RestoreDatabaseAsync(target.DatabasePath, cancellationToken);
            databaseApplied = true;
            testHook.ThrowOnceAfterDatabaseApplied();

            await ApplyPortableSettingsAsync(target.PortableSettingsPath, cancellationToken);
            portableSettingsApplied = true;
            var missingVideoCount = await NormalizeRestoredStateAsync(cancellationToken);
            var restoredDatabase = await ValidateRestoreDatabaseAsync(
                databaseLocation.Path,
                target.Manifest,
                cancellationToken,
                validateManifestCounts: true);

            CommitAssetSwap(assetSwap);
            logger.LogWarning(
                "Restored backup {BackupId}. Safety backup {SafetyBackupId} was retained.",
                backupId,
                safetyBackup.Id);
            return new RestoreResult(
                backupId,
                safetyBackup,
                restoredDatabase,
                target.AssetCount.CoverCount,
                target.AssetCount.SubtitleFileCount,
                missingVideoCount,
                target.Manifest.Warnings ?? [],
                ConfigurationRestartRequired: true);
        }
        catch (BackupValidationException) when (
            safetyBackup is null
            && assetSwap is null
            && !databaseApplied
            && !portableSettingsApplied)
        {
            throw;
        }
        catch (Exception restoreError)
        {
            var rollbackErrors = new List<string>();
            if (portableSettingsApplied)
            {
                try
                {
                    RestorePortableSettings(previousSettingsExisted, previousSettingsPath);
                }
                catch (Exception ex)
                {
                    rollbackErrors.Add($"便携配置回滚失败：{ex.Message}");
                }
            }

            if (databaseApplied && safetyBackup is not null)
            {
                try
                {
                    var rollbackDatabasePath = Path.Combine(rollbackValidationRoot, DatabaseEntryName);
                    await RestoreDatabaseAsync(rollbackDatabasePath, CancellationToken.None);
                    await NormalizeRestoredStateAsync(CancellationToken.None);
                    await ValidateRestoreDatabaseAsync(
                        databaseLocation.Path,
                        manifest: null,
                        CancellationToken.None,
                        validateManifestCounts: false);
                }
                catch (Exception ex)
                {
                    rollbackErrors.Add($"数据库回滚失败：{ex.Message}");
                }
            }

            if (assetSwap is not null)
            {
                try
                {
                    RollbackAssetSwap(assetSwap);
                }
                catch (Exception ex)
                {
                    rollbackErrors.Add($"封面/字幕回滚失败：{ex.Message}");
                }
            }

            if (rollbackErrors.Count == 0)
            {
                logger.LogError(
                    restoreError,
                    "Restore of backup {BackupId} failed and was rolled back to safety backup {SafetyBackupId}.",
                    backupId,
                    safetyBackup?.Id);
                throw new RestoreFailedException(
                    $"恢复失败，已自动回滚。{restoreError.Message}",
                    safetyBackup?.Id,
                    rolledBack: true,
                    restoreError);
            }

            logger.LogCritical(
                restoreError,
                "Restore of backup {BackupId} failed and rollback was incomplete: {RollbackErrors}",
                backupId,
                string.Join("；", rollbackErrors));
            throw new RestoreFailedException(
                $"恢复失败且自动回滚不完整：{string.Join("；", rollbackErrors)}",
                safetyBackup?.Id,
                rolledBack: false,
                restoreError);
        }
        finally
        {
            TryDeleteDirectory(incomingAssetsRoot);
            TryDeleteDirectory(operationRoot);
        }
    }

    private async Task<ValidatedRestorePackage> ValidatePackageAsync(
        string packagePath,
        string validationRoot,
        string? incomingAssetsRoot,
        CancellationToken cancellationToken)
    {
        try
        {
            return await ValidatePackageCoreAsync(
                packagePath,
                validationRoot,
                incomingAssetsRoot,
                cancellationToken);
        }
        catch (BackupValidationException)
        {
            throw;
        }
        catch (Exception ex) when (
            ex is InvalidDataException
                or JsonException
                or CryptographicException
                or SqliteException
                or InvalidOperationException
                or ArgumentException)
        {
            throw new BackupValidationException($"备份包校验失败：{ex.Message}");
        }
    }

    private async Task<ValidatedRestorePackage> ValidatePackageCoreAsync(
        string packagePath,
        string validationRoot,
        string? incomingAssetsRoot,
        CancellationToken cancellationToken)
    {
        var packageFile = new FileInfo(packagePath);
        if (!packageFile.Exists || packageFile.Length <= 0 || packageFile.Length > backupStorage.MaxUploadBytes)
        {
            throw new BackupValidationException("备份包不存在、为空或超过允许大小。");
        }

        Directory.CreateDirectory(validationRoot);
        using var archive = ZipFile.OpenRead(packagePath);
        if (archive.Entries.Count is 0 or > MaximumEntryCount)
        {
            throw new BackupValidationException($"备份包文件数量必须在 1 到 {MaximumEntryCount} 之间。");
        }

        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in archive.Entries)
        {
            if (!IsSafeArchivePath(entry.FullName) || !names.Add(entry.FullName))
            {
                throw new BackupValidationException($"备份包包含重复或不安全路径：{entry.FullName}");
            }
            if (entry.FullName.EndsWith("/", StringComparison.Ordinal))
            {
                throw new BackupValidationException($"备份包不允许目录占位条目：{entry.FullName}");
            }
        }

        var manifestEntry = archive.GetEntry(ManifestEntryName)
            ?? throw new BackupValidationException("备份包缺少 manifest.json。");
        if (manifestEntry.Length is <= 0 or > MaximumManifestBytes)
        {
            throw new BackupValidationException("备份清单为空或超过 16 MB 限制。");
        }
        BackupManifest manifest;
        await using (var stream = manifestEntry.Open())
        {
            manifest = await JsonSerializer.DeserializeAsync<BackupManifest>(stream, JsonOptions, cancellationToken)
                ?? throw new BackupValidationException("备份清单无法读取。");
        }

        ValidateManifest(manifest, names);
        long expandedBytes = 0;
        foreach (var file in manifest.Files)
        {
            if (file.Size > backupStorage.MaxExpandedBytes - expandedBytes)
            {
                throw new BackupValidationException(
                    $"备份解压后大小超过 {FormatBytes(backupStorage.MaxExpandedBytes)} 限制。");
            }
            expandedBytes += file.Size;
        }
        if (expandedBytes > backupStorage.MaxExpandedBytes)
        {
            throw new BackupValidationException(
                $"备份解压后大小超过 {FormatBytes(backupStorage.MaxExpandedBytes)} 限制。");
        }

        string? databasePath = null;
        string? portableSettingsPath = null;
        var copiedCoverCount = 0;
        var copiedSubtitleCount = 0;
        foreach (var expected in manifest.Files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var entry = archive.GetEntry(expected.Path)
                ?? throw new BackupValidationException($"备份包缺少文件：{expected.Path}");
            if (entry.Length != expected.Size)
            {
                throw new BackupValidationException($"备份文件大小不匹配：{expected.Path}");
            }

            string? outputPath = null;
            if (expected.Path == DatabaseEntryName)
            {
                outputPath = Path.Combine(validationRoot, DatabaseEntryName.Replace('/', Path.DirectorySeparatorChar));
                databasePath = outputPath;
            }
            else if (expected.Path == PortableSettingsEntryName)
            {
                outputPath = Path.Combine(
                    validationRoot,
                    PortableSettingsEntryName.Replace('/', Path.DirectorySeparatorChar));
                portableSettingsPath = outputPath;
            }
            else if (incomingAssetsRoot is not null && expected.Path.StartsWith("assets/", StringComparison.Ordinal))
            {
                var relativeAssetPath = expected.Path["assets/".Length..]
                    .Replace('/', Path.DirectorySeparatorChar);
                outputPath = GetContainedPath(incomingAssetsRoot, relativeAssetPath);
                if (expected.Path.StartsWith("assets/covers/", StringComparison.Ordinal))
                {
                    copiedCoverCount++;
                }
                else
                {
                    copiedSubtitleCount++;
                }
            }

            await VerifyAndOptionallyExtractAsync(entry, expected, outputPath, cancellationToken);
        }

        if (databasePath is null || portableSettingsPath is null)
        {
            throw new BackupValidationException("备份包缺少数据库或便携配置。");
        }

        await ValidatePortableSettingsAsync(portableSettingsPath, cancellationToken);
        var database = await ValidateRestoreDatabaseAsync(
            databasePath,
            manifest,
            cancellationToken,
            validateManifestCounts: true);
        var databaseAssets = await ReadExpectedAssetEntriesAsync(databasePath, cancellationToken);
        var manifestAssets = manifest.Files
            .Where(file => file.Path.StartsWith("assets/", StringComparison.Ordinal))
            .Select(file => file.Path)
            .ToHashSet(StringComparer.Ordinal);
        var extra = manifestAssets.Except(databaseAssets).Take(3).ToArray();
        if (extra.Length > 0)
        {
            throw new BackupValidationException(
                $"备份包含数据库未引用的资产：{string.Join(", ", extra)}");
        }

        return new ValidatedRestorePackage(
            manifest,
            databasePath,
            portableSettingsPath,
            database,
            new BackupAssetSummary(
                incomingAssetsRoot is null
                    ? manifestAssets.Count(path => path.StartsWith("assets/covers/", StringComparison.Ordinal))
                    : copiedCoverCount,
                incomingAssetsRoot is null
                    ? manifestAssets.Count(path => path.StartsWith("assets/subtitles/", StringComparison.Ordinal))
                    : copiedSubtitleCount));
    }

    private static void ValidateManifest(BackupManifest manifest, HashSet<string> archiveNames)
    {
        if (manifest.FormatVersion != BackupService.CurrentFormatVersion)
        {
            throw new BackupValidationException($"不支持的备份格式版本：{manifest.FormatVersion}。");
        }
        if (!BackupIdPattern.IsMatch(manifest.BackupId))
        {
            throw new BackupValidationException("备份清单中的 ID 格式无效。");
        }
        if (manifest.IncludesOriginalVideos)
        {
            throw new BackupValidationException("恢复不接受包含原视频的备份包。");
        }
        if (manifest.Database is null
            || manifest.Assets is null
            || manifest.Warnings is null
            || manifest.DatabasePath != DatabaseEntryName
            || manifest.PortableSettingsPath != PortableSettingsEntryName)
        {
            throw new BackupValidationException("备份清单使用了不支持的数据库或配置路径。");
        }
        if (manifest.Files is null || manifest.Files.Count is 0 or > MaximumEntryCount)
        {
            throw new BackupValidationException("备份清单文件列表为空或过大。");
        }

        var manifestNames = new HashSet<string>(StringComparer.Ordinal) { ManifestEntryName };
        foreach (var file in manifest.Files)
        {
            if (file.Size < 0
                || (file.Path == PortableSettingsEntryName && file.Size > MaximumPortableSettingsBytes)
                || !Regex.IsMatch(file.Sha256 ?? "", "^[0-9a-f]{64}$", RegexOptions.CultureInvariant)
                || !IsAllowedPayloadPath(file.Path)
                || !manifestNames.Add(file.Path))
            {
                throw new BackupValidationException($"备份清单包含无效文件项：{file.Path}");
            }
        }

        if (!manifestNames.SetEquals(archiveNames))
        {
            throw new BackupValidationException("ZIP 实际条目与备份清单不一致。");
        }
    }

    private static bool IsAllowedPayloadPath(string path) =>
        IsSafeArchivePath(path)
        && (path == DatabaseEntryName
            || path == PortableSettingsEntryName
            || path.StartsWith("assets/covers/", StringComparison.Ordinal)
            || path.StartsWith("assets/subtitles/", StringComparison.Ordinal));

    private static bool IsSafeArchivePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path)
            || path.Contains('\\')
            || path.StartsWith("/", StringComparison.Ordinal)
            || path.Contains(':'))
        {
            return false;
        }

        var segments = path.Split('/');
        return segments.All(segment => segment.Length > 0 && segment is not "." and not "..");
    }

    private static async Task VerifyAndOptionallyExtractAsync(
        ZipArchiveEntry entry,
        BackupFileEntry expected,
        string? outputPath,
        CancellationToken cancellationToken)
    {
        if (outputPath is not null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        }

        await using var input = entry.Open();
        await using var output = outputPath is null
            ? Stream.Null
            : new FileStream(
                outputPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 1024 * 128,
                useAsync: true);
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = new byte[1024 * 128];
        long total = 0;
        while (true)
        {
            var read = await input.ReadAsync(buffer, cancellationToken);
            if (read == 0)
            {
                break;
            }
            total += read;
            if (total > expected.Size)
            {
                throw new BackupValidationException($"备份文件读取大小超过清单：{expected.Path}");
            }
            hash.AppendData(buffer, 0, read);
            if (outputPath is not null)
            {
                await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            }
        }

        var actualHash = Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant();
        if (total != expected.Size || !actualHash.Equals(expected.Sha256, StringComparison.OrdinalIgnoreCase))
        {
            throw new BackupValidationException($"备份文件校验失败：{expected.Path}");
        }
    }

    private static async Task ValidatePortableSettingsAsync(
        string path,
        CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        if (document.RootElement.ValueKind != JsonValueKind.Object
            || !document.RootElement.TryGetProperty("formatVersion", out var formatVersion)
            || !formatVersion.TryGetInt32(out var version)
            || version != BackupService.CurrentFormatVersion
            || !document.RootElement.TryGetProperty("aiSubtitles", out var aiSubtitles)
            || aiSubtitles.ValueKind != JsonValueKind.Object)
        {
            throw new BackupValidationException("便携配置结构或版本无效。");
        }

        var forbiddenNames = new HashSet<string>(
            ["apiKey", "baseUrl", "credential", "password", "cookie", "provider", "model", "audioWorkingPath"],
            StringComparer.OrdinalIgnoreCase);
        foreach (var property in aiSubtitles.EnumerateObject())
        {
            if (forbiddenNames.Contains(property.Name))
            {
                throw new BackupValidationException($"便携配置包含禁止字段：{property.Name}");
            }
        }
    }

    private static async Task<BackupDatabaseSummary> ValidateRestoreDatabaseAsync(
        string databasePath,
        BackupManifest? manifest,
        CancellationToken cancellationToken,
        bool validateManifestCounts)
    {
        await using var connection = await OpenConnectionAsync(databasePath, SqliteOpenMode.ReadOnly, cancellationToken);
        await using (var integrity = connection.CreateCommand())
        {
            integrity.CommandText = "PRAGMA integrity_check;";
            var result = Convert.ToString(await integrity.ExecuteScalarAsync(cancellationToken));
            if (!string.Equals(result, "ok", StringComparison.OrdinalIgnoreCase))
            {
                throw new BackupValidationException($"SQLite 完整性检查失败：{result}");
            }
        }

        foreach (var required in RequiredSchema)
        {
            var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            await using var command = connection.CreateCommand();
            command.CommandText = $"PRAGMA table_info(\"{required.Key}\");";
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                columns.Add(reader.GetString(1));
            }
            var missingColumns = required.Value.Where(column => !columns.Contains(column)).ToArray();
            if (missingColumns.Length > 0)
            {
                throw new BackupValidationException(
                    $"数据库缺少表或字段 {required.Key}.{string.Join(",", missingColumns)}。");
            }
        }

        await using (var foreignKeys = connection.CreateCommand())
        {
            foreignKeys.CommandText = "PRAGMA foreign_key_check;";
            await using var reader = await foreignKeys.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                throw new BackupValidationException("SQLite 外键检查失败。");
            }
        }

        var summary = new BackupDatabaseSummary(
            await CountRowsAsync(connection, "Videos", cancellationToken),
            await CountRowsAsync(connection, "TagCategories", cancellationToken),
            await CountRowsAsync(connection, "Tags", cancellationToken),
            await CountRowsAsync(connection, "SubtitleTracks", cancellationToken),
            await CountRowsAsync(connection, "SubtitleCues", cancellationToken));
        if (validateManifestCounts && manifest is not null && summary != manifest.Database)
        {
            throw new BackupValidationException("数据库记录数量与备份清单不一致。");
        }

        await ValidateDatabasePathsAsync(connection, cancellationToken);
        return summary;
    }

    private static async Task ValidateDatabasePathsAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        await using (var videos = connection.CreateCommand())
        {
            videos.CommandText = """SELECT "RelativePath", "CoverRelativePath" FROM "Videos";""";
            await using var reader = await videos.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                if (!IsSafeManagedPath(reader.GetString(0), "originals"))
                {
                    throw new BackupValidationException("数据库包含不安全的原视频路径。");
                }
                if (!reader.IsDBNull(1) && !IsSafeManagedPath(reader.GetString(1), "covers"))
                {
                    throw new BackupValidationException("数据库包含不安全的封面路径。");
                }
            }
        }

        await using var subtitles = connection.CreateCommand();
        subtitles.CommandText = """
            SELECT "OriginalRelativePath"
            FROM "SubtitleTracks"
            WHERE "OriginalRelativePath" IS NOT NULL AND trim("OriginalRelativePath") <> '';
            """;
        await using var subtitleReader = await subtitles.ExecuteReaderAsync(cancellationToken);
        while (await subtitleReader.ReadAsync(cancellationToken))
        {
            if (!IsSafeManagedPath(subtitleReader.GetString(0), "subtitles"))
            {
                throw new BackupValidationException("数据库包含不安全的字幕路径。");
            }
        }
    }

    private static async Task<HashSet<string>> ReadExpectedAssetEntriesAsync(
        string databasePath,
        CancellationToken cancellationToken)
    {
        var entries = new HashSet<string>(StringComparer.Ordinal);
        await using var connection = await OpenConnectionAsync(databasePath, SqliteOpenMode.ReadOnly, cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT 'cover', "CoverRelativePath"
            FROM "Videos"
            WHERE "CoverRelativePath" IS NOT NULL AND trim("CoverRelativePath") <> ''
            UNION
            SELECT 'subtitle', "OriginalRelativePath"
            FROM "SubtitleTracks"
            WHERE "OriginalRelativePath" IS NOT NULL AND trim("OriginalRelativePath") <> '';
            """;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var expectedRoot = reader.GetString(0) == "cover" ? "covers" : "subtitles";
            var relativePath = reader.GetString(1);
            if (!IsSafeManagedPath(relativePath, expectedRoot))
            {
                throw new BackupValidationException("数据库资产路径不安全。");
            }
            entries.Add($"assets/{NormalizeRelativePath(relativePath)}");
        }
        return entries;
    }

    private async Task RestoreDatabaseAsync(string snapshotPath, CancellationToken cancellationToken)
    {
        SqliteConnection.ClearAllPools();
        await using var source = await OpenConnectionAsync(snapshotPath, SqliteOpenMode.ReadOnly, cancellationToken);
        await using var destination = await OpenConnectionAsync(
            databaseLocation.Path,
            SqliteOpenMode.ReadWriteCreate,
            cancellationToken);
        source.BackupDatabase(destination);
        await using var journalMode = destination.CreateCommand();
        journalMode.CommandText = "PRAGMA journal_mode = WAL;";
        var mode = Convert.ToString(await journalMode.ExecuteScalarAsync(cancellationToken));
        if (!string.Equals(mode, "wal", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException($"恢复数据库无法启用 WAL：{mode}");
        }
        SqliteConnection.ClearAllPools();
    }

    private async Task<int> NormalizeRestoredStateAsync(CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(
            databaseLocation.Path,
            SqliteOpenMode.ReadWrite,
            cancellationToken);
        var videos = new List<(string Id, string RelativePath, string Status, string? HardSubtitleRelativePath)>();
        await using (var select = connection.CreateCommand())
        {
            select.CommandText = """SELECT "Id", "RelativePath", "Status", "HardSubtitleRelativePath" FROM "Videos";""";
            await using var reader = await select.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                videos.Add((
                    reader.GetString(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.IsDBNull(3) ? null : reader.GetString(3)));
            }
        }

        var missingCount = 0;
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        foreach (var video in videos)
        {
            if (video.Status == VideoStatus.Trashed)
            {
                continue;
            }
            var originalExists = File.Exists(videoStorage.GetAbsolutePath(video.RelativePath));
            var hardSubtitleExists = !string.IsNullOrWhiteSpace(video.HardSubtitleRelativePath)
                && File.Exists(videoStorage.GetAbsolutePath(video.HardSubtitleRelativePath));
            var status = originalExists || hardSubtitleExists
                ? VideoStatus.Available
                : VideoStatus.Missing;
            if (!originalExists && !hardSubtitleExists)
            {
                missingCount++;
            }
            await using var update = connection.CreateCommand();
            update.Transaction = (SqliteTransaction)transaction;
            update.CommandText = """
                UPDATE "Videos"
                SET "Status" = $status,
                    "OriginalAvailable" = $originalAvailable,
                    "OriginalDeletedAt" = CASE
                        WHEN $originalAvailable = 1 THEN NULL
                        ELSE "OriginalDeletedAt"
                    END,
                    "HardSubtitleRelativePath" = CASE
                        WHEN $hardSubtitleAvailable = 1 THEN "HardSubtitleRelativePath"
                        ELSE NULL
                    END,
                    "HardSubtitleTrackId" = CASE
                        WHEN $hardSubtitleAvailable = 1 THEN "HardSubtitleTrackId"
                        ELSE NULL
                    END,
                    "HardSubtitleTrackUpdatedAt" = CASE
                        WHEN $hardSubtitleAvailable = 1 THEN "HardSubtitleTrackUpdatedAt"
                        ELSE NULL
                    END,
                    "HardSubtitleSize" = CASE
                        WHEN $hardSubtitleAvailable = 1 THEN "HardSubtitleSize"
                        ELSE NULL
                    END,
                    "HardSubtitleCreatedAt" = CASE
                        WHEN $hardSubtitleAvailable = 1 THEN "HardSubtitleCreatedAt"
                        ELSE NULL
                    END,
                    "HardSubtitleStyleJson" = CASE
                        WHEN $hardSubtitleAvailable = 1 THEN "HardSubtitleStyleJson"
                        ELSE NULL
                    END,
                    "UpdatedAt" = $updatedAt
                WHERE "Id" = $id;
                """;
            update.Parameters.AddWithValue("$status", status);
            update.Parameters.AddWithValue("$originalAvailable", originalExists ? 1 : 0);
            update.Parameters.AddWithValue("$hardSubtitleAvailable", hardSubtitleExists ? 1 : 0);
            update.Parameters.AddWithValue("$updatedAt", DateTime.Now);
            update.Parameters.AddWithValue("$id", video.Id);
            await update.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var cancelJobs = connection.CreateCommand())
        {
            cancelJobs.Transaction = (SqliteTransaction)transaction;
            cancelJobs.CommandText = """
                UPDATE "MediaJobs"
                SET "Status" = 'Cancelled',
                    "Stage" = '备份恢复后已取消',
                    "CancellationRequested" = 1,
                    "UpdatedAt" = $now,
                    "CompletedAt" = $now
                WHERE "Status" IN ('Queued', 'Running');
                """;
            cancelJobs.Parameters.AddWithValue("$now", DateTime.Now);
            await cancelJobs.ExecuteNonQueryAsync(cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
        return missingCount;
    }

    private AssetSwapState SwapAssetDirectories(string incomingRoot, string rollbackRoot)
    {
        Directory.CreateDirectory(rollbackRoot);
        var state = new AssetSwapState(
            new AssetDirectorySwap(videoStorage.CoversPath, Path.Combine(incomingRoot, "covers"), Path.Combine(rollbackRoot, "covers")),
            new AssetDirectorySwap(videoStorage.SubtitlesPath, Path.Combine(incomingRoot, "subtitles"), Path.Combine(rollbackRoot, "subtitles")),
            rollbackRoot);
        try
        {
            SwapDirectory(state.Covers);
            SwapDirectory(state.Subtitles);
            return state;
        }
        catch
        {
            RollbackAssetSwap(state);
            throw;
        }
    }

    private static void SwapDirectory(AssetDirectorySwap swap)
    {
        if (!Directory.Exists(swap.IncomingPath))
        {
            Directory.CreateDirectory(swap.IncomingPath);
        }
        if (Directory.Exists(swap.RollbackPath))
        {
            throw new IOException($"恢复回滚目录已存在：{swap.RollbackPath}");
        }
        if (Directory.Exists(swap.LivePath))
        {
            Directory.Move(swap.LivePath, swap.RollbackPath);
            swap.LiveMoved = true;
        }
        Directory.Move(swap.IncomingPath, swap.LivePath);
        swap.IncomingMoved = true;
    }

    private static void RollbackAssetSwap(AssetSwapState state)
    {
        RollbackDirectory(state.Subtitles);
        RollbackDirectory(state.Covers);
        TryDeleteDirectory(state.RollbackRoot);
    }

    private static void RollbackDirectory(AssetDirectorySwap swap)
    {
        if (swap.IncomingMoved && Directory.Exists(swap.LivePath))
        {
            Directory.Delete(swap.LivePath, recursive: true);
        }
        if (swap.LiveMoved && Directory.Exists(swap.RollbackPath))
        {
            Directory.Move(swap.RollbackPath, swap.LivePath);
        }
    }

    private static void CommitAssetSwap(AssetSwapState state)
    {
        TryDeleteDirectory(state.RollbackRoot);
    }

    private async Task ApplyPortableSettingsAsync(string restoredPath, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(portableSettingsLocation.Path)!);
        var temporaryPath = portableSettingsLocation.Path + $".{Guid.NewGuid():N}.tmp";
        try
        {
            await using (var source = File.OpenRead(restoredPath))
            await using (var destination = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 64 * 1024,
                useAsync: true))
            {
                await source.CopyToAsync(destination, cancellationToken);
            }
            File.Move(temporaryPath, portableSettingsLocation.Path, overwrite: true);
        }
        finally
        {
            TryDeleteFile(temporaryPath);
        }
    }

    private void RestorePortableSettings(bool previousExisted, string previousPath)
    {
        if (previousExisted)
        {
            File.Copy(previousPath, portableSettingsLocation.Path, overwrite: true);
        }
        else
        {
            TryDeleteFile(portableSettingsLocation.Path);
        }
    }

    private static async Task CopyUploadAsync(
        Stream source,
        string destinationPath,
        long maximumBytes,
        CancellationToken cancellationToken)
    {
        await using var destination = new FileStream(
            destinationPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 1024 * 128,
            useAsync: true);
        var buffer = new byte[1024 * 128];
        long total = 0;
        while (true)
        {
            var read = await source.ReadAsync(buffer, cancellationToken);
            if (read == 0)
            {
                break;
            }
            total += read;
            if (total > maximumBytes)
            {
                throw new BackupValidationException(
                    $"上传备份超过 {FormatBytes(maximumBytes)} 限制。");
            }
            await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }
    }

    private static async Task<SqliteConnection> OpenConnectionAsync(
        string path,
        SqliteOpenMode mode,
        CancellationToken cancellationToken)
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = mode,
            Pooling = false,
            DefaultTimeout = 30
        }.ToString());
        await connection.OpenAsync(cancellationToken);
        return connection;
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

    private static bool IsSafeManagedPath(string value, string expectedRoot)
    {
        if (string.IsNullOrWhiteSpace(value) || Path.IsPathRooted(value))
        {
            return false;
        }
        var normalized = NormalizeRelativePath(value);
        var segments = normalized.Split('/');
        return segments.Length >= 2
            && segments[0].Equals(expectedRoot, StringComparison.OrdinalIgnoreCase)
            && segments.All(segment =>
                segment.Length > 0
                && segment is not "." and not ".."
                && !segment.Contains(':')
                && !segment.Any(char.IsControl));
    }

    private static string NormalizeRelativePath(string value) =>
        value.Replace('\\', '/').TrimStart('/');

    private static string GetContainedPath(string root, string relativePath)
    {
        var rootFullPath = Path.GetFullPath(root);
        var targetPath = Path.GetFullPath(Path.Combine(rootFullPath, relativePath));
        var relative = Path.GetRelativePath(rootFullPath, targetPath);
        if (relative == ".."
            || relative.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
            || Path.IsPathRooted(relative))
        {
            throw new BackupValidationException("备份文件路径越过恢复目录。");
        }
        return targetPath;
    }

    private static async Task<string> ComputeFileHashAsync(
        string path,
        CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string FormatBytes(long bytes) =>
        bytes >= 1024L * 1024 * 1024
            ? $"{bytes / (1024d * 1024 * 1024):0.##} GB"
            : $"{bytes / (1024d * 1024):0.##} MB";

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
            // Best-effort cleanup only.
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
            // Best-effort cleanup only.
        }
    }
}

public sealed class RestoreTestHook(IHostEnvironment environment, IConfiguration configuration)
{
    private int failAfterDatabaseApplied =
        environment.IsEnvironment("Testing")
        && configuration.GetValue("Testing:RestoreFailOnceAfterDatabaseApplied", false)
            ? 1
            : 0;

    public void ThrowOnceAfterDatabaseApplied()
    {
        if (Interlocked.Exchange(ref failAfterDatabaseApplied, 0) == 1)
        {
            throw new InvalidOperationException("测试注入：数据库应用后模拟恢复失败。");
        }
    }
}

public sealed record RestoreResult(
    string BackupId,
    BackupPackageItem SafetyBackup,
    BackupDatabaseSummary Database,
    int RestoredCoverCount,
    int RestoredSubtitleFileCount,
    int MissingVideoCount,
    IReadOnlyList<string> Warnings,
    bool ConfigurationRestartRequired);
public sealed record ValidatedRestorePackage(
    BackupManifest Manifest,
    string DatabasePath,
    string PortableSettingsPath,
    BackupDatabaseSummary Database,
    BackupAssetSummary AssetCount);
public sealed record AssetSwapState(
    AssetDirectorySwap Covers,
    AssetDirectorySwap Subtitles,
    string RollbackRoot);
public sealed class AssetDirectorySwap(string livePath, string incomingPath, string rollbackPath)
{
    public string LivePath { get; } = livePath;
    public string IncomingPath { get; } = incomingPath;
    public string RollbackPath { get; } = rollbackPath;
    public bool LiveMoved { get; set; }
    public bool IncomingMoved { get; set; }
}
public sealed class BackupValidationException(string message) : IOException(message);
public sealed class BackupConflictException(string message) : InvalidOperationException(message);
public sealed class RestoreBusyException(string message) : InvalidOperationException(message);
public sealed class RestoreFailedException(
    string message,
    string? safetyBackupId,
    bool rolledBack,
    Exception innerException) : InvalidOperationException(message, innerException)
{
    public string? SafetyBackupId { get; } = safetyBackupId;
    public bool RolledBack { get; } = rolledBack;
}
