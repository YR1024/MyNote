using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SubtitlesParserV2;
using SubtitlesParserV2.Models;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

var builder = WebApplication.CreateBuilder(args);
builder.Configuration
    .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false)
    .AddEnvironmentVariables()
    .AddCommandLine(args);

var videoStorage = VideoStorage.FromConfiguration(builder.Configuration, builder.Environment.ContentRootPath);
Directory.CreateDirectory(videoStorage.RootPath);
Directory.CreateDirectory(videoStorage.OriginalsPath);
Directory.CreateDirectory(videoStorage.CoversPath);
Directory.CreateDirectory(videoStorage.SubtitlesPath);
Directory.CreateDirectory(videoStorage.TrashPath);

var databasePath = ResolveDatabasePath(builder.Configuration, builder.Environment.ContentRootPath);
Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);
var connectionString = new SqliteConnectionStringBuilder { DataSource = databasePath }.ToString();
var credentialPath = ResolveCredentialPath(builder.Configuration, builder.Environment.ContentRootPath);
var credentialStore = new AdminCredentialStore(credentialPath);
var mediaTools = MediaTools.FromConfiguration(builder.Configuration, builder.Environment.ContentRootPath);
var aiSubtitleOptions = AiSubtitleOptions.FromConfiguration(builder.Configuration, builder.Environment.ContentRootPath);
await credentialStore.InitializeAsync();

builder.Services.AddSingleton(videoStorage);
builder.Services.AddSingleton(credentialStore);
builder.Services.AddSingleton(mediaTools);
builder.Services.AddSingleton(aiSubtitleOptions);
builder.Services.AddSingleton(AiSubtitleProviderFactory.CreateSpeech(aiSubtitleOptions));
builder.Services.AddSingleton(AiSubtitleProviderFactory.CreateTranslation(aiSubtitleOptions));
builder.Services.AddSingleton<AudioChunkExtractor>();
builder.Services.AddSingleton<MediaAnalyzer>();
builder.Services.AddSingleton<MediaJobQueueGate>();
builder.Services.AddDbContext<CinemaDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddScoped<AiSubtitlePipeline>();
builder.Services.AddSingleton(new MediaJobHandlers(
    async (services, _, cancellationToken, reportProgress) =>
    {
        var db = services.GetRequiredService<CinemaDbContext>();
        var storage = services.GetRequiredService<VideoStorage>();
        var analyzer = services.GetRequiredService<MediaAnalyzer>();
        var result = await SyncVideosAsync(db, storage, analyzer, cancellationToken, reportProgress);
        return JsonSerializer.Serialize(result, JsonSerializerOptions.Web);
    },
    (services, jobId, cancellationToken, reportProgress) =>
        services.GetRequiredService<AiSubtitlePipeline>()
            .RunSpeechRecognitionAsync(jobId, cancellationToken, reportProgress),
    (services, jobId, cancellationToken, reportProgress) =>
        services.GetRequiredService<AiSubtitlePipeline>()
            .RunSubtitleTranslationAsync(jobId, cancellationToken, reportProgress)));
builder.Services.AddHostedService<MediaJobWorker>();
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "DreamyCinema.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromHours(12);
        options.SlidingExpiration = false;
        options.LoginPath = "/login";
        options.Events.OnRedirectToLogin = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            }
            else
            {
                context.Response.Redirect(context.RedirectUri);
            }
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
            }
            else
            {
                context.Response.Redirect(context.RedirectUri);
            }
            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "DreamyCinema.Csrf";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CinemaDbContext>();
    await EnsureDatabaseAsync(db);
}

app.UseDefaultFiles();
app.UseAuthentication();
app.UseStaticFiles();
app.UseAuthorization();
app.UseRateLimiter();
app.UseAntiforgery();
app.Use(async (context, next) =>
{
    var method = context.Request.Method;
    var requiresValidation = context.Request.Path.StartsWithSegments("/api")
        && method is "POST" or "PUT" or "PATCH" or "DELETE";
    if (requiresValidation)
    {
        try
        {
            await context.RequestServices.GetRequiredService<IAntiforgery>().ValidateRequestAsync(context);
        }
        catch (AntiforgeryValidationException)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { message = "请求验证失败，请刷新页面后重试。" });
            return;
        }
    }

    await next();
});

app.MapGet("/api/auth/session", (HttpContext context, IAntiforgery antiforgery, AdminCredentialStore credentials) =>
{
    var tokens = antiforgery.GetAndStoreTokens(context);
    return Results.Ok(new
    {
        authenticated = context.User.Identity?.IsAuthenticated == true,
        setupRequired = !credentials.IsConfigured,
        canSetup = !credentials.IsConfigured && IsLoopbackRequest(context),
        requestToken = tokens.RequestToken
    });
}).AllowAnonymous();

app.MapPost("/api/auth/setup", async Task<IResult> (
    PasswordRequest request,
    HttpContext context,
    AdminCredentialStore credentials) =>
{
    if (credentials.IsConfigured)
    {
        return Results.Conflict(new { message = "管理员密码已经设置。" });
    }

    if (!IsLoopbackRequest(context))
    {
        return Results.StatusCode(StatusCodes.Status403Forbidden);
    }

    var validation = ValidateAdminPassword(request.Password);
    if (validation is not null)
    {
        return validation;
    }

    if (!await credentials.TryInitializeAsync(request.Password!))
    {
        return Results.Conflict(new { message = "管理员密码已经设置。" });
    }

    await SignInAdminAsync(context);
    return Results.Ok(new { authenticated = true });
}).AllowAnonymous().RequireRateLimiting("login");

app.MapPost("/api/auth/login", async Task<IResult> (
    PasswordRequest request,
    HttpContext context,
    AdminCredentialStore credentials) =>
{
    if (!credentials.IsConfigured || string.IsNullOrEmpty(request.Password)
        || !credentials.Verify(request.Password))
    {
        return Results.Unauthorized();
    }

    await SignInAdminAsync(context);
    return Results.Ok(new { authenticated = true });
}).AllowAnonymous().RequireRateLimiting("login");

app.MapPost("/api/auth/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.NoContent();
});

app.MapGet("/api/tag-categories", async (CinemaDbContext db) =>
{
    var categories = await db.TagCategories
        .AsNoTracking()
        .OrderBy(category => category.SortOrder)
        .ThenBy(category => category.Name)
        .ToListAsync();

    var tags = await db.Tags
        .AsNoTracking()
        .OrderBy(tag => tag.SortOrder)
        .ThenBy(tag => tag.Name)
        .ToListAsync();

    var counts = await db.VideoTags
        .AsNoTracking()
        .GroupBy(videoTag => videoTag.TagId)
        .Select(group => new { TagId = group.Key, Count = group.Count() })
        .ToDictionaryAsync(item => item.TagId, item => item.Count);

    return Results.Ok(categories.Select(category => new TagCategoryItem(
        category.Id,
        category.Name,
        tags
            .Where(tag => tag.CategoryId == category.Id)
            .Select(tag => new TagItem(
                tag.Id,
                tag.CategoryId,
                category.Name,
                tag.Name,
                counts.GetValueOrDefault(tag.Id)))
            .ToList())));
});

app.MapPost("/api/tag-categories", async Task<IResult> (TagNameRequest request, CinemaDbContext db) =>
{
    var validation = ValidateTagName(request.Name, out var name);
    if (validation is not null)
    {
        return validation;
    }

    var exists = await db.TagCategories.AnyAsync(category =>
        EF.Functions.Collate(category.Name, "NOCASE") == name);
    if (exists)
    {
        return Results.Conflict(new { message = "已存在同名分类。" });
    }

    var sortOrder = (await db.TagCategories.Select(category => (int?)category.SortOrder).MaxAsync() ?? 0) + 10;
    var category = new TagCategory { Name = name, SortOrder = sortOrder };
    db.TagCategories.Add(category);
    await db.SaveChangesAsync();
    return Results.Created($"/api/tag-categories/{category.Id}", new { category.Id, category.Name });
});

app.MapPut("/api/tag-categories/{id:int}", async Task<IResult> (
    int id,
    TagNameRequest request,
    CinemaDbContext db) =>
{
    var validation = ValidateTagName(request.Name, out var name);
    if (validation is not null)
    {
        return validation;
    }

    var category = await db.TagCategories.FirstOrDefaultAsync(item => item.Id == id);
    if (category is null)
    {
        return Results.NotFound();
    }

    var exists = await db.TagCategories.AnyAsync(item => item.Id != id
        && EF.Functions.Collate(item.Name, "NOCASE") == name);
    if (exists)
    {
        return Results.Conflict(new { message = "已存在同名分类。" });
    }

    category.Name = name;
    await db.SaveChangesAsync();
    return Results.Ok(new { category.Id, category.Name });
});

app.MapDelete("/api/tag-categories/{id:int}", async Task<IResult> (int id, CinemaDbContext db) =>
{
    var category = await db.TagCategories.FirstOrDefaultAsync(item => item.Id == id);
    if (category is null)
    {
        return Results.NotFound();
    }

    db.TagCategories.Remove(category);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.MapPost("/api/tag-categories/{categoryId:int}/tags", async Task<IResult> (
    int categoryId,
    TagNameRequest request,
    CinemaDbContext db) =>
{
    var validation = ValidateTagName(request.Name, out var name);
    if (validation is not null)
    {
        return validation;
    }

    var categoryExists = await db.TagCategories.AnyAsync(category => category.Id == categoryId);
    if (!categoryExists)
    {
        return Results.NotFound();
    }

    var exists = await db.Tags.AnyAsync(tag => tag.CategoryId == categoryId
        && EF.Functions.Collate(tag.Name, "NOCASE") == name);
    if (exists)
    {
        return Results.Conflict(new { message = "该分类中已存在同名标签。" });
    }

    var sortOrder = (await db.Tags
        .Where(tag => tag.CategoryId == categoryId)
        .Select(tag => (int?)tag.SortOrder)
        .MaxAsync() ?? 0) + 10;
    var tag = new Tag { CategoryId = categoryId, Name = name, SortOrder = sortOrder };
    db.Tags.Add(tag);
    await db.SaveChangesAsync();
    return Results.Created($"/api/tags/{tag.Id}", new { tag.Id, tag.CategoryId, tag.Name });
});

app.MapPut("/api/tags/{id:int}", async Task<IResult> (int id, TagNameRequest request, CinemaDbContext db) =>
{
    var validation = ValidateTagName(request.Name, out var name);
    if (validation is not null)
    {
        return validation;
    }

    var tag = await db.Tags.FirstOrDefaultAsync(item => item.Id == id);
    if (tag is null)
    {
        return Results.NotFound();
    }

    var exists = await db.Tags.AnyAsync(item => item.Id != id
        && item.CategoryId == tag.CategoryId
        && EF.Functions.Collate(item.Name, "NOCASE") == name);
    if (exists)
    {
        return Results.Conflict(new { message = "该分类中已存在同名标签。" });
    }

    tag.Name = name;
    await db.SaveChangesAsync();
    return Results.Ok(new { tag.Id, tag.CategoryId, tag.Name });
});

app.MapDelete("/api/tags/{id:int}", async Task<IResult> (int id, CinemaDbContext db) =>
{
    var tag = await db.Tags.FirstOrDefaultAsync(item => item.Id == id);
    if (tag is null)
    {
        return Results.NotFound();
    }

    db.Tags.Remove(tag);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.MapGet("/api/videos", async Task<IResult> (HttpRequest request, CinemaDbContext db) =>
{
    var selectedTagIds = ParseTagIds(request);
    var searchText = request.Query["q"].ToString().Trim();
    var sort = request.Query["sort"].ToString().Trim().ToLowerInvariant();
    var page = Math.Min(ParsePositiveQueryValue(request, "page", 1), 1_000_000);
    var pageSize = Math.Min(ParsePositiveQueryValue(request, "pageSize", 20), 50);
    if (searchText.Length > 120)
    {
        return Results.BadRequest(new { message = "搜索内容不能超过 120 个字符。" });
    }

    IQueryable<Video> query = db.Videos
        .AsNoTracking()
        .AsSplitQuery()
        .Include(video => video.VideoTags)
            .ThenInclude(videoTag => videoTag.Tag)
                .ThenInclude(tag => tag.Category)
        .Include(video => video.SubtitleTracks)
        .Where(video => video.Status == VideoStatus.Available);

    if (searchText.Length > 0)
    {
        var pattern = $"%{EscapeLikePattern(searchText)}%";
        query = query.Where(video =>
            EF.Functions.Like(video.Number, pattern, "\\")
            || EF.Functions.Like(video.Title, pattern, "\\")
            || EF.Functions.Like(video.Description, pattern, "\\")
            || EF.Functions.Like(video.OriginalFileName, pattern, "\\"));
    }

    foreach (var selectedTagId in selectedTagIds)
    {
        var tagId = selectedTagId;
        query = query.Where(video => video.VideoTags.Any(videoTag => videoTag.TagId == tagId));
    }

    var total = await query.CountAsync();

    query = sort switch
    {
        "created-desc" => query.OrderByDescending(video => video.CreatedAt).ThenBy(video => video.Number),
        "created-asc" => query.OrderBy(video => video.CreatedAt).ThenBy(video => video.Number),
        "number-asc" => query.OrderBy(video => EF.Functions.Collate(video.Number, "NOCASE")),
        "size-desc" => query.OrderByDescending(video => video.Size).ThenBy(video => video.Number),
        _ => query.OrderByDescending(video => video.ImportedAt).ThenBy(video => video.Number)
    };

    var videos = await query
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    return Results.Ok(new VideoPage(
        videos.Select(ToVideoItem).ToList(),
        total,
        page,
        pageSize,
        (long)page * pageSize < total));
});

app.MapGet("/api/videos/{id}", async (string id, CinemaDbContext db) =>
{
    var video = await LoadVideoAsync(db, id, tracking: false);
    return video is null
        ? Results.NotFound()
        : Results.Ok(ToVideoItem(video));
});

app.MapGet("/api/videos/{id}/subtitles", async (string id, CinemaDbContext db) =>
{
    var exists = await db.Videos.AsNoTracking().AnyAsync(video => video.Id == id);
    if (!exists)
    {
        return Results.NotFound();
    }

    var tracks = await db.SubtitleTracks
        .AsNoTracking()
        .Where(track => track.VideoId == id)
        .OrderByDescending(track => track.IsDefault)
        .ThenBy(track => track.CreatedAt)
        .ToListAsync();
    return Results.Ok(tracks.Select(ToSubtitleTrackItem));
});

app.MapGet("/api/subtitles/{id}/vtt", async Task<IResult> (string id, HttpContext context, CinemaDbContext db) =>
{
    var track = await db.SubtitleTracks
        .AsNoTracking()
        .Include(item => item.Cues)
        .FirstOrDefaultAsync(item => item.Id == id);
    if (track is null)
    {
        return Results.NotFound();
    }

    context.Response.Headers.CacheControl = "no-store";
    return Results.Text(BuildWebVtt(track.Cues), "text/vtt; charset=utf-8", Encoding.UTF8);
});

app.MapGet("/api/ai-subtitles/status", (
    AiSubtitleOptions options,
    ISpeechRecognitionProvider speechProvider,
    ISubtitleTranslationProvider translationProvider) =>
{
    return Results.Ok(new AiSubtitleStatus(
        options.Enabled,
        options.TargetLanguage,
        new AiProviderStatus(speechProvider.Name, speechProvider.Model, speechProvider.IsAvailable, speechProvider.IsMock),
        new AiProviderStatus(translationProvider.Name, translationProvider.Model, translationProvider.IsAvailable, translationProvider.IsMock),
        options.PreserveExplicitLanguage,
        options.TranslationStyle));
});

app.MapPost("/api/videos/{id}/subtitles/transcribe", async Task<IResult> (
    string id,
    SpeechJobInput? request,
    CinemaDbContext db,
    MediaJobQueueGate queueGate,
    AiSubtitleOptions options,
    ISpeechRecognitionProvider provider) =>
{
    if (!options.Enabled || !provider.IsAvailable)
    {
        return Results.Conflict(new { message = "语音识别 Provider 尚未启用或不可用。" });
    }
    var videoExists = await db.Videos.AsNoTracking().AnyAsync(video => video.Id == id && video.Status == VideoStatus.Available);
    if (!videoExists)
    {
        return Results.NotFound();
    }
    return await EnqueueAiJobAsync(
        db,
        queueGate,
        MediaJobType.SpeechRecognition,
        id,
        request ?? new SpeechJobInput(null));
});

app.MapPost("/api/videos/{id}/subtitles/{trackId}/translate", async Task<IResult> (
    string id,
    string trackId,
    CinemaDbContext db,
    MediaJobQueueGate queueGate,
    AiSubtitleOptions options,
    ISubtitleTranslationProvider provider,
    bool force = false) =>
{
    if (!options.Enabled || !provider.IsAvailable)
    {
        return Results.Conflict(new { message = "字幕翻译 Provider 尚未启用或不可用。" });
    }
    var trackExists = await db.SubtitleTracks.AsNoTracking().AnyAsync(
        track => track.Id == trackId && track.VideoId == id && track.CueCount > 0);
    if (!trackExists)
    {
        return Results.NotFound();
    }
    return await EnqueueAiJobAsync(
        db,
        queueGate,
        MediaJobType.SubtitleTranslation,
        id,
        new TranslationJobInput(
            trackId,
            options.TargetLanguage,
            options.CreateTranslationProfile(),
            force ? Guid.NewGuid().ToString("N") : null));
});

app.MapPut("/api/videos/{id}", async (string id, UpdateVideoRequest request, CinemaDbContext db) =>
{
    var number = request.Number?.Trim() ?? "";
    var title = request.Title?.Trim() ?? "";
    var description = request.Description?.Trim() ?? "";
    var tagIds = request.TagIds?.Distinct().ToList() ?? [];

    if (string.IsNullOrWhiteSpace(number))
    {
        return Results.BadRequest(new { message = "番号不能为空。" });
    }

    if (number.Length > 120 || title.Length > 300 || description.Length > 1200)
    {
        return Results.BadRequest(new { message = "填写内容超过允许长度。" });
    }

    var video = await LoadVideoAsync(db, id, tracking: true);
    if (video is null)
    {
        return Results.NotFound();
    }

    var numberInUse = await db.Videos.AnyAsync(item => item.Id != id && item.Number == number);
    if (numberInUse)
    {
        return Results.Conflict(new { message = "这个番号已被其他视频使用。" });
    }

    var tags = await db.Tags
        .Where(tag => tagIds.Contains(tag.Id))
        .ToListAsync();

    if (tags.Count != tagIds.Count)
    {
        return Results.BadRequest(new { message = "包含不存在的标签。" });
    }

    await using var transaction = await db.Database.BeginTransactionAsync();
    video.Number = number;
    video.Title = string.IsNullOrWhiteSpace(title) ? number : title;
    video.Description = description;
    video.UpdatedAt = DateTime.Now;

    db.VideoTags.RemoveRange(video.VideoTags);
    foreach (var tag in tags)
    {
        db.VideoTags.Add(new VideoTag { VideoId = video.Id, TagId = tag.Id });
    }

    await db.SaveChangesAsync();
    await transaction.CommitAsync();

    var updatedVideo = await LoadVideoAsync(db, id, tracking: false);
    return Results.Ok(ToVideoItem(updatedVideo!));
});

app.MapPost("/api/videos/sync", async Task<IResult> (CinemaDbContext db, MediaJobQueueGate queueGate) =>
{
    await queueGate.Gate.WaitAsync();
    try
    {
        var existing = await db.MediaJobs
            .AsNoTracking()
            .Where(job => job.Type == MediaJobType.Sync
                && (job.Status == MediaJobStatus.Queued || job.Status == MediaJobStatus.Running))
            .OrderBy(job => job.CreatedAt)
            .FirstOrDefaultAsync();
        if (existing is not null)
        {
            return Results.Accepted($"/api/jobs/{existing.Id}", ToMediaJobItem(existing));
        }

        var now = DateTime.Now;
        var job = new MediaJob
        {
            Id = Guid.NewGuid().ToString("N"),
            Type = MediaJobType.Sync,
            Status = MediaJobStatus.Queued,
            Stage = "等待执行",
            Progress = 0,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.MediaJobs.Add(job);
        await db.SaveChangesAsync();
        return Results.Accepted($"/api/jobs/{job.Id}", ToMediaJobItem(job));
    }
    finally
    {
        queueGate.Gate.Release();
    }
});

app.MapGet("/api/jobs", async (CinemaDbContext db) =>
{
    var jobs = await db.MediaJobs
        .AsNoTracking()
        .OrderByDescending(job => job.CreatedAt)
        .Take(50)
        .ToListAsync();
    return Results.Ok(jobs.Select(ToMediaJobItem));
});

app.MapGet("/api/jobs/{id}", async Task<IResult> (string id, CinemaDbContext db) =>
{
    var job = await db.MediaJobs.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id);
    return job is null ? Results.NotFound() : Results.Ok(ToMediaJobItem(job));
});

app.MapPost("/api/jobs/{id}/cancel", async Task<IResult> (string id, CinemaDbContext db) =>
{
    var job = await db.MediaJobs.FirstOrDefaultAsync(item => item.Id == id);
    if (job is null)
    {
        return Results.NotFound();
    }

    if (job.Status == MediaJobStatus.Queued)
    {
        job.Status = MediaJobStatus.Cancelled;
        job.Stage = "已取消";
        job.CompletedAt = DateTime.Now;
    }
    else if (job.Status == MediaJobStatus.Running)
    {
        job.CancellationRequested = true;
        job.Stage = "正在取消";
    }
    else
    {
        return Results.Conflict(new { message = "只有等待中或运行中的任务可以取消。" });
    }

    job.UpdatedAt = DateTime.Now;
    await db.SaveChangesAsync();
    return Results.Ok(ToMediaJobItem(job));
});

app.MapPost("/api/jobs/{id}/retry", async Task<IResult> (string id, CinemaDbContext db, MediaJobQueueGate queueGate) =>
{
    await queueGate.Gate.WaitAsync();
    try
    {
        var job = await db.MediaJobs.FirstOrDefaultAsync(item => item.Id == id);
        if (job is null)
        {
            return Results.NotFound();
        }
        if (job.Status is not (MediaJobStatus.Failed or MediaJobStatus.Cancelled))
        {
            return Results.Conflict(new { message = "只有失败或已取消的任务可以重试。" });
        }

        var duplicate = await db.MediaJobs.AnyAsync(item => item.Id != id
            && item.Type == job.Type
            && item.VideoId == job.VideoId
            && (item.Status == MediaJobStatus.Queued || item.Status == MediaJobStatus.Running));
        if (duplicate)
        {
            return Results.Conflict(new { message = "已有同类任务正在等待或执行。" });
        }

        job.Status = MediaJobStatus.Queued;
        job.Stage = "等待重试";
        job.Progress = 0;
        job.CurrentItem = null;
        job.Error = null;
        job.ResultJson = null;
        job.CancellationRequested = false;
        job.StartedAt = null;
        job.CompletedAt = null;
        job.UpdatedAt = DateTime.Now;
        await db.SaveChangesAsync();
        return Results.Accepted($"/api/jobs/{job.Id}", ToMediaJobItem(job));
    }
    finally
    {
        queueGate.Gate.Release();
    }
});

app.MapGet("/api/videos/{id}/cover", async Task<IResult> (
    string id,
    HttpContext context,
    CinemaDbContext db,
    VideoStorage storage) =>
{
    var video = await db.Videos.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id);
    if (video is null || string.IsNullOrWhiteSpace(video.CoverRelativePath))
    {
        return Results.NotFound();
    }

    var filePath = storage.GetAbsolutePath(video.CoverRelativePath);
    if (!System.IO.File.Exists(filePath))
    {
        return Results.NotFound();
    }

    context.Response.Headers.CacheControl = "no-store";
    return Results.File(filePath, GetCoverContentType(filePath));
});

app.MapPost("/api/videos/{id}/cover", async Task<IResult> (
    string id,
    HttpRequest request,
    CinemaDbContext db,
    VideoStorage storage) =>
{
    const long maxCoverSize = 8 * 1024 * 1024;

    var video = await db.Videos.FirstOrDefaultAsync(item => item.Id == id);
    if (video is null)
    {
        return Results.NotFound();
    }

    if (!request.HasFormContentType)
    {
        return Results.BadRequest(new { message = "请选择封面图片。" });
    }

    var form = await request.ReadFormAsync();
    var cover = form.Files.GetFile("cover");
    if (cover is null || cover.Length == 0)
    {
        return Results.BadRequest(new { message = "请选择封面图片。" });
    }

    if (cover.Length > maxCoverSize)
    {
        return Results.BadRequest(new { message = "封面不能超过 8 MB。" });
    }

    if (!TryGetCoverFormat(cover, out var extension, out _))
    {
        return Results.BadRequest(new { message = "封面仅支持 JPEG、PNG 或 WebP。" });
    }

    var now = DateTime.Now;
    var relativePath = Path.Combine(
        "covers",
        now.Year.ToString("0000"),
        now.Month.ToString("00"),
        $"{video.Id}-{Guid.NewGuid():N}{extension}");
    var destinationPath = storage.GetAbsolutePath(relativePath);
    Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);

    await using var input = cover.OpenReadStream();
    var signature = new byte[12];
    var signatureLength = await input.ReadAsync(signature);
    if (!HasValidCoverSignature(signature.AsSpan(0, signatureLength), extension))
    {
        return Results.BadRequest(new { message = "图片文件内容与格式不符。" });
    }

    input.Position = 0;
    try
    {
        await using (var output = System.IO.File.Create(destinationPath))
        {
            await input.CopyToAsync(output);
        }

        var oldCoverRelativePath = video.CoverRelativePath;
        video.CoverRelativePath = relativePath;
        video.UpdatedAt = now;
        await db.SaveChangesAsync();

        DeleteCoverIfPresent(storage, oldCoverRelativePath);
        return Results.Ok(new { coverUrl = $"/api/videos/{Uri.EscapeDataString(video.Id)}/cover" });
    }
    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or DbUpdateException)
    {
        DeleteCoverIfPresent(storage, relativePath);
        return Results.Problem("保存封面失败，请稍后重试。");
    }
});

app.MapDelete("/api/videos/{id}/cover", async Task<IResult> (
    string id,
    CinemaDbContext db,
    VideoStorage storage) =>
{
    var video = await db.Videos.FirstOrDefaultAsync(item => item.Id == id);
    if (video is null)
    {
        return Results.NotFound();
    }

    var oldCoverRelativePath = video.CoverRelativePath;
    video.CoverRelativePath = null;
    video.UpdatedAt = DateTime.Now;
    await db.SaveChangesAsync();
    DeleteCoverIfPresent(storage, oldCoverRelativePath);
    return Results.NoContent();
});

app.MapGet("/api/videos/maintenance", async (CinemaDbContext db, VideoStorage storage) =>
{
    await RefreshMissingStatusAsync(db, storage, DateTime.Now);
    var videos = await db.Videos
        .AsNoTracking()
        .Where(video => video.Status == VideoStatus.Trashed || video.Status == VideoStatus.Missing)
        .OrderBy(video => video.Status)
        .ThenByDescending(video => video.UpdatedAt)
        .ToListAsync();

    return Results.Ok(videos.Select(ToMaintenanceVideoItem));
});

app.MapPost("/api/videos/{id}/trash", async Task<IResult> (
    string id,
    CinemaDbContext db,
    VideoStorage storage) =>
{
    var video = await db.Videos.FirstOrDefaultAsync(item => item.Id == id);
    if (video is null)
    {
        return Results.NotFound();
    }

    if (video.Status == VideoStatus.Trashed)
    {
        return Results.Conflict(new { message = "视频已经在回收站中。" });
    }

    var sourcePath = storage.GetAbsolutePath(video.RelativePath);
    var trashRelativePath = GetTrashRelativePath(video);
    var trashPath = storage.GetAbsolutePath(trashRelativePath);
    var moved = false;

    if (System.IO.File.Exists(sourcePath))
    {
        if (System.IO.File.Exists(trashPath))
        {
            return Results.Conflict(new { message = "回收站中已存在同名文件。" });
        }

        if (!CanMoveFile(sourcePath, out var reason))
        {
            return Results.Conflict(new { message = reason });
        }

        Directory.CreateDirectory(Path.GetDirectoryName(trashPath)!);
        System.IO.File.Move(sourcePath, trashPath);
        moved = true;
    }

    try
    {
        video.Status = VideoStatus.Trashed;
        video.UpdatedAt = DateTime.Now;
        await db.SaveChangesAsync();
        return Results.Ok(ToMaintenanceVideoItem(video));
    }
    catch (DbUpdateException)
    {
        if (moved && System.IO.File.Exists(trashPath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(sourcePath)!);
            System.IO.File.Move(trashPath, sourcePath);
        }
        return Results.Problem("移动到回收站失败，请稍后重试。");
    }
});

app.MapPost("/api/videos/{id}/restore", async Task<IResult> (
    string id,
    CinemaDbContext db,
    VideoStorage storage) =>
{
    var video = await db.Videos.FirstOrDefaultAsync(item => item.Id == id);
    if (video is null)
    {
        return Results.NotFound();
    }

    if (video.Status != VideoStatus.Trashed)
    {
        return Results.Conflict(new { message = "只有回收站中的视频可以恢复。" });
    }

    var originalPath = storage.GetAbsolutePath(video.RelativePath);
    var trashPath = storage.GetAbsolutePath(GetTrashRelativePath(video));
    var moved = false;

    if (System.IO.File.Exists(trashPath))
    {
        if (System.IO.File.Exists(originalPath))
        {
            return Results.Conflict(new { message = "原位置已有文件，无法覆盖恢复。" });
        }

        if (!CanMoveFile(trashPath, out var reason))
        {
            return Results.Conflict(new { message = reason });
        }

        Directory.CreateDirectory(Path.GetDirectoryName(originalPath)!);
        System.IO.File.Move(trashPath, originalPath);
        moved = true;
    }

    try
    {
        video.Status = System.IO.File.Exists(originalPath)
            ? VideoStatus.Available
            : VideoStatus.Missing;
        video.UpdatedAt = DateTime.Now;
        await db.SaveChangesAsync();
        DeleteDirectoryIfEmpty(Path.GetDirectoryName(trashPath));
        return Results.Ok(new { video.Id, video.Status });
    }
    catch (DbUpdateException)
    {
        if (moved && System.IO.File.Exists(originalPath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(trashPath)!);
            System.IO.File.Move(originalPath, trashPath);
        }
        return Results.Problem("恢复视频失败，请稍后重试。");
    }
});

app.MapDelete("/api/videos/{id}", async Task<IResult> (
    string id,
    CinemaDbContext db,
    VideoStorage storage) =>
{
    var video = await db.Videos.FirstOrDefaultAsync(item => item.Id == id);
    if (video is null)
    {
        return Results.NotFound();
    }

    if (video.Status == VideoStatus.Available)
    {
        return Results.Conflict(new { message = "请先将视频移入回收站。" });
    }

    var originalPath = storage.GetAbsolutePath(video.RelativePath);
    var trashPath = storage.GetAbsolutePath(GetTrashRelativePath(video));
    if (System.IO.File.Exists(originalPath))
    {
        video.Status = VideoStatus.Available;
        video.UpdatedAt = DateTime.Now;
        await db.SaveChangesAsync();
        return Results.Conflict(new { message = "原视频文件仍然存在，请先移入回收站。" });
    }

    if (!TryDeleteManagedFile(trashPath, out var deleteError))
    {
        return Results.Conflict(new { message = deleteError });
    }

    if (!string.IsNullOrWhiteSpace(video.CoverRelativePath))
    {
        var coverPath = storage.GetAbsolutePath(video.CoverRelativePath);
        if (!TryDeleteManagedFile(coverPath, out deleteError))
        {
            return Results.Conflict(new { message = deleteError });
        }
    }

    var subtitleDirectory = storage.GetAbsolutePath(Path.Combine("subtitles", video.Id));
    if (!TryDeleteManagedDirectory(subtitleDirectory, out deleteError))
    {
        return Results.Conflict(new { message = deleteError });
    }

    db.Videos.Remove(video);
    await db.SaveChangesAsync();
    DeleteDirectoryIfEmpty(Path.GetDirectoryName(trashPath));
    return Results.NoContent();
});

app.MapGet("/api/videos/{id}/stream", async Task<Results<PhysicalFileHttpResult, NotFound>> (
    string id,
    CinemaDbContext db,
    VideoStorage storage) =>
{
    var video = await db.Videos.FirstOrDefaultAsync(item => item.Id == id);
    if (video is null || video.Status == VideoStatus.Trashed)
    {
        return TypedResults.NotFound();
    }

    var filePath = storage.GetAbsolutePath(video.RelativePath);
    if (!System.IO.File.Exists(filePath))
    {
        video.Status = VideoStatus.Missing;
        video.UpdatedAt = DateTime.Now;
        await db.SaveChangesAsync();
        return TypedResults.NotFound();
    }

    return TypedResults.PhysicalFile(
        filePath,
        contentType: video.ContentType,
        fileDownloadName: null,
        enableRangeProcessing: true);
});

app.MapFallback("/api/{**path}", () => Results.NotFound());
app.MapFallbackToFile("index.html").AllowAnonymous();

app.Run();

static async Task<SyncResult> SyncVideosAsync(
    CinemaDbContext db,
    VideoStorage storage,
    MediaAnalyzer mediaAnalyzer,
    CancellationToken cancellationToken = default,
    Func<JobProgress, Task>? reportProgress = null)
{
    reportProgress ??= _ => Task.CompletedTask;
    await reportProgress(new JobProgress(2, "检查片库", null));
    cancellationToken.ThrowIfCancellationRequested();
    Directory.CreateDirectory(storage.RootPath);
    Directory.CreateDirectory(storage.OriginalsPath);

    var now = DateTime.Now;
    var imported = new List<VideoItem>();
    var failed = new List<SyncFailure>();
    var warnings = new List<SyncWarning>();
    var analyzedCount = 0;
    var generatedCoverCount = 0;
    var importedSubtitleCount = 0;
    var existingNumbers = await db.Videos.Select(video => video.Number).ToListAsync();
    var usedNumbers = existingNumbers
        .Where(number => !string.IsNullOrWhiteSpace(number))
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    await RefreshMissingStatusAsync(db, storage, now);

    await reportProgress(new JobProgress(8, "补齐媒体信息", null));
    var enrichment = await EnrichExistingVideosAsync(db, storage, mediaAnalyzer, now, warnings, cancellationToken);
    analyzedCount += enrichment.AnalyzedCount;
    generatedCoverCount += enrichment.GeneratedCoverCount;

    var sourcePaths = Directory.EnumerateFiles(storage.RootPath, "*.mp4", SearchOption.TopDirectoryOnly).ToList();
    for (var sourceIndex = 0; sourceIndex < sourcePaths.Count; sourceIndex++)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var sourcePath = sourcePaths[sourceIndex];
        var sourceFile = new FileInfo(sourcePath);
        var progress = 20 + (int)Math.Floor(50d * sourceIndex / Math.Max(1, sourcePaths.Count));
        await reportProgress(new JobProgress(progress, "导入视频", sourceFile.Name));
        if (!CanMoveFile(sourcePath, out var reason))
        {
            failed.Add(new SyncFailure(sourceFile.Name, reason));
            continue;
        }

        var id = Guid.NewGuid().ToString("N");
        var storedFileName = $"{id}{sourceFile.Extension.ToLowerInvariant()}";
        var relativePath = Path.Combine("originals", now.Year.ToString("0000"), now.Month.ToString("00"), storedFileName);
        var destinationPath = storage.GetAbsolutePath(relativePath);
        var number = GenerateUniqueNumber(Path.GetFileNameWithoutExtension(sourceFile.Name), usedNumbers);

        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);

        try
        {
            System.IO.File.Move(sourcePath, destinationPath, overwrite: false);

            var media = await mediaAnalyzer.AnalyzeAsync(destinationPath);
            if (media.Warning is not null)
            {
                warnings.Add(new SyncWarning(sourceFile.Name, media.Warning));
            }
            else
            {
                analyzedCount++;
            }

            string? coverRelativePath = null;
            var coverResult = await GenerateAutomaticCoverAsync(mediaAnalyzer, storage, id, destinationPath, media.DurationSeconds, now);
            if (coverResult.RelativePath is not null)
            {
                coverRelativePath = coverResult.RelativePath;
                generatedCoverCount++;
            }
            else if (coverResult.Warning is not null)
            {
                warnings.Add(new SyncWarning(sourceFile.Name, coverResult.Warning));
            }

            var video = new Video
            {
                Id = id,
                Number = number,
                Title = number,
                Description = BuildDescription(sourceFile.Name),
                OriginalFileName = sourceFile.Name,
                StoredFileName = storedFileName,
                RelativePath = relativePath,
                CoverRelativePath = coverRelativePath,
                Size = new FileInfo(destinationPath).Length,
                DurationSeconds = media.DurationSeconds,
                Width = media.Width,
                Height = media.Height,
                VideoCodec = media.VideoCodec,
                ContentType = "video/mp4",
                Status = VideoStatus.Available,
                ImportedAt = now,
                CreatedAt = now,
                UpdatedAt = now
            };

            db.Videos.Add(video);
            await db.SaveChangesAsync();
            await AssignDefaultTagsAsync(db, video, now);

            imported.Add(ToVideoItem(video));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or DbUpdateException)
        {
            failed.Add(new SyncFailure(sourceFile.Name, ex.Message));
        }
    }

    cancellationToken.ThrowIfCancellationRequested();
    await reportProgress(new JobProgress(72, "发现字幕", null));
    importedSubtitleCount = await SyncSubtitlesAsync(
        db,
        storage,
        mediaAnalyzer,
        now,
        warnings,
        cancellationToken,
        reportProgress);

    await reportProgress(new JobProgress(98, "整理同步结果", null));
    var availableCount = await db.Videos.CountAsync(video => video.Status == VideoStatus.Available);
    var missingCount = await db.Videos.CountAsync(video => video.Status == VideoStatus.Missing);

    return new SyncResult(imported.Count, availableCount, missingCount, analyzedCount, generatedCoverCount, importedSubtitleCount, failed, warnings);
}

static async Task<int> SyncSubtitlesAsync(
    CinemaDbContext db,
    VideoStorage storage,
    MediaAnalyzer mediaAnalyzer,
    DateTime now,
    List<SyncWarning> warnings,
    CancellationToken cancellationToken,
    Func<JobProgress, Task> reportProgress)
{
    var importedCount = 0;
    var videos = await db.Videos
        .Include(video => video.SubtitleTracks)
        .Where(video => video.Status == VideoStatus.Available)
        .ToListAsync();

    var externalFiles = Directory.EnumerateFiles(storage.RootPath, "*", SearchOption.TopDirectoryOnly)
        .Where(IsSupportedSubtitleFile)
        .ToList();
    for (var externalIndex = 0; externalIndex < externalFiles.Count; externalIndex++)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var subtitlePath = externalFiles[externalIndex];
        var file = new FileInfo(subtitlePath);
        await reportProgress(new JobProgress(
            74 + (int)Math.Floor(8d * externalIndex / Math.Max(1, externalFiles.Count)),
            "导入外挂字幕",
            file.Name));
        var match = MatchSubtitleToVideo(file, videos);
        if (match is null)
        {
            warnings.Add(new SyncWarning(file.Name, "未找到同番号或同原文件名的视频，字幕保留在 Videos 目录。"));
            continue;
        }

        var sourceKey = $"external:{file.Name.ToLowerInvariant()}";
        if (match.Video.SubtitleTracks.Any(track => track.SourceKey == sourceKey))
        {
            continue;
        }

        var result = await ImportSubtitleFileAsync(
            db,
            storage,
            match.Video,
            subtitlePath,
            sourceKey,
            SubtitleSource.External,
            match.Language,
            BuildSubtitleLabel(match.Language, "外挂字幕"),
            moveSource: true,
            now);
        if (result.Warning is not null)
        {
            warnings.Add(new SyncWarning(file.Name, result.Warning));
            continue;
        }

        importedCount++;
    }

    var videosToScan = videos.Where(item => !item.SubtitleScanCompleted).ToList();
    for (var videoIndex = 0; videoIndex < videosToScan.Count; videoIndex++)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var video = videosToScan[videoIndex];
        await reportProgress(new JobProgress(
            83 + (int)Math.Floor(14d * videoIndex / Math.Max(1, videosToScan.Count)),
            "检查内嵌字幕",
            video.OriginalFileName));
        var videoPath = storage.GetAbsolutePath(video.RelativePath);
        if (!System.IO.File.Exists(videoPath))
        {
            continue;
        }

        var probe = await mediaAnalyzer.GetSubtitleStreamsAsync(videoPath);
        if (probe.Warning is not null)
        {
            warnings.Add(new SyncWarning(video.OriginalFileName, probe.Warning));
            continue;
        }

        foreach (var stream in probe.Streams)
        {
            var sourceKey = $"embedded:{stream.Index}:{stream.Codec}";
            if (video.SubtitleTracks.Any(track => track.SourceKey == sourceKey))
            {
                continue;
            }

            var trackId = Guid.NewGuid().ToString("N");
            var relativePath = Path.Combine("subtitles", video.Id, $"{trackId}.vtt");
            var outputPath = storage.GetAbsolutePath(relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            var extractWarning = await mediaAnalyzer.ExtractSubtitleAsync(videoPath, stream.Index, outputPath);
            if (extractWarning is not null)
            {
                warnings.Add(new SyncWarning(video.OriginalFileName, $"内嵌字幕轨道 {stream.Index}：{extractWarning}"));
                continue;
            }

            var result = await ImportSubtitleFileAsync(
                db,
                storage,
                video,
                outputPath,
                sourceKey,
                SubtitleSource.Embedded,
                NormalizeLanguage(stream.Language),
                string.IsNullOrWhiteSpace(stream.Title) ? BuildSubtitleLabel(stream.Language, "内嵌字幕") : stream.Title!,
                moveSource: false,
                now,
                trackId,
                relativePath);
            if (result.Warning is not null)
            {
                DeleteManagedFileIfPresent(storage, relativePath);
                warnings.Add(new SyncWarning(video.OriginalFileName, $"内嵌字幕轨道 {stream.Index}：{result.Warning}"));
                continue;
            }

            importedCount++;
        }

        video.SubtitleScanCompleted = true;
        video.UpdatedAt = now;
        await db.SaveChangesAsync();
    }

    return importedCount;
}

static async Task<SubtitleImportResult> ImportSubtitleFileAsync(
    CinemaDbContext db,
    VideoStorage storage,
    Video video,
    string sourcePath,
    string sourceKey,
    string source,
    string language,
    string label,
    bool moveSource,
    DateTime now,
    string? requestedTrackId = null,
    string? existingRelativePath = null)
{
    ParsedSubtitle parsed;
    try
    {
        parsed = ParseSubtitleFile(sourcePath);
    }
    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or DecoderFallbackException)
    {
        return new SubtitleImportResult(null, $"字幕解析失败：{ex.Message}");
    }

    var trackId = requestedTrackId ?? Guid.NewGuid().ToString("N");
    var extension = Path.GetExtension(sourcePath).ToLowerInvariant();
    var relativePath = existingRelativePath ?? Path.Combine("subtitles", video.Id, $"{trackId}{extension}");
    var destinationPath = storage.GetAbsolutePath(relativePath);
    var moved = false;

    try
    {
        if (moveSource)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
            System.IO.File.Move(sourcePath, destinationPath, overwrite: false);
            moved = true;
        }

        var track = new SubtitleTrack
        {
            Id = trackId,
            VideoId = video.Id,
            Label = label,
            Language = NormalizeLanguage(language),
            Kind = SubtitleKind.Original,
            Source = source,
            RevisionStage = SubtitleRevisionStage.SourceOriginal,
            SourceKey = sourceKey,
            Format = parsed.Format,
            OriginalRelativePath = relativePath,
            IsDefault = video.SubtitleTracks.Count == 0,
            CueCount = parsed.Cues.Count,
            CreatedAt = now,
            UpdatedAt = now,
            Cues = parsed.Cues.Select((cue, index) => new SubtitleCue
            {
                TrackId = trackId,
                Index = index,
                StartMilliseconds = cue.StartMilliseconds,
                EndMilliseconds = cue.EndMilliseconds,
                Text = cue.Text
            }).ToList()
        };

        video.SubtitleTracks.Add(track);
        await db.SaveChangesAsync();
        return new SubtitleImportResult(track, null);
    }
    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or DbUpdateException)
    {
        var attached = video.SubtitleTracks.FirstOrDefault(track => track.Id == trackId);
        if (attached is not null)
        {
            video.SubtitleTracks.Remove(attached);
            foreach (var cue in attached.Cues)
            {
                db.Entry(cue).State = EntityState.Detached;
            }
            db.Entry(attached).State = EntityState.Detached;
        }
        if (moved && System.IO.File.Exists(destinationPath) && !System.IO.File.Exists(sourcePath))
        {
            System.IO.File.Move(destinationPath, sourcePath);
        }
        return new SubtitleImportResult(null, $"字幕入库失败：{ex.Message}");
    }
}

static ParsedSubtitle ParseSubtitleFile(string path)
{
    var encodings = new Encoding[]
    {
        new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true),
        Encoding.GetEncoding(54936, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback)
    };

    foreach (var encoding in encodings)
    {
        try
        {
            using var stream = System.IO.File.OpenRead(path);
            var result = SubtitleParser.ParseStream(stream, encoding);
            if (result is null)
            {
                continue;
            }

            var cues = result.Subtitles
                .Where(item => item.StartTime >= 0 && item.EndTime > item.StartTime)
                .Select(item => new ParsedSubtitleCue(
                    item.StartTime,
                    item.EndTime,
                    string.Join('\n', item.Lines).Replace("\\N", "\n", StringComparison.Ordinal).Trim()))
                .Where(item => !string.IsNullOrWhiteSpace(item.Text))
                .ToList();
            if (cues.Count == 0)
            {
                throw new InvalidDataException("没有找到带有效时间轴的字幕条目。");
            }

            return new ParsedSubtitle(result.FormatType.ToString(), cues);
        }
        catch (DecoderFallbackException)
        {
            // Try the next supported text encoding.
        }
    }

    throw new InvalidDataException("不支持该字幕格式或文本编码（支持 UTF-8 和 GB18030）。");
}

static SubtitleMatch? MatchSubtitleToVideo(FileInfo subtitleFile, IReadOnlyList<Video> videos)
{
    var stem = Path.GetFileNameWithoutExtension(subtitleFile.Name);
    var candidates = videos
        .SelectMany(video => new[]
        {
            new { Video = video, Key = video.Number },
            new { Video = video, Key = Path.GetFileNameWithoutExtension(video.OriginalFileName) }
        })
        .Where(candidate => !string.IsNullOrWhiteSpace(candidate.Key))
        .OrderByDescending(candidate => candidate.Key.Length);

    foreach (var candidate in candidates)
    {
        if (stem.Equals(candidate.Key, StringComparison.OrdinalIgnoreCase))
        {
            return new SubtitleMatch(candidate.Video, "und");
        }

        var prefix = candidate.Key + ".";
        if (stem.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return new SubtitleMatch(candidate.Video, stem[prefix.Length..]);
        }
    }

    return null;
}

static bool IsSupportedSubtitleFile(string path)
{
    return Path.GetExtension(path).ToLowerInvariant() is ".srt" or ".vtt" or ".ass" or ".ssa";
}

static string NormalizeLanguage(string? language)
{
    var value = language?.Trim().Replace('_', '-').ToLowerInvariant() ?? "";
    return value switch
    {
        "zh" or "chi" or "zho" or "chs" or "zh-cn" or "zh-hans" => "zh-CN",
        "cht" or "zh-tw" or "zh-hant" => "zh-TW",
        "en" or "eng" => "en",
        "ja" or "jp" or "jpn" => "ja",
        "ko" or "kor" => "ko",
        "fr" or "fre" or "fra" => "fr",
        "de" or "ger" or "deu" => "de",
        "es" or "spa" => "es",
        "und" or "" => "und",
        _ when value.Length <= 20 => value,
        _ => "und"
    };
}

static string BuildSubtitleLabel(string? language, string suffix)
{
    var normalized = NormalizeLanguage(language);
    return normalized == "und" ? suffix : $"{normalized} {suffix}";
}

static string BuildWebVtt(IEnumerable<SubtitleCue> cues)
{
    var builder = new StringBuilder("WEBVTT\n\n");
    foreach (var cue in cues.OrderBy(item => item.Index))
    {
        builder.Append(cue.Index + 1).Append('\n');
        builder.Append(FormatVttTime(cue.StartMilliseconds))
            .Append(" --> ")
            .Append(FormatVttTime(cue.EndMilliseconds))
            .Append('\n');
        builder.Append(cue.Text.Replace("\r", "", StringComparison.Ordinal).Trim()).Append("\n\n");
    }
    return builder.ToString();
}

static string FormatVttTime(long milliseconds)
{
    var time = TimeSpan.FromMilliseconds(Math.Max(0, milliseconds));
    return $"{(int)time.TotalHours:00}:{time.Minutes:00}:{time.Seconds:00}.{time.Milliseconds:000}";
}

static async Task<EnrichmentResult> EnrichExistingVideosAsync(
    CinemaDbContext db,
    VideoStorage storage,
    MediaAnalyzer mediaAnalyzer,
    DateTime now,
    List<SyncWarning> warnings,
    CancellationToken cancellationToken)
{
    var videos = await db.Videos
        .Where(video => video.Status == VideoStatus.Available
            && (video.DurationSeconds == null
                || video.Width == null
                || video.Height == null
                || video.VideoCodec == null
                || video.CoverRelativePath == null))
        .ToListAsync();
    var analyzedCount = 0;
    var generatedCoverCount = 0;

    foreach (var video in videos)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var filePath = storage.GetAbsolutePath(video.RelativePath);
        if (!System.IO.File.Exists(filePath))
        {
            continue;
        }

        var needsMetadata = video.DurationSeconds == null || video.Width == null || video.Height == null || video.VideoCodec == null;
        var media = needsMetadata
            ? await mediaAnalyzer.AnalyzeAsync(filePath)
            : new MediaAnalysis(video.DurationSeconds, video.Width, video.Height, video.VideoCodec, null);

        if (needsMetadata)
        {
            if (media.Warning is not null)
            {
                warnings.Add(new SyncWarning(video.OriginalFileName, media.Warning));
            }
            else
            {
                video.DurationSeconds = media.DurationSeconds;
                video.Width = media.Width;
                video.Height = media.Height;
                video.VideoCodec = media.VideoCodec;
                analyzedCount++;
            }
        }

        if (video.CoverRelativePath == null)
        {
            var coverResult = await GenerateAutomaticCoverAsync(mediaAnalyzer, storage, video.Id, filePath, media.DurationSeconds, now);
            if (coverResult.RelativePath is not null)
            {
                video.CoverRelativePath = coverResult.RelativePath;
                generatedCoverCount++;
            }
            else if (coverResult.Warning is not null)
            {
                warnings.Add(new SyncWarning(video.OriginalFileName, coverResult.Warning));
            }
        }

        video.UpdatedAt = now;
    }

    if (videos.Count > 0)
    {
        await db.SaveChangesAsync();
    }

    return new EnrichmentResult(analyzedCount, generatedCoverCount);
}

static async Task<CoverGenerationResult> GenerateAutomaticCoverAsync(
    MediaAnalyzer mediaAnalyzer,
    VideoStorage storage,
    string videoId,
    string videoPath,
    double? durationSeconds,
    DateTime now)
{
    var relativePath = Path.Combine("covers", now.Year.ToString("0000"), now.Month.ToString("00"), $"{videoId}-auto.jpg");
    var outputPath = storage.GetAbsolutePath(relativePath);
    Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
    var result = await mediaAnalyzer.GenerateCoverAsync(videoPath, outputPath, durationSeconds);
    if (result is null)
    {
        return new CoverGenerationResult(relativePath, null);
    }

    DeleteCoverIfPresent(storage, relativePath);
    return new CoverGenerationResult(null, result);
}

static async Task EnsureDatabaseAsync(CinemaDbContext db)
{
    var databaseCreated = await db.Database.EnsureCreatedAsync();
    await EnsureVideoColumnAsync(db, "Number", """ALTER TABLE "Videos" ADD COLUMN "Number" TEXT NOT NULL DEFAULT '';""");
    await EnsureVideoColumnAsync(db, "Description", """ALTER TABLE "Videos" ADD COLUMN "Description" TEXT NOT NULL DEFAULT '';""");
    await EnsureVideoColumnAsync(db, "CoverRelativePath", """ALTER TABLE "Videos" ADD COLUMN "CoverRelativePath" TEXT NULL;""");
    await EnsureVideoColumnAsync(db, "DurationSeconds", """ALTER TABLE "Videos" ADD COLUMN "DurationSeconds" REAL NULL;""");
    await EnsureVideoColumnAsync(db, "Width", """ALTER TABLE "Videos" ADD COLUMN "Width" INTEGER NULL;""");
    await EnsureVideoColumnAsync(db, "Height", """ALTER TABLE "Videos" ADD COLUMN "Height" INTEGER NULL;""");
    await EnsureVideoColumnAsync(db, "VideoCodec", """ALTER TABLE "Videos" ADD COLUMN "VideoCodec" TEXT NULL;""");
    await EnsureVideoColumnAsync(db, "SubtitleScanCompleted", """ALTER TABLE "Videos" ADD COLUMN "SubtitleScanCompleted" INTEGER NOT NULL DEFAULT 0;""");

    await db.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS "TagCategories" (
            "Id" INTEGER NOT NULL CONSTRAINT "PK_TagCategories" PRIMARY KEY AUTOINCREMENT,
            "Name" TEXT NOT NULL,
            "SortOrder" INTEGER NOT NULL
        );
        """);

    await db.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS "Tags" (
            "Id" INTEGER NOT NULL CONSTRAINT "PK_Tags" PRIMARY KEY AUTOINCREMENT,
            "CategoryId" INTEGER NOT NULL,
            "Name" TEXT NOT NULL,
            "SortOrder" INTEGER NOT NULL,
            CONSTRAINT "FK_Tags_TagCategories_CategoryId" FOREIGN KEY ("CategoryId") REFERENCES "TagCategories" ("Id") ON DELETE CASCADE
        );
        """);

    await db.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS "VideoTags" (
            "VideoId" TEXT NOT NULL,
            "TagId" INTEGER NOT NULL,
            CONSTRAINT "PK_VideoTags" PRIMARY KEY ("VideoId", "TagId"),
            CONSTRAINT "FK_VideoTags_Tags_TagId" FOREIGN KEY ("TagId") REFERENCES "Tags" ("Id") ON DELETE CASCADE,
            CONSTRAINT "FK_VideoTags_Videos_VideoId" FOREIGN KEY ("VideoId") REFERENCES "Videos" ("Id") ON DELETE CASCADE
        );
        """);

    await db.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS "SubtitleTracks" (
            "Id" TEXT NOT NULL CONSTRAINT "PK_SubtitleTracks" PRIMARY KEY,
            "VideoId" TEXT NOT NULL,
            "Label" TEXT NOT NULL,
            "Language" TEXT NOT NULL,
            "Kind" TEXT NOT NULL,
            "Source" TEXT NOT NULL,
            "RevisionStage" TEXT NOT NULL DEFAULT 'SourceOriginal',
            "SourceKey" TEXT NOT NULL,
            "Format" TEXT NOT NULL,
            "OriginalRelativePath" TEXT NOT NULL,
            "IsDefault" INTEGER NOT NULL,
            "CueCount" INTEGER NOT NULL,
            "CreatedAt" TEXT NOT NULL,
            "UpdatedAt" TEXT NOT NULL,
            CONSTRAINT "FK_SubtitleTracks_Videos_VideoId" FOREIGN KEY ("VideoId") REFERENCES "Videos" ("Id") ON DELETE CASCADE
        );
        """);

    await db.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS "SubtitleCues" (
            "Id" INTEGER NOT NULL CONSTRAINT "PK_SubtitleCues" PRIMARY KEY AUTOINCREMENT,
            "TrackId" TEXT NOT NULL,
            "Index" INTEGER NOT NULL,
            "StartMilliseconds" INTEGER NOT NULL,
            "EndMilliseconds" INTEGER NOT NULL,
            "Text" TEXT NOT NULL,
            CONSTRAINT "FK_SubtitleCues_SubtitleTracks_TrackId" FOREIGN KEY ("TrackId") REFERENCES "SubtitleTracks" ("Id") ON DELETE CASCADE
        );
        """);

    await db.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS "MediaJobs" (
            "Id" TEXT NOT NULL CONSTRAINT "PK_MediaJobs" PRIMARY KEY,
            "Type" TEXT NOT NULL,
            "Status" TEXT NOT NULL,
            "VideoId" TEXT NULL,
            "Progress" INTEGER NOT NULL,
            "Stage" TEXT NOT NULL,
            "CurrentItem" TEXT NULL,
            "Error" TEXT NULL,
            "InputJson" TEXT NULL,
            "ResultJson" TEXT NULL,
            "CancellationRequested" INTEGER NOT NULL,
            "AttemptCount" INTEGER NOT NULL,
            "CreatedAt" TEXT NOT NULL,
            "UpdatedAt" TEXT NOT NULL,
            "StartedAt" TEXT NULL,
            "CompletedAt" TEXT NULL
        );
        """);

    await EnsureTableColumnAsync(db, "SubtitleTracks", "RevisionStage", """ALTER TABLE "SubtitleTracks" ADD COLUMN "RevisionStage" TEXT NOT NULL DEFAULT 'SourceOriginal';""");
    await EnsureTableColumnAsync(db, "MediaJobs", "InputJson", """ALTER TABLE "MediaJobs" ADD COLUMN "InputJson" TEXT NULL;""");

    await db.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS "AiJobChunks" (
            "Id" TEXT NOT NULL CONSTRAINT "PK_AiJobChunks" PRIMARY KEY,
            "JobId" TEXT NOT NULL,
            "Index" INTEGER NOT NULL,
            "Kind" TEXT NOT NULL,
            "Status" TEXT NOT NULL,
            "OutputJson" TEXT NULL,
            "InputUnits" INTEGER NOT NULL,
            "OutputUnits" INTEGER NOT NULL,
            "AttemptCount" INTEGER NOT NULL,
            "Error" TEXT NULL,
            "CreatedAt" TEXT NOT NULL,
            "UpdatedAt" TEXT NOT NULL,
            "CompletedAt" TEXT NULL,
            CONSTRAINT "FK_AiJobChunks_MediaJobs_JobId" FOREIGN KEY ("JobId") REFERENCES "MediaJobs" ("Id") ON DELETE CASCADE
        );
        """);

    await db.Database.ExecuteSqlRawAsync("""CREATE UNIQUE INDEX IF NOT EXISTS "IX_TagCategories_Name" ON "TagCategories" ("Name");""");
    await db.Database.ExecuteSqlRawAsync("""CREATE UNIQUE INDEX IF NOT EXISTS "IX_Tags_CategoryId_Name" ON "Tags" ("CategoryId", "Name");""");
    await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_VideoTags_TagId" ON "VideoTags" ("TagId");""");
    await db.Database.ExecuteSqlRawAsync("""CREATE UNIQUE INDEX IF NOT EXISTS "IX_SubtitleTracks_VideoId_SourceKey" ON "SubtitleTracks" ("VideoId", "SourceKey");""");
    await db.Database.ExecuteSqlRawAsync("""CREATE UNIQUE INDEX IF NOT EXISTS "IX_SubtitleCues_TrackId_Index" ON "SubtitleCues" ("TrackId", "Index");""");
    await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_MediaJobs_Status_CreatedAt" ON "MediaJobs" ("Status", "CreatedAt");""");
    await db.Database.ExecuteSqlRawAsync("""CREATE INDEX IF NOT EXISTS "IX_MediaJobs_Type_VideoId_Status" ON "MediaJobs" ("Type", "VideoId", "Status");""");
    await db.Database.ExecuteSqlRawAsync("""CREATE UNIQUE INDEX IF NOT EXISTS "IX_AiJobChunks_JobId_Index" ON "AiJobChunks" ("JobId", "Index");""");

    if (databaseCreated)
    {
        await SeedTagsAsync(db);
    }
    await EnsureVideoMetadataAsync(db);
    await db.Database.ExecuteSqlRawAsync("""CREATE UNIQUE INDEX IF NOT EXISTS "IX_Videos_Number" ON "Videos" ("Number");""");
}

static async Task EnsureTableColumnAsync(CinemaDbContext db, string tableName, string columnName, string alterSql)
{
    var columns = await GetTableColumnsAsync(db, tableName);
    if (!columns.Contains(columnName))
    {
        await db.Database.ExecuteSqlRawAsync(alterSql);
    }
}

static async Task EnsureVideoColumnAsync(CinemaDbContext db, string columnName, string alterSql)
{
    var columns = await GetTableColumnsAsync(db, "Videos");
    if (columns.Contains(columnName))
    {
        return;
    }

    await db.Database.ExecuteSqlRawAsync(alterSql);
}

static async Task<HashSet<string>> GetTableColumnsAsync(CinemaDbContext db, string tableName)
{
    var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    var connection = db.Database.GetDbConnection();

    await db.Database.OpenConnectionAsync();
    try
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"""PRAGMA table_info("{tableName}");""";
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            columns.Add(reader.GetString(1));
        }
    }
    finally
    {
        await db.Database.CloseConnectionAsync();
    }

    return columns;
}

static async Task SeedTagsAsync(CinemaDbContext db)
{
    var seeds = new[]
    {
        new TagCategorySeed("类型", 10, ["伦理", "动作", "爱情", "戏剧", "悬疑", "喜剧"]),
        new TagCategorySeed("地区", 20, ["日本", "欧美", "国产", "韩国"]),
        new TagCategorySeed("年份", 30, ["2026", "2025", "2024", "更早"]),
        new TagCategorySeed("清晰度", 40, ["4K", "1080P", "720P"]),
        new TagCategorySeed("状态", 50, ["未看", "已看", "收藏"])
    };

    foreach (var seed in seeds)
    {
        var category = await db.TagCategories.FirstOrDefaultAsync(item => item.Name == seed.Name);
        if (category is null)
        {
            category = new TagCategory { Name = seed.Name, SortOrder = seed.SortOrder };
            db.TagCategories.Add(category);
            await db.SaveChangesAsync();
        }
        else if (category.SortOrder != seed.SortOrder)
        {
            category.SortOrder = seed.SortOrder;
            await db.SaveChangesAsync();
        }

        for (var index = 0; index < seed.Tags.Length; index++)
        {
            var tagName = seed.Tags[index];
            var sortOrder = (index + 1) * 10;
            var tag = await db.Tags.FirstOrDefaultAsync(item => item.CategoryId == category.Id && item.Name == tagName);

            if (tag is null)
            {
                db.Tags.Add(new Tag { CategoryId = category.Id, Name = tagName, SortOrder = sortOrder });
            }
            else if (tag.SortOrder != sortOrder)
            {
                tag.SortOrder = sortOrder;
            }
        }

        await db.SaveChangesAsync();
    }
}

static async Task EnsureVideoMetadataAsync(CinemaDbContext db)
{
    var videos = await db.Videos.OrderBy(video => video.ImportedAt).ToListAsync();
    var usedNumbers = videos
        .Select(video => video.Number)
        .Where(number => !string.IsNullOrWhiteSpace(number))
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    foreach (var video in videos)
    {
        if (string.IsNullOrWhiteSpace(video.Number))
        {
            var preferredNumber = !string.IsNullOrWhiteSpace(video.Title)
                ? video.Title
                : Path.GetFileNameWithoutExtension(video.OriginalFileName);

            video.Number = GenerateUniqueNumber(preferredNumber, usedNumbers);
        }
        else
        {
            usedNumbers.Add(video.Number);
        }

        if (string.IsNullOrWhiteSpace(video.Title))
        {
            video.Title = video.Number;
        }

        if (string.IsNullOrWhiteSpace(video.Description))
        {
            video.Description = BuildDescription(video.OriginalFileName);
        }
    }

    await db.SaveChangesAsync();
}

static async Task AssignDefaultTagsAsync(CinemaDbContext db, Video video, DateTime date)
{
    var tagIds = new[]
    {
        await GetTagIdAsync(db, "状态", "未看"),
        await GetTagIdAsync(db, "年份", GetYearTagName(date))
    };

    foreach (var tagId in tagIds.Where(id => id is not null).Cast<int>())
    {
        var exists = await db.VideoTags.AnyAsync(item => item.VideoId == video.Id && item.TagId == tagId);
        if (!exists)
        {
            db.VideoTags.Add(new VideoTag { VideoId = video.Id, TagId = tagId });
        }
    }

    await db.SaveChangesAsync();
}

static async Task<int?> GetTagIdAsync(CinemaDbContext db, string categoryName, string tagName)
{
    return await db.Tags
        .Where(tag => tag.Name == tagName && tag.Category.Name == categoryName)
        .Select(tag => (int?)tag.Id)
        .FirstOrDefaultAsync();
}

static string GetYearTagName(DateTime date)
{
    return date.Year switch
    {
        2026 => "2026",
        2025 => "2025",
        2024 => "2024",
        _ => "更早"
    };
}

static async Task RefreshMissingStatusAsync(CinemaDbContext db, VideoStorage storage, DateTime now)
{
    var videos = await db.Videos.ToListAsync();
    foreach (var video in videos)
    {
        if (video.Status == VideoStatus.Trashed)
        {
            continue;
        }

        var exists = System.IO.File.Exists(storage.GetAbsolutePath(video.RelativePath));
        var expectedStatus = exists ? VideoStatus.Available : VideoStatus.Missing;

        if (video.Status != expectedStatus)
        {
            video.Status = expectedStatus;
            video.UpdatedAt = now;
        }
    }

    await db.SaveChangesAsync();
}

static string GetTrashRelativePath(Video video)
{
    return Path.Combine("trash", video.Id, video.StoredFileName);
}

static bool TryDeleteManagedFile(string path, out string reason)
{
    try
    {
        if (System.IO.File.Exists(path))
        {
            System.IO.File.Delete(path);
        }
        reason = "";
        return true;
    }
    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
    {
        reason = "文件正在使用中或没有删除权限。";
        return false;
    }
}

static void DeleteDirectoryIfEmpty(string? path)
{
    if (!string.IsNullOrWhiteSpace(path)
        && Directory.Exists(path)
        && !Directory.EnumerateFileSystemEntries(path).Any())
    {
        Directory.Delete(path);
    }
}

static bool CanMoveFile(string path, out string reason)
{
    try
    {
        using var stream = System.IO.File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        reason = "";
        return true;
    }
    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
    {
        reason = "文件正在使用中或没有权限移动，稍后再同步。";
        return false;
    }
}

static string ResolveDatabasePath(IConfiguration configuration, string contentRootPath)
{
    var configuredPath = configuration["Database:Path"];
    if (string.IsNullOrWhiteSpace(configuredPath))
    {
        configuredPath = Path.Combine("Data", "dreamy-cinema.db");
    }

    return Path.IsPathRooted(configuredPath)
        ? configuredPath
        : Path.GetFullPath(Path.Combine(contentRootPath, configuredPath));
}

static string ResolveCredentialPath(IConfiguration configuration, string contentRootPath)
{
    var configuredPath = configuration["Security:CredentialPath"];
    if (string.IsNullOrWhiteSpace(configuredPath))
    {
        configuredPath = Path.Combine("Data", "admin-credentials.json");
    }

    return Path.IsPathRooted(configuredPath)
        ? configuredPath
        : Path.GetFullPath(Path.Combine(contentRootPath, configuredPath));
}

static bool IsLoopbackRequest(HttpContext context)
{
    var address = context.Connection.RemoteIpAddress;
    return address is not null && IPAddress.IsLoopback(address);
}

static IResult? ValidateAdminPassword(string? password)
{
    if (string.IsNullOrEmpty(password) || password.Length < 10)
    {
        return Results.BadRequest(new { message = "密码至少需要 10 个字符。" });
    }

    if (password.Length > 128)
    {
        return Results.BadRequest(new { message = "密码不能超过 128 个字符。" });
    }

    return null;
}

static Task SignInAdminAsync(HttpContext context)
{
    var identity = new ClaimsIdentity(
        [new Claim(ClaimTypes.Name, "admin"), new Claim(ClaimTypes.Role, "Admin")],
        CookieAuthenticationDefaults.AuthenticationScheme);
    var principal = new ClaimsPrincipal(identity);
    return context.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        principal,
        new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(12),
            AllowRefresh = false
        });
}

static HashSet<int> ParseTagIds(HttpRequest request)
{
    var tagIds = new HashSet<int>();

    foreach (var value in request.Query["tagIds"])
    {
        foreach (var part in value?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [])
        {
            if (int.TryParse(part, out var tagId))
            {
                tagIds.Add(tagId);
            }
        }
    }

    return tagIds;
}

static int ParsePositiveQueryValue(HttpRequest request, string name, int fallback)
{
    return int.TryParse(request.Query[name], out var value) && value > 0 ? value : fallback;
}

static string EscapeLikePattern(string value)
{
    return value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("%", "\\%", StringComparison.Ordinal)
        .Replace("_", "\\_", StringComparison.Ordinal);
}

static string BuildDescription(string originalFileName)
{
    return $"本地导入视频，原文件名：{originalFileName}";
}

static IResult? ValidateTagName(string? value, out string name)
{
    name = value?.Trim() ?? "";
    if (name.Length == 0)
    {
        return Results.BadRequest(new { message = "名称不能为空。" });
    }

    if (name.Length > 80)
    {
        return Results.BadRequest(new { message = "名称不能超过 80 个字符。" });
    }

    return null;
}

static bool TryGetCoverFormat(IFormFile cover, out string extension, out string contentType)
{
    var sourceExtension = Path.GetExtension(cover.FileName).ToLowerInvariant();
    (extension, contentType) = (sourceExtension, cover.ContentType.ToLowerInvariant()) switch
    {
        (".jpg" or ".jpeg", "image/jpeg") => (".jpg", "image/jpeg"),
        (".png", "image/png") => (".png", "image/png"),
        (".webp", "image/webp") => (".webp", "image/webp"),
        _ => ("", "")
    };

    return extension.Length > 0;
}

static bool HasValidCoverSignature(ReadOnlySpan<byte> signature, string extension)
{
    return extension switch
    {
        ".jpg" => signature.Length >= 3
            && signature[0] == 0xFF && signature[1] == 0xD8 && signature[2] == 0xFF,
        ".png" => signature.Length >= 8
            && signature[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
        ".webp" => signature.Length >= 12
            && signature[..4].SequenceEqual("RIFF"u8)
            && signature[8..12].SequenceEqual("WEBP"u8),
        _ => false
    };
}

static string GetCoverContentType(string filePath)
{
    return Path.GetExtension(filePath).ToLowerInvariant() switch
    {
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".webp" => "image/webp",
        _ => "application/octet-stream"
    };
}

static void DeleteCoverIfPresent(VideoStorage storage, string? relativePath)
{
    DeleteManagedFileIfPresent(storage, relativePath);
}

static void DeleteManagedFileIfPresent(VideoStorage storage, string? relativePath)
{
    if (string.IsNullOrWhiteSpace(relativePath))
    {
        return;
    }

    try
    {
        var filePath = storage.GetAbsolutePath(relativePath);
        if (System.IO.File.Exists(filePath))
        {
            System.IO.File.Delete(filePath);
        }
    }
    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
    {
        // A stale cover can be cleaned up later without failing the metadata update.
    }
}

static bool TryDeleteManagedDirectory(string directoryPath, out string error)
{
    error = "";
    if (!Directory.Exists(directoryPath))
    {
        return true;
    }

    try
    {
        Directory.Delete(directoryPath, recursive: true);
        return true;
    }
    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
    {
        error = $"无法删除字幕目录：{ex.Message}";
        return false;
    }
}

static string GenerateUniqueNumber(string preferredNumber, HashSet<string> usedNumbers)
{
    var root = string.IsNullOrWhiteSpace(preferredNumber)
        ? "video"
        : preferredNumber.Trim();

    var candidate = root;
    var suffix = 2;

    while (!usedNumbers.Add(candidate))
    {
        candidate = $"{root}-{suffix}";
        suffix++;
    }

    return candidate;
}

static Task<Video?> LoadVideoAsync(CinemaDbContext db, string id, bool tracking)
{
    IQueryable<Video> query = db.Videos;
    if (!tracking)
    {
        query = query.AsNoTracking();
    }

    return query
        .AsSplitQuery()
        .Include(video => video.VideoTags)
            .ThenInclude(videoTag => videoTag.Tag)
                .ThenInclude(tag => tag.Category)
        .Include(video => video.SubtitleTracks)
        .FirstOrDefaultAsync(video => video.Id == id);
}

static VideoItem ToVideoItem(Video video)
{
    var tags = video.VideoTags
        .Where(videoTag => videoTag.Tag is not null)
        .OrderBy(videoTag => videoTag.Tag.Category.SortOrder)
        .ThenBy(videoTag => videoTag.Tag.SortOrder)
        .Select(videoTag => new TagItem(
            videoTag.Tag.Id,
            videoTag.Tag.CategoryId,
            videoTag.Tag.Category.Name,
            videoTag.Tag.Name,
            VideoCount: 0))
        .ToList();
    var subtitles = video.SubtitleTracks
        .OrderByDescending(track => track.IsDefault)
        .ThenBy(track => track.CreatedAt)
        .Select(ToSubtitleTrackItem)
        .ToList();

    return new VideoItem(
        video.Id,
        video.Number,
        video.Title,
        video.Description,
        video.OriginalFileName,
        video.Size,
        video.DurationSeconds,
        video.Width,
        video.Height,
        video.VideoCodec,
        $"/api/videos/{Uri.EscapeDataString(video.Id)}/stream",
        string.IsNullOrWhiteSpace(video.CoverRelativePath) ? null : $"/api/videos/{Uri.EscapeDataString(video.Id)}/cover",
        video.CreatedAt,
        video.ImportedAt,
        subtitles,
        tags);
}

static SubtitleTrackItem ToSubtitleTrackItem(SubtitleTrack track) => new(
    track.Id,
    track.Label,
    track.Language,
    track.Kind,
    track.Source,
    track.RevisionStage,
    track.Format,
    track.CueCount,
    track.IsDefault,
    $"/api/subtitles/{Uri.EscapeDataString(track.Id)}/vtt");

static MaintenanceVideoItem ToMaintenanceVideoItem(Video video)
{
    return new MaintenanceVideoItem(
        video.Id,
        video.Number,
        video.Title,
        video.OriginalFileName,
        video.Size,
        video.Status,
        string.IsNullOrWhiteSpace(video.CoverRelativePath) ? null : $"/api/videos/{Uri.EscapeDataString(video.Id)}/cover",
        video.UpdatedAt);
}

static MediaJobItem ToMediaJobItem(MediaJob job)
{
    JsonElement? result = string.IsNullOrWhiteSpace(job.ResultJson)
        ? null
        : JsonSerializer.Deserialize<JsonElement>(job.ResultJson);
    return new MediaJobItem(
        job.Id,
        job.Type,
        job.Status,
        job.VideoId,
        job.Progress,
        job.Stage,
        job.CurrentItem,
        job.Error,
        result,
        job.CancellationRequested,
        job.AttemptCount,
        job.CreatedAt,
        job.UpdatedAt,
        job.StartedAt,
        job.CompletedAt);
}

static async Task<IResult> EnqueueAiJobAsync<TInput>(
    CinemaDbContext db,
    MediaJobQueueGate queueGate,
    string type,
    string videoId,
    TInput input)
{
    await queueGate.Gate.WaitAsync();
    try
    {
        var existing = await db.MediaJobs
            .AsNoTracking()
            .Where(job => job.Type == type
                && job.VideoId == videoId
                && (job.Status == MediaJobStatus.Queued || job.Status == MediaJobStatus.Running))
            .OrderBy(job => job.CreatedAt)
            .FirstOrDefaultAsync();
        if (existing is not null)
        {
            return Results.Accepted($"/api/jobs/{existing.Id}", ToMediaJobItem(existing));
        }

        var now = DateTime.Now;
        var job = new MediaJob
        {
            Id = Guid.NewGuid().ToString("N"),
            Type = type,
            Status = MediaJobStatus.Queued,
            VideoId = videoId,
            Progress = 0,
            Stage = "等待执行",
            InputJson = JsonSerializer.Serialize(input, JsonSerializerOptions.Web),
            CreatedAt = now,
            UpdatedAt = now
        };
        db.MediaJobs.Add(job);
        await db.SaveChangesAsync();
        return Results.Accepted($"/api/jobs/{job.Id}", ToMediaJobItem(job));
    }
    finally
    {
        queueGate.Gate.Release();
    }
}

public sealed class CinemaDbContext(DbContextOptions<CinemaDbContext> options) : DbContext(options)
{
    public DbSet<Video> Videos => Set<Video>();
    public DbSet<TagCategory> TagCategories => Set<TagCategory>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<VideoTag> VideoTags => Set<VideoTag>();
    public DbSet<SubtitleTrack> SubtitleTracks => Set<SubtitleTrack>();
    public DbSet<SubtitleCue> SubtitleCues => Set<SubtitleCue>();
    public DbSet<MediaJob> MediaJobs => Set<MediaJob>();
    public DbSet<AiJobChunk> AiJobChunks => Set<AiJobChunk>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Video>(entity =>
        {
            entity.HasKey(video => video.Id);
            entity.Property(video => video.Id).HasMaxLength(32);
            entity.Property(video => video.Number).HasMaxLength(120);
            entity.Property(video => video.Title).HasMaxLength(300);
            entity.Property(video => video.Description).HasMaxLength(1200);
            entity.Property(video => video.OriginalFileName).HasMaxLength(500);
            entity.Property(video => video.StoredFileName).HasMaxLength(80);
            entity.Property(video => video.RelativePath).HasMaxLength(700);
            entity.Property(video => video.CoverRelativePath).HasMaxLength(700);
            entity.Property(video => video.VideoCodec).HasMaxLength(80);
            entity.Property(video => video.ContentType).HasMaxLength(100);
            entity.Property(video => video.Status).HasMaxLength(40);
            entity.HasIndex(video => video.RelativePath).IsUnique();
            entity.HasIndex(video => video.Number).IsUnique();
            entity.HasIndex(video => video.Status);
        });

        modelBuilder.Entity<TagCategory>(entity =>
        {
            entity.HasKey(category => category.Id);
            entity.Property(category => category.Name).HasMaxLength(80);
            entity.HasIndex(category => category.Name).IsUnique();
        });

        modelBuilder.Entity<Tag>(entity =>
        {
            entity.HasKey(tag => tag.Id);
            entity.Property(tag => tag.Name).HasMaxLength(80);
            entity.HasIndex(tag => new { tag.CategoryId, tag.Name }).IsUnique();
            entity.HasOne(tag => tag.Category)
                .WithMany(category => category.Tags)
                .HasForeignKey(tag => tag.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<VideoTag>(entity =>
        {
            entity.HasKey(videoTag => new { videoTag.VideoId, videoTag.TagId });
            entity.HasOne(videoTag => videoTag.Video)
                .WithMany(video => video.VideoTags)
                .HasForeignKey(videoTag => videoTag.VideoId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(videoTag => videoTag.Tag)
                .WithMany(tag => tag.VideoTags)
                .HasForeignKey(videoTag => videoTag.TagId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SubtitleTrack>(entity =>
        {
            entity.HasKey(track => track.Id);
            entity.Property(track => track.Id).HasMaxLength(32);
            entity.Property(track => track.VideoId).HasMaxLength(32);
            entity.Property(track => track.Label).HasMaxLength(120);
            entity.Property(track => track.Language).HasMaxLength(20);
            entity.Property(track => track.Kind).HasMaxLength(30);
            entity.Property(track => track.Source).HasMaxLength(30);
            entity.Property(track => track.SourceKey).HasMaxLength(300);
            entity.Property(track => track.Format).HasMaxLength(30);
            entity.Property(track => track.OriginalRelativePath).HasMaxLength(700);
            entity.HasIndex(track => new { track.VideoId, track.SourceKey }).IsUnique();
            entity.HasOne(track => track.Video)
                .WithMany(video => video.SubtitleTracks)
                .HasForeignKey(track => track.VideoId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SubtitleCue>(entity =>
        {
            entity.HasKey(cue => cue.Id);
            entity.Property(cue => cue.Text).HasMaxLength(4000);
            entity.HasIndex(cue => new { cue.TrackId, cue.Index }).IsUnique();
            entity.HasOne(cue => cue.Track)
                .WithMany(track => track.Cues)
                .HasForeignKey(cue => cue.TrackId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MediaJob>(entity =>
        {
            entity.HasKey(job => job.Id);
            entity.Property(job => job.Id).HasMaxLength(32);
            entity.Property(job => job.Type).HasMaxLength(40);
            entity.Property(job => job.Status).HasMaxLength(30);
            entity.Property(job => job.VideoId).HasMaxLength(32);
            entity.Property(job => job.Stage).HasMaxLength(120);
            entity.Property(job => job.CurrentItem).HasMaxLength(500);
            entity.Property(job => job.Error).HasMaxLength(2000);
            entity.HasIndex(job => new { job.Status, job.CreatedAt });
            entity.HasIndex(job => new { job.Type, job.VideoId, job.Status });
        });

        modelBuilder.Entity<AiJobChunk>(entity =>
        {
            entity.HasKey(chunk => chunk.Id);
            entity.Property(chunk => chunk.Id).HasMaxLength(32);
            entity.Property(chunk => chunk.JobId).HasMaxLength(32);
            entity.Property(chunk => chunk.Kind).HasMaxLength(40);
            entity.Property(chunk => chunk.Status).HasMaxLength(30);
            entity.Property(chunk => chunk.Error).HasMaxLength(2000);
            entity.HasIndex(chunk => new { chunk.JobId, chunk.Index }).IsUnique();
            entity.HasOne(chunk => chunk.Job)
                .WithMany(job => job.Chunks)
                .HasForeignKey(chunk => chunk.JobId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

public sealed class Video
{
    public string Id { get; set; } = "";
    public string Number { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string OriginalFileName { get; set; } = "";
    public string StoredFileName { get; set; } = "";
    public string RelativePath { get; set; } = "";
    public string? CoverRelativePath { get; set; }
    public long Size { get; set; }
    public double? DurationSeconds { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public string? VideoCodec { get; set; }
    public string ContentType { get; set; } = "video/mp4";
    public string Status { get; set; } = VideoStatus.Available;
    public bool SubtitleScanCompleted { get; set; }
    public DateTime ImportedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ICollection<VideoTag> VideoTags { get; set; } = new List<VideoTag>();
    public ICollection<SubtitleTrack> SubtitleTracks { get; set; } = new List<SubtitleTrack>();
}

public sealed class SubtitleTrack
{
    public string Id { get; set; } = "";
    public string VideoId { get; set; } = "";
    public string Label { get; set; } = "";
    public string Language { get; set; } = "und";
    public string Kind { get; set; } = SubtitleKind.Original;
    public string Source { get; set; } = SubtitleSource.External;
    public string RevisionStage { get; set; } = SubtitleRevisionStage.SourceOriginal;
    public string SourceKey { get; set; } = "";
    public string Format { get; set; } = "";
    public string OriginalRelativePath { get; set; } = "";
    public bool IsDefault { get; set; }
    public int CueCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Video Video { get; set; } = null!;
    public ICollection<SubtitleCue> Cues { get; set; } = new List<SubtitleCue>();
}

public sealed class SubtitleCue
{
    public long Id { get; set; }
    public string TrackId { get; set; } = "";
    public int Index { get; set; }
    public long StartMilliseconds { get; set; }
    public long EndMilliseconds { get; set; }
    public string Text { get; set; } = "";
    public SubtitleTrack Track { get; set; } = null!;
}

public sealed class MediaJob
{
    public string Id { get; set; } = "";
    public string Type { get; set; } = "";
    public string Status { get; set; } = MediaJobStatus.Queued;
    public string? VideoId { get; set; }
    public int Progress { get; set; }
    public string Stage { get; set; } = "等待执行";
    public string? CurrentItem { get; set; }
    public string? Error { get; set; }
    public string? InputJson { get; set; }
    public string? ResultJson { get; set; }
    public bool CancellationRequested { get; set; }
    public int AttemptCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public ICollection<AiJobChunk> Chunks { get; set; } = new List<AiJobChunk>();
}

public sealed class AiJobChunk
{
    public string Id { get; set; } = "";
    public string JobId { get; set; } = "";
    public int Index { get; set; }
    public string Kind { get; set; } = "";
    public string Status { get; set; } = AiJobChunkStatus.Queued;
    public string? OutputJson { get; set; }
    public int InputUnits { get; set; }
    public int OutputUnits { get; set; }
    public int AttemptCount { get; set; }
    public string? Error { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public MediaJob Job { get; set; } = null!;
}

public sealed class TagCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int SortOrder { get; set; }
    public ICollection<Tag> Tags { get; set; } = new List<Tag>();
}

public sealed class Tag
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public string Name { get; set; } = "";
    public int SortOrder { get; set; }
    public TagCategory Category { get; set; } = null!;
    public ICollection<VideoTag> VideoTags { get; set; } = new List<VideoTag>();
}

public sealed class VideoTag
{
    public string VideoId { get; set; } = "";
    public int TagId { get; set; }
    public Video Video { get; set; } = null!;
    public Tag Tag { get; set; } = null!;
}

public static class VideoStatus
{
    public const string Available = "Available";
    public const string Missing = "Missing";
    public const string Trashed = "Trashed";
}

public static class SubtitleKind
{
    public const string Original = "Original";
    public const string Translated = "Translated";
}

public static class SubtitleSource
{
    public const string External = "External";
    public const string Embedded = "Embedded";
    public const string SpeechRecognition = "SpeechRecognition";
    public const string AiTranslation = "AiTranslation";
}

public static class SubtitleRevisionStage
{
    public const string SourceOriginal = "SourceOriginal";
    public const string RawRecognition = "RawRecognition";
    public const string SourceCorrected = "SourceCorrected";
    public const string ChineseDraft = "ChineseDraft";
    public const string FinalPolished = "FinalPolished";
}

public static class MediaJobType
{
    public const string Sync = "Sync";
    public const string SpeechRecognition = "SpeechRecognition";
    public const string SubtitleTranslation = "SubtitleTranslation";
}

public static class MediaJobStatus
{
    public const string Queued = "Queued";
    public const string Running = "Running";
    public const string Completed = "Completed";
    public const string Failed = "Failed";
    public const string Cancelled = "Cancelled";
}

public static class AiJobChunkStatus
{
    public const string Queued = "Queued";
    public const string Failed = "Failed";
    public const string Completed = "Completed";
}

public sealed record VideoStorage(string RootPath, string OriginalsPath, string CoversPath, string SubtitlesPath, string TrashPath)
{
    public static VideoStorage FromConfiguration(IConfiguration configuration, string contentRootPath)
    {
        var rootPath = ResolveRootPath(configuration, contentRootPath);
        var originalsPath = Path.Combine(rootPath, "originals");
        var coversPath = Path.Combine(rootPath, "covers");
        var subtitlesPath = Path.Combine(rootPath, "subtitles");
        var trashPath = Path.Combine(rootPath, "trash");
        return new VideoStorage(rootPath, originalsPath, coversPath, subtitlesPath, trashPath);
    }

    public string GetAbsolutePath(string relativePath)
    {
        var absolutePath = Path.GetFullPath(Path.Combine(RootPath, relativePath));
        var root = Path.GetFullPath(RootPath);

        if (!absolutePath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Video path escapes the configured video root.");
        }

        return absolutePath;
    }

    private static string ResolveRootPath(IConfiguration configuration, string contentRootPath)
    {
        var configuredPath = configuration["VideoStorage:RootPath"];
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            configuredPath = "Videos";
        }

        return Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.GetFullPath(Path.Combine(contentRootPath, configuredPath));
    }
}

public sealed record VideoItem(
    string Id,
    string Number,
    string Title,
    string Description,
    string FileName,
    long Size,
    double? DurationSeconds,
    int? Width,
    int? Height,
    string? VideoCodec,
    string StreamUrl,
    string? CoverUrl,
    DateTime CreatedAt,
    DateTime ImportedAt,
    IReadOnlyList<SubtitleTrackItem> Subtitles,
    IReadOnlyList<TagItem> Tags);

public sealed record SubtitleTrackItem(
    string Id,
    string Label,
    string Language,
    string Kind,
    string Source,
    string RevisionStage,
    string Format,
    int CueCount,
    bool IsDefault,
    string VttUrl);

public sealed record VideoPage(
    IReadOnlyList<VideoItem> Items,
    int Total,
    int Page,
    int PageSize,
    bool HasMore);

public sealed record MaintenanceVideoItem(
    string Id,
    string Number,
    string Title,
    string FileName,
    long Size,
    string Status,
    string? CoverUrl,
    DateTime UpdatedAt);

public sealed record MediaJobItem(
    string Id,
    string Type,
    string Status,
    string? VideoId,
    int Progress,
    string Stage,
    string? CurrentItem,
    string? Error,
    JsonElement? Result,
    bool CancellationRequested,
    int AttemptCount,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt);

public sealed record TagCategoryItem(
    int Id,
    string Name,
    IReadOnlyList<TagItem> Tags);

public sealed record TagItem(
    int Id,
    int CategoryId,
    string CategoryName,
    string Name,
    int VideoCount);

public sealed record SyncResult(
    int ImportedCount,
    int AvailableCount,
    int MissingCount,
    int AnalyzedCount,
    int GeneratedCoverCount,
    int ImportedSubtitleCount,
    IReadOnlyList<SyncFailure> Failed,
    IReadOnlyList<SyncWarning> Warnings);

public sealed record SyncFailure(string FileName, string Reason);
public sealed record SyncWarning(string FileName, string Reason);
public sealed record EnrichmentResult(int AnalyzedCount, int GeneratedCoverCount);
public sealed record CoverGenerationResult(string? RelativePath, string? Warning);
public sealed record MediaAnalysis(double? DurationSeconds, int? Width, int? Height, string? VideoCodec, string? Warning);
public sealed record MediaSubtitleStream(int Index, string Codec, string? Language, string? Title);
public sealed record SubtitleStreamProbe(IReadOnlyList<MediaSubtitleStream> Streams, string? Warning);
public sealed record SubtitleImportResult(SubtitleTrack? Track, string? Warning);
public sealed record ParsedSubtitle(string Format, IReadOnlyList<ParsedSubtitleCue> Cues);
public sealed record ParsedSubtitleCue(long StartMilliseconds, long EndMilliseconds, string Text);
public sealed record SubtitleMatch(Video Video, string Language);
public sealed record JobProgress(int Percent, string Stage, string? CurrentItem);
public sealed record MediaJobHandlers(
    Func<IServiceProvider, string, CancellationToken, Func<JobProgress, Task>, Task<string>> RunSyncAsync,
    Func<IServiceProvider, string, CancellationToken, Func<JobProgress, Task>, Task<string>> RunSpeechRecognitionAsync,
    Func<IServiceProvider, string, CancellationToken, Func<JobProgress, Task>, Task<string>> RunSubtitleTranslationAsync);

public sealed class MediaJobQueueGate
{
    public SemaphoreSlim Gate { get; } = new(1, 1);
}

public sealed record UpdateVideoRequest(
    string? Number,
    string? Title,
    string? Description,
    IReadOnlyList<int>? TagIds);

public sealed record TagNameRequest(string? Name);

public sealed record PasswordRequest(string? Password);

public sealed record TagCategorySeed(string Name, int SortOrder, string[] Tags);

public sealed record MediaTools(string FfprobePath, string FfmpegPath, TimeSpan Timeout)
{
    public static MediaTools FromConfiguration(IConfiguration configuration, string contentRootPath)
    {
        var timeoutSeconds = configuration.GetValue("MediaTools:TimeoutSeconds", 90);
        return new MediaTools(
            ResolveExecutable(configuration["MediaTools:FfprobePath"], "ffprobe", contentRootPath),
            ResolveExecutable(configuration["MediaTools:FfmpegPath"], "ffmpeg", contentRootPath),
            TimeSpan.FromSeconds(Math.Clamp(timeoutSeconds, 10, 600)));
    }

    private static string ResolveExecutable(string? configuredPath, string executableName, string contentRootPath)
    {
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            return Path.IsPathRooted(configuredPath)
                ? configuredPath
                : Path.GetFullPath(Path.Combine(contentRootPath, configuredPath));
        }

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var wingetLink = Path.Combine(localAppData, "Microsoft", "WinGet", "Links", $"{executableName}.exe");
        return System.IO.File.Exists(wingetLink) ? wingetLink : executableName;
    }
}

public sealed class MediaAnalyzer(MediaTools tools)
{
    public async Task<MediaAnalysis> AnalyzeAsync(string videoPath)
    {
        try
        {
            var result = await RunAsync(tools.FfprobePath,
            [
                "-v", "error",
                "-select_streams", "v:0",
                "-show_entries", "stream=width,height,codec_name:format=duration",
                "-of", "json",
                videoPath
            ]);

            if (result.ExitCode != 0)
            {
                return new MediaAnalysis(null, null, null, null, BuildToolError("ffprobe", result.Error));
            }

            using var document = JsonDocument.Parse(result.Output);
            var root = document.RootElement;
            if (!root.TryGetProperty("streams", out var streams) || streams.GetArrayLength() == 0)
            {
                return new MediaAnalysis(null, null, null, null, "未找到可分析的视频轨道。");
            }

            var stream = streams[0];
            var width = TryGetInt32(stream, "width");
            var height = TryGetInt32(stream, "height");
            var codec = TryGetString(stream, "codec_name");
            double? duration = null;
            if (root.TryGetProperty("format", out var format))
            {
                duration = TryGetDouble(format, "duration");
            }

            if (width is null || height is null)
            {
                return new MediaAnalysis(duration, width, height, codec, "视频轨道缺少分辨率信息。");
            }

            return new MediaAnalysis(duration, width, height, codec, null);
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or UnauthorizedAccessException or JsonException or Win32Exception)
        {
            return new MediaAnalysis(null, null, null, null, $"视频信息提取失败：{ex.Message}");
        }
    }

    public async Task<SubtitleStreamProbe> GetSubtitleStreamsAsync(string videoPath)
    {
        try
        {
            var result = await RunAsync(tools.FfprobePath,
            [
                "-v", "error",
                "-select_streams", "s",
                "-show_entries", "stream=index,codec_name:stream_tags=language,title",
                "-of", "json",
                videoPath
            ]);

            if (result.ExitCode != 0)
            {
                return new SubtitleStreamProbe([], BuildToolError("ffprobe", result.Error));
            }

            using var document = JsonDocument.Parse(result.Output);
            var streams = new List<MediaSubtitleStream>();
            if (document.RootElement.TryGetProperty("streams", out var streamElements))
            {
                foreach (var stream in streamElements.EnumerateArray())
                {
                    var index = TryGetInt32(stream, "index");
                    if (index is null)
                    {
                        continue;
                    }

                    string? language = null;
                    string? title = null;
                    if (stream.TryGetProperty("tags", out var tags))
                    {
                        language = TryGetString(tags, "language");
                        title = TryGetString(tags, "title");
                    }

                    streams.Add(new MediaSubtitleStream(
                        index.Value,
                        TryGetString(stream, "codec_name") ?? "unknown",
                        language,
                        title));
                }
            }

            return new SubtitleStreamProbe(streams, null);
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or UnauthorizedAccessException or JsonException or Win32Exception)
        {
            return new SubtitleStreamProbe([], $"内嵌字幕检测失败：{ex.Message}");
        }
    }

    public async Task<string?> ExtractSubtitleAsync(string videoPath, int streamIndex, string outputPath)
    {
        try
        {
            var result = await RunAsync(tools.FfmpegPath,
            [
                "-hide_banner", "-loglevel", "error", "-y",
                "-i", videoPath,
                "-map", $"0:{streamIndex}",
                "-c:s", "webvtt",
                "-f", "webvtt",
                outputPath
            ]);

            if (result.ExitCode != 0 || !System.IO.File.Exists(outputPath) || new FileInfo(outputPath).Length == 0)
            {
                if (System.IO.File.Exists(outputPath))
                {
                    System.IO.File.Delete(outputPath);
                }
                return BuildToolError("ffmpeg", result.Error);
            }

            return null;
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or UnauthorizedAccessException or Win32Exception)
        {
            return $"内嵌字幕提取失败：{ex.Message}";
        }
    }

    public async Task<string?> GenerateCoverAsync(string videoPath, string outputPath, double? durationSeconds)
    {
        try
        {
            var seekSeconds = durationSeconds is > 1 ? durationSeconds.Value / 2 : 0;
            var result = await RunAsync(tools.FfmpegPath,
            [
                "-hide_banner", "-loglevel", "error", "-y",
                "-ss", seekSeconds.ToString("0.###", CultureInfo.InvariantCulture),
                "-i", videoPath,
                "-frames:v", "1",
                "-vf", "scale=1280:-2:force_original_aspect_ratio=decrease",
                "-q:v", "3",
                outputPath
            ]);

            if (result.ExitCode != 0 || !System.IO.File.Exists(outputPath) || new FileInfo(outputPath).Length == 0)
            {
                return BuildToolError("ffmpeg", result.Error);
            }

            return null;
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or UnauthorizedAccessException or Win32Exception)
        {
            return $"自动封面生成失败：{ex.Message}";
        }
    }

    private async Task<MediaToolResult> RunAsync(string executablePath, IReadOnlyList<string> arguments)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = executablePath,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }
        };
        foreach (var argument in arguments)
        {
            process.StartInfo.ArgumentList.Add(argument);
        }

        if (!process.Start())
        {
            throw new InvalidOperationException($"无法启动 {Path.GetFileName(executablePath)}。");
        }

        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(tools.Timeout);
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync();
            return new MediaToolResult(-1, await outputTask, $"处理超过 {tools.Timeout.TotalSeconds:0} 秒，已终止。");
        }

        return new MediaToolResult(process.ExitCode, await outputTask, await errorTask);
    }

    private static int? TryGetInt32(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var value) && value.TryGetInt32(out var parsed) ? parsed : null;

    private static string? TryGetString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var value) ? value.GetString() : null;

    private static double? TryGetDouble(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var number))
        {
            return number;
        }

        return value.ValueKind == JsonValueKind.String
            && double.TryParse(value.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var textNumber)
                ? textNumber
                : null;
    }

    private static string BuildToolError(string toolName, string error)
    {
        var detail = string.IsNullOrWhiteSpace(error) ? "没有返回错误详情。" : error.Trim();
        if (detail.Length > 300)
        {
            detail = detail[..300];
        }
        return $"{toolName} 处理失败：{detail}";
    }

    private sealed record MediaToolResult(int ExitCode, string Output, string Error);
}

public sealed class MediaJobWorker(
    IServiceScopeFactory scopeFactory,
    MediaJobHandlers handlers,
    ILogger<MediaJobWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RecoverInterruptedJobsAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var pending = await StartNextJobAsync(stoppingToken);
                if (pending is null)
                {
                    await Task.Delay(500, stoppingToken);
                    continue;
                }

                await ExecuteJobAsync(pending, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "后台任务 Worker 循环发生异常。");
                await Task.Delay(1000, stoppingToken);
            }
        }
    }

    private async Task RecoverInterruptedJobsAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CinemaDbContext>();
        var jobs = await db.MediaJobs
            .Where(job => job.Status == MediaJobStatus.Running)
            .ToListAsync(cancellationToken);
        var now = DateTime.Now;
        foreach (var job in jobs)
        {
            if (job.CancellationRequested)
            {
                job.Status = MediaJobStatus.Cancelled;
                job.Stage = "已取消";
                job.CompletedAt = now;
            }
            else
            {
                job.Status = MediaJobStatus.Queued;
                job.Stage = "服务重启后等待恢复";
                job.StartedAt = null;
            }
            job.CurrentItem = null;
            job.UpdatedAt = now;
        }

        if (jobs.Count > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<PendingMediaJob?> StartNextJobAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CinemaDbContext>();
        var job = await db.MediaJobs
            .Where(item => item.Status == MediaJobStatus.Queued)
            .OrderBy(item => item.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (job is null)
        {
            return null;
        }

        var now = DateTime.Now;
        job.Status = MediaJobStatus.Running;
        job.Stage = "正在启动";
        job.CurrentItem = null;
        job.Error = null;
        job.CancellationRequested = false;
        job.AttemptCount++;
        job.StartedAt = now;
        job.CompletedAt = null;
        job.UpdatedAt = now;
        await db.SaveChangesAsync(cancellationToken);
        return new PendingMediaJob(job.Id, job.Type);
    }

    private async Task ExecuteJobAsync(PendingMediaJob pending, CancellationToken stoppingToken)
    {
        using var executionCancellation = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        var cancellationMonitor = MonitorCancellationAsync(pending.Id, executionCancellation, stoppingToken);
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var resultJson = pending.Type switch
            {
                MediaJobType.Sync => await handlers.RunSyncAsync(
                    scope.ServiceProvider,
                    pending.Id,
                    executionCancellation.Token,
                    progress => ReportProgressAsync(pending.Id, progress, executionCancellation.Token)),
                MediaJobType.SpeechRecognition => await handlers.RunSpeechRecognitionAsync(
                    scope.ServiceProvider,
                    pending.Id,
                    executionCancellation.Token,
                    progress => ReportProgressAsync(pending.Id, progress, executionCancellation.Token)),
                MediaJobType.SubtitleTranslation => await handlers.RunSubtitleTranslationAsync(
                    scope.ServiceProvider,
                    pending.Id,
                    executionCancellation.Token,
                    progress => ReportProgressAsync(pending.Id, progress, executionCancellation.Token)),
                _ => throw new InvalidOperationException($"不支持的任务类型：{pending.Type}")
            };
            await CompleteJobAsync(pending.Id, resultJson, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Leave the job as Running so the next service start can recover it.
        }
        catch (OperationCanceledException)
        {
            await CancelJobAsync(pending.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "后台任务 {JobId} 执行失败。", pending.Id);
            await FailJobAsync(pending.Id, ex.Message);
        }
        finally
        {
            await executionCancellation.CancelAsync();
            try
            {
                await cancellationMonitor;
            }
            catch (OperationCanceledException)
            {
                // The monitor is expected to stop when execution completes.
            }
        }
    }

    private async Task MonitorCancellationAsync(
        string id,
        CancellationTokenSource executionCancellation,
        CancellationToken stoppingToken)
    {
        while (!executionCancellation.IsCancellationRequested)
        {
            await Task.Delay(500, executionCancellation.Token);
            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<CinemaDbContext>();
            var state = await db.MediaJobs
                .AsNoTracking()
                .Where(job => job.Id == id)
                .Select(job => new { job.Status, job.CancellationRequested })
                .FirstOrDefaultAsync(executionCancellation.Token);
            if (state is null || state.CancellationRequested || state.Status != MediaJobStatus.Running)
            {
                await executionCancellation.CancelAsync();
                return;
            }
            stoppingToken.ThrowIfCancellationRequested();
        }
    }

    private async Task ReportProgressAsync(string id, JobProgress progress, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CinemaDbContext>();
        var job = await db.MediaJobs.FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new OperationCanceledException("任务记录已不存在。", cancellationToken);
        if (job.CancellationRequested || job.Status != MediaJobStatus.Running)
        {
            throw new OperationCanceledException("任务已请求取消。", cancellationToken);
        }

        job.Progress = Math.Max(job.Progress, Math.Clamp(progress.Percent, 0, 99));
        job.Stage = progress.Stage;
        job.CurrentItem = progress.CurrentItem;
        job.UpdatedAt = DateTime.Now;
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task CompleteJobAsync(string id, string resultJson, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CinemaDbContext>();
        var job = await db.MediaJobs.FirstAsync(item => item.Id == id, cancellationToken);
        var now = DateTime.Now;
        if (job.CancellationRequested)
        {
            job.Status = MediaJobStatus.Cancelled;
            job.Stage = "已取消";
        }
        else
        {
            job.Status = MediaJobStatus.Completed;
            job.Stage = "已完成";
            job.Progress = 100;
            job.ResultJson = resultJson;
        }
        job.CurrentItem = null;
        job.CompletedAt = now;
        job.UpdatedAt = now;
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task CancelJobAsync(string id)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CinemaDbContext>();
        var job = await db.MediaJobs.FirstOrDefaultAsync(item => item.Id == id);
        if (job is null)
        {
            return;
        }
        job.Status = MediaJobStatus.Cancelled;
        job.Stage = "已取消";
        job.CurrentItem = null;
        job.CompletedAt = DateTime.Now;
        job.UpdatedAt = DateTime.Now;
        await db.SaveChangesAsync();
    }

    private async Task FailJobAsync(string id, string error)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CinemaDbContext>();
        var job = await db.MediaJobs.FirstOrDefaultAsync(item => item.Id == id);
        if (job is null)
        {
            return;
        }
        job.Status = MediaJobStatus.Failed;
        job.Stage = "执行失败";
        job.CurrentItem = null;
        job.Error = error.Length > 2000 ? error[..2000] : error;
        job.CompletedAt = DateTime.Now;
        job.UpdatedAt = DateTime.Now;
        await db.SaveChangesAsync();
    }

    private sealed record PendingMediaJob(string Id, string Type);
}

public sealed class AdminCredentialStore(string path)
{
    private const int Iterations = 600_000;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private AdminCredential? _credential;

    public bool IsConfigured => _credential is not null;

    public async Task InitializeAsync()
    {
        if (!System.IO.File.Exists(path))
        {
            return;
        }

        try
        {
            await using var stream = System.IO.File.OpenRead(path);
            _credential = await JsonSerializer.DeserializeAsync<AdminCredential>(stream)
                ?? throw new InvalidOperationException("管理员凭据文件为空。");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or FormatException)
        {
            throw new InvalidOperationException("无法读取管理员凭据文件。", ex);
        }
    }

    public async Task<bool> TryInitializeAsync(string password)
    {
        await _gate.WaitAsync();
        try
        {
            if (_credential is not null || System.IO.File.Exists(path))
            {
                return false;
            }

            var salt = RandomNumberGenerator.GetBytes(16);
            var hash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                Iterations,
                HashAlgorithmName.SHA256,
                32);
            var credential = new AdminCredential(
                Version: 1,
                Iterations,
                Convert.ToBase64String(salt),
                Convert.ToBase64String(hash));

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var temporaryPath = $"{path}.{Guid.NewGuid():N}.tmp";
            await System.IO.File.WriteAllTextAsync(
                temporaryPath,
                JsonSerializer.Serialize(credential, new JsonSerializerOptions { WriteIndented = true }));
            System.IO.File.Move(temporaryPath, path, overwrite: false);
            _credential = credential;
            return true;
        }
        finally
        {
            _gate.Release();
        }
    }

    public bool Verify(string password)
    {
        var credential = _credential;
        if (credential is null || credential.Version != 1 || credential.Iterations < 100_000)
        {
            return false;
        }

        try
        {
            var salt = Convert.FromBase64String(credential.Salt);
            var expectedHash = Convert.FromBase64String(credential.Hash);
            var actualHash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                credential.Iterations,
                HashAlgorithmName.SHA256,
                expectedHash.Length);
            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private sealed record AdminCredential(int Version, int Iterations, string Salt, string Hash);
}
