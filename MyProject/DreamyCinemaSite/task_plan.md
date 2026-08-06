# Task Plan

Goal: Build a mobile-first, authenticated local video library with a Vue frontend and ASP.NET Core API.

## Phases

1. [complete] Inspect current MVP and project constraints.
2. [complete] Add SQLite/EF Core video metadata storage and sync endpoint.
3. [complete] Update web UI with manual sync flow.
4. [complete] Validate build and non-destructive sync behavior.
5. [complete] Redesign the video library page for mobile browsing.
6. [complete] Implement selected mobile UI and many-to-many tag data model.
7. [complete] Add video metadata editing and tag assignment.
8. [complete] Fix edit drawer visibility and interaction regression.
9. [complete] Change multi-tag filtering to require every selected tag.
10. [complete] Add real cover storage, serving, and editing.
11. [complete] Add tag category and tag management.
12. [complete] Add combined text search and video sorting.
13. [complete] Add video trash, restore, permanent deletion, and missing-record cleanup.
14. [complete] Add single-admin authentication, CSRF protection, and login rate limiting.
15. [complete] Enable private-LAN access on the default development port.
16. [complete] Fix mobile inline video stacking and scrolling behavior.
17. [complete] Add a native-video overlay fallback for Android vendor browsers.
18. [complete] Replace the legacy static frontend with Vue 3, TypeScript, and Vite.
19. [complete] Rebuild authentication, library, editing, tag management, and maintenance as Vue views/components.
20. [complete] Add a dedicated mobile playback route without a fixed filter overlay.
21. [complete] Integrate Vue production builds with ASP.NET Core and verify the end-to-end application.
22. [complete] Merge cover and preview into one media panel with inline playback.
23. [complete] Keep a separate explicit action for opening the dedicated playback route and verify both flows.
24. [complete] Fix Xiaomi Browser edit-drawer cover and field row overlap.
25. [completed] Make the editor body scrollable and keep save actions reachable at mobile viewport heights.
26. [completed] Extract video metadata and generate covers during sync, including backfilling existing managed videos.
27. [completed] Regenerate an automatic cover on sync whenever a video currently has no cover.
28. [completed] Add server-side video pagination and mobile infinite loading with lazy-loaded covers.
29. [completed] Add subtitle track/cue storage, external and embedded subtitle import, WebVTT delivery, and player track selection.
30. [completed] Add a durable background-job framework shared by sync, speech recognition, and AI translation.
31. [complete] Add optional speech recognition plus provider-neutral AI subtitle translation with chunk validation, glossary, usage limits, and resume.
32. [complete] Add subtitle review/editing, partial retranslation, bilingual tracks, quality warnings, and export.
33. [complete] Add consistent metadata/cover/subtitle backup packages and backup management.
34. [complete] Add validated restore with pre-restore snapshots and rollback.
35. [complete] Add mobile batch organization, media re-analysis, cover regeneration, and codec compatibility warnings.
36. [complete] Add administrator password rotation, session invalidation, and management audit logs.
37. [pending] Add Windows production publishing, automatic startup, health checks, and structured logs.
38. [pending] Complete integration/E2E/performance/security validation and release documentation.
39. [complete] Add mobile-browser-compatible self-rendered subtitle modes.
40. [in_progress] Add caption-preserving fullscreen playback with a viewport fallback.
45. [complete] Add a native Android library and player client backed by the existing ASP.NET Core API.
46. [complete] Bring the native Android client to functional parity with the requested mobile web management workflow.

## Decisions

### Phase 31 execution plan (home server)

1. [complete] Audit the Windows/NVIDIA/Python/.NET/Node/FFmpeg environment and preserve the existing working tree.
2. [complete] Implement configurable FFmpeg audio chunking plus the real loopback faster-whisper HTTP provider with cancellation and per-chunk resume.
3. [complete] Implement the real loopback llama.cpp OpenAI-compatible provider with structured Cue-ID output, context, glossary/character names, validation, and retry.
4. [complete] Add non-committed home-server configuration and repeatable PowerShell setup/start scripts for services bound only to `127.0.0.1`.
5. [complete] Install and verify the pinned faster-whisper/CTranslate2 runtime and official llama.cpp CUDA Windows release with Qwen3-8B Q4_K_M.
6. [complete] Validate providers and restart/cancel/resume behavior against isolated data, then run the full build, type check, Playwright, and dependency audits.
7. [complete] Benchmark a ten-minute isolated video with production 300-second speech chunks and record recognition speed, GPU memory, and system memory.
8. [complete] Fix CJK prompt serialization, add semantic translation quality checks, prevent bad context from cascading, and support explicit retranslation without reusing an existing generated track.
9. [complete] Repeat the real-video translation through the deployed website and perform final manual phone subtitle acceptance.

### Phase 32 implementation plan

1. [complete] Add paired Cue review APIs, manual-edit protection, deterministic quality warnings, and SRT/WebVTT export.
2. [complete] Add selected-Cue retranslation that updates only a review-stage track and preserves unrelated/manual cues.
3. [complete] Add bilingual track generation without changing either source timeline or reviewed Chinese text.
4. [complete] Build a mobile review page with video seeking, source/Chinese comparison, editing, selection, warnings, and export actions.
5. [complete] Verify full workflow with isolated E2E, mobile visual QA, builds, and dependency audits.

### Phase 33 implementation plan

1. [complete] Audit database, media paths, authentication, and configuration boundaries for a safe backup format.
2. [complete] Add consistent SQLite snapshot creation and a versioned ZIP manifest with checksums.
3. [complete] Include covers and durable subtitle/configuration assets while excluding original videos and secrets by default.
4. [complete] Add authenticated backup list/create/download/delete APIs and a mobile management interface.
5. [complete] Verify backup contents, consistency, authorization, frontend behavior, builds, and dependency audits.

### Phase 34 implementation plan

1. [complete] Define restore maintenance coordination, package/schema/path validation, and exact rollback boundaries.
2. [complete] Add validated backup import plus server-backup restore APIs with CSRF/authentication and upload limits.
3. [complete] Create an automatic pre-restore snapshot, restore SQLite through the online backup API, swap cover/subtitle assets, apply portable settings, and roll back every changed layer on failure.
4. [complete] Add mobile import/restore controls with explicit destructive confirmation and refresh restored library state.
5. [complete] Verify successful restore and injected post-database failure rollback in isolated runtimes, then run builds, E2E, package audits, and live deployment checks.

### Phase 35 implementation plan

1. [completed] Add bounded batch tag editing, batch trash with file rollback, and resolution-tag suggestion/apply APIs.
2. [completed] Add durable media-maintenance jobs for metadata reanalysis and automatic-cover regeneration with progress, cancellation, retry, and restart recovery.
3. [completed] Surface explicit browser codec compatibility warnings and build mobile selection/bulk-maintenance controls.
4. [completed] Verify batch relationships, file moves, resolution tags, media jobs, and mobile layout in an isolated E2E runtime.
5. [completed] Run final builds and repository checks, document phase 35, and deploy the updated service to port 5210.

### Phase 36 implementation plan

1. [completed] Add a credential security stamp, authenticated password change, and immediate invalidation of every existing login Cookie.
2. [completed] Expose session expiration and clear re-login reasons in the Vue authentication flow.
3. [completed] Persist bounded management audit events without request bodies, passwords, CSRF tokens, or Cookies, and add a mobile account/security drawer.
4. [completed] Verify authentication, CSRF, old-session invalidation, audit redaction, backup/restore compatibility, and mobile behavior in an isolated E2E runtime.
5. [completed] Run final builds and dependency checks, document phase 36, and deploy the updated service to port 5210.

### Phase 39 implementation plan

1. [complete] Extract device-local subtitle track/display preferences and a reusable active-Cue renderer.
2. [complete] Add self-rendered overlay and below-video compatibility captions to inline and dedicated playback.
3. [complete] Add explicit display-mode controls and focused mobile playback assertions.
4. [complete] Run Vue/.NET builds, browser QA, and prepare Xiaomi handset verification.

### Phase 40 implementation plan

1. [complete] Add dedicated-player container fullscreen state and standards/vendor exit handling.
2. [complete] Add a CSS viewport fallback and caption-safe fullscreen layout.
3. [complete] Suppress the native fullscreen control where supported and add explicit mobile controls.
4. [complete] Extend focused mobile E2E, build, visually inspect, and prepare Xiaomi handset verification.

Phase 40 was an experiment and is being rolled back after real Xiaomi Browser validation showed that the vendor fullscreen layer still excludes the DOM caption UI.

### Phase 41 rollback plan

1. [complete] Remove the dedicated caption-fullscreen controls, state, and viewport fallback without changing normal subtitle modes.
2. [complete] Restore the phase 39 focused subtitle assertions and rebuild the production frontend.
3. [complete] Record the handset result and evaluate offline FFmpeg hard-subtitle derivatives.

### Phase 42 hard-subtitle derivative plan

1. [complete] Extend video persistence and API contracts with a generated hard-subtitle variant and original-file availability.
2. [complete] Add a durable hard-subtitle FFmpeg job with progress, cancellation, retry, atomic output, and media validation.
3. [complete] Add player source selection, generation controls, derivative deletion, and guarded original deletion.
4. [complete] Add isolated backend/Pixel 7 coverage for generation, switching, Range playback, and original deletion.
5. [complete] Run final builds/checks and document storage/deployment behavior.

### Phase 43 NVENC fallback plan

1. [complete] Detect NVIDIA encoder initialization failures without masking unrelated FFmpeg errors.
2. [complete] Retry the same hard-subtitle job with `libx264` and report the actual encoder in progress/result data.
3. [complete] Add a deterministic fallback regression that does not require changing the machine GPU/driver.
4. [complete] Build, run the focused regression, and document automatic fallback behavior.

### Phase 44 hard-subtitle style plan

1. [complete] Define validated per-generation subtitle style settings and lower the default font size from 22 to 18.
2. [complete] Persist the selected style with the durable job and generated derivative, then apply it through FFmpeg/libass.
3. [complete] Add a mobile settings drawer with a real-cover preview for font, size, opacity, colors, outline, and bottom margin.
4. [complete] Extend focused E2E coverage for defaults, custom style submission, persistence, and regeneration.
5. [complete] Build, visually inspect, deploy to port 5210, and document the style controls.

### Phase 45 Android client plan

1. [complete] Lock the native Android architecture, authentication boundary, API contracts, and reproducible build toolchain.
2. [complete] Add server configuration, Cookie/CSRF session handling, login, and session recovery.
3. [complete] Add the mobile video library with search, sort, categorized multi-tag intersection filtering, paging, and authenticated covers.
4. [complete] Add Media3/ExoPlayer playback with original/hard-subtitle source selection, side-loaded WebVTT tracks, fullscreen captions, and playback errors.
5. [complete] Build a debug APK, run focused tests/static checks, document installation and LAN requirements, and record the remaining real-device acceptance test.

### Phase 46 Android parity plan

1. [complete] Fix system-bar Insets and make video-card tags directly add intersection filters without opening playback.
2. [complete] Add a native management hub with sync-job progress, tag administration, trash maintenance, and backup operations.
3. [complete] Add player-side AI speech/translation task controls and hard-subtitle generation/deletion controls using the existing durable-job APIs.
4. [complete] Add authenticated streaming backup download/upload helpers, preserve CSRF/session-expiry behavior, and expose clear operation states.
5. [complete] Build/lint/test the expanded APK, inspect the packaged manifest, update documentation, and prepare a real Xiaomi acceptance checklist.

- `Videos` root is the manual drop/download directory.
- Sync is explicit via button/API, not performed on every page visit.
- Managed videos are moved to `Videos/originals/yyyy/MM/{videoId}.mp4`.
- SQLite uses `EnsureCreated` for this early MVP stage.
- Tags are categorized, and video/tag relations are many-to-many through `VideoTags`.
- Tag categories and tags can be created, renamed, and deleted from the mobile management drawer.
- Filtering uses intersection semantics: a video must contain every selected tag, including tags from the same category.
- Text search and sorting are server-side and combine with selected-tag intersection filtering.
- Video lists are paged server-side; mobile browsing appends the next page as the load sentinel enters view.
- Video edits update number, title, description, and `VideoTags` atomically through the API.
- Cover images are stored under `Videos/covers/yyyy/MM` and referenced by relative path from the video record.
- Normal deletion moves managed files to `Videos/trash/{videoId}`; permanent deletion is limited to trashed or missing records.
- The site is private by default with one cookie-authenticated administrator; initial password setup is restricted to loopback access.
- The default HTTP development profile listens on all local interfaces at port 5210 for same-LAN phone access.
- Frontend source uses Vue 3, TypeScript, Vite, Vue Router, and Pinia; Vite proxies APIs during development and ASP.NET Core serves the production build on port 5210.
- Video playback moves to a dedicated route so native Android video overlays do not compete with the library's fixed filters.
- Video cards use one merged media panel: cover as poster, center action for inline playback, and a separate labeled action for the dedicated playback page.
- Editor cover controls live in an explicit layout group with a concrete responsive block height; form labels use explicit text elements for mobile browser layout stability.
- Editor drawers use a constrained flex column: only the content body scrolls, while destructive/save actions remain in a fixed footer.
- Media inspection uses configurable FFmpeg/ffprobe executables; analysis failures are reported as warnings and do not block video import.
- Automatic covers are generated whenever no cover exists; manual covers remain untouched while present, and removing one makes the video eligible again on the next sync.
- During active development, schema and architecture may be rewritten without preserving old local database compatibility.
- Final product scope is a single-admin, local-storage, trusted-LAN application using folder-based import; uploads, multi-user access, public exposure, and playback-progress tracking remain optional.
- AI subtitle import, optional speech recognition, translation, human review, bilingual playback, and export are core final-product requirements.
- Subtitle files are parsed into stable database cues; WebVTT is generated from those cues for browser playback rather than treated as the editing source of truth.
- Long-running media operations use durable SQLite jobs; HTTP requests enqueue work and the single background worker owns execution, progress, cancellation, retry, and restart recovery.
- AI subtitle providers use a configurable OpenAI-compatible HTTP boundary; transcription and translation jobs persist chunk checkpoints and validate stable cue identity before creating tracks.
- AI processing is disabled by default on development machines. Explicit `Mock` providers validate orchestration in isolated tests; real faster-whisper and llama.cpp adapters will use loopback services on the home server.
- The final implementation sequence and acceptance criteria are maintained in `ROADMAP.md`.

## Errors Encountered

| Error | Attempt | Resolution |
|-------|---------|------------|
| NuGet vulnerability warning for transitive `SQLitePCLRaw.lib.e_sqlite3` 2.1.11 | Added `Microsoft.EntityFrameworkCore.Sqlite` | Added direct `SQLitePCLRaw.bundle_e_sqlite3` 3.0.3 package reference |
| Android Gradle Wrapper download remained at a zero-byte partial file | Ran the first Gradle build through `services.gradle.org` on the company network | Stopped only the verified wrapper Java process, downloaded the official Gradle 9.1 distribution with curl, and used the ignored local distribution for verification |
| Maven Central returned 403 when all Gradle traffic was forced through the local proxy | Added JVM proxy properties for the first dependency retry | Used direct Maven Central access; only the missing Google lint artifact required a hybrid proxy/non-proxy retry |
| Android Lint reported two blocking resource/property errors | Ran `lintDebug` after the first APK build | Removed the API-27-only cosmetic theme item and escaped the Windows SDK drive separator; offline lint then passed |
| SQLite `DateTimeOffset` ORDER BY is unsupported | Tested `/api/videos` after sync | Changed entity/API timestamp fields to `DateTime` |
| Browser visual QA blocked access to `http://127.0.0.1:5057` by enterprise policy | Tried in-app browser mobile viewport check | Stopped browser automation and used build/API/static checks instead |
| Inline JavaScript validation command had an escaped-regex syntax error | First Node.js parse check | Re-ran validation by locating script boundaries without regex |
| Build output was locked by the running development server (PID 17464) | Ran `dotnet build` while port 5057 was active | Stop the old server, rebuild, and restart it before API verification |
| Installed Node.js runtime does not provide global `fetch` | Tried a secondary raw JSON assertion with `node -e` | Use `Invoke-WebRequest` and explicit `ConvertFrom-Json` instead |
| Source-location `rg` pattern had an unclosed regex group | Combined several escaped patterns | Re-run with fixed-string or separate simple patterns |
| First missing-record test reported false counts | Wrapped JSON arrays with PowerShell `@(...)`, which treated an empty response array as one nested item | Recreated the case and parsed raw response content with `ConvertFrom-Json` |
| First auth lifecycle script produced invalid redirect/status results | Used PowerShell's reserved `$HOME` name case-insensitively and reused the anonymous CSRF token after sign-in | Use curl for redirect checks and refresh `/api/auth/session` after identity changes |
| Public login page redirected to itself | Static files were served after fallback authorization | Serve static files after the explicit index redirect but before API authorization |
| Anonymous `/index.html` verification returned the login page | Tried to inspect the served player markup without an authenticated cookie | Validate the source markup and inline script directly; handset verification remains the authoritative native-player check |
| Source search regex had an unclosed group | Combined startup/static-file patterns in one `rg` expression | Re-run with fixed-string searches and inspect the relevant Program.cs sections directly |
| First Vue dependency install ran under stale Node 16 PATH | Installed Node 24 during the current Codex process, but bare `node` still resolved to the old runtime | Invoke `C:\Program Files\nodejs\npm.cmd` explicitly for migration commands |
| `lucide-vue-next` latest package is deprecated | Installed the queried latest package name | Replace it with the maintained `@lucide/vue` package before the first build |
| `vue-tsc` cannot load TypeScript 7 compiler internals | Ran the first typed Vue build with latest TypeScript 7.0.2 | Use TypeScript 6.0.3, which satisfies the current Vue toolchain peer requirements, and retain strict type checking |
| Initial TS 6 config referenced a removed Vue Node preset and deprecated `baseUrl` | Reused older Vite scaffold tsconfig conventions | Extend the current `@vue/tsconfig/tsconfig.json` for Vite config files and resolve aliases without `baseUrl` |
| NVM could not install Node 24 from the configured npmmirror | Tried to make the new Vite toolchain available through the user's existing NVM setup | Switched NVM's Node mirror to the official Node.js distribution URL, installed/selected 24.18.0, then removed the duplicate winget installation |
| `npm audit` returned 404 from npmmirror | Ran the production dependency audit with the machine's configured npm registry | Re-run only the audit against `https://registry.npmjs.org` because npmmirror does not implement the advisory endpoint |
| Official-registry audit reported no lockfile | Re-ran `npm audit` from the ASP.NET project root instead of the Vue project directory | Run npm audit from `DreamyCinema.Web`, where `package-lock.json` is located |
| Subtitle foundation compile used `.Value` on a nullable reference record | First phase 29 backend build | Treat the null-checked `SubtitleMatch` as a reference directly |
| Full backend build could not replace the running executable | Built phase 29 while the real service PID 20252 was using the Debug output | Verified the process path, stopped the service, then rebuild before restarting it |
| Completed background job did not refresh the Vue list | First phase 30 E2E cancellation/retry run | Serialize persisted job results with web camelCase and retain a frontend fallback for existing PascalCase records |
| Parallel backend/frontend builds raced on hashed `wwwroot` assets | Rebuilt both projects concurrently after the job-result fix | Keep Vite and ASP.NET builds serial because they share the production static output directory |
| Phase 31 backend build could not replace the running executable | Built against the live Debug output while PID 23632 served the real library | Compile to an isolated output directory first; stop the real service only for the final deployment build |
| Isolated intermediate output under project root produced duplicate assembly attributes | Set `BaseIntermediateOutputPath` to `.artifacts`, which SDK default globs treated as source on the next evaluation | Removed the generated directory and place verification output under the Windows temp directory outside the project |
| Phase 32 test setup command was rejected by command safety policy | Combined a recursive temp cleanup with fixture creation in one command | Use a new unique verified temp directory and perform any cleanup separately after the test process stops |
| Subtitle preference build failed strict TypeScript indexing | Accessed `matches[matches.length - 1]` after a length guard that `noUncheckedIndexedAccess` does not narrow | Use `matches.at(-1)` and an explicit undefined guard |
| Recursive cleanup of verified phase 32 temp directories was rejected by command policy | Attempted PowerShell `Remove-Item -Recurse` after resolving every target under `%TEMP%` | Leave the isolated temp artifacts in `%TEMP%`; they contain only generated test media/build outputs and do not affect the project or real library |
| Isolated backup creation could not reopen the SQLite snapshot for packaging | First phase 33 live-server backup test left the destination snapshot handle in the Microsoft.Data.Sqlite connection pool | Disable pooling for all short-lived backup source, destination, and validation connections so disposal releases file handles immediately |
| Unified PTY could not launch the WindowsApps PowerShell shim for the isolated server | First phase 33 test-server start attempted the default PTY shell and received access denied | Start the long-running isolated server without PTY through the system Windows PowerShell executable |
| First phase 33 delete verification treated the backup JSON array as one nested PowerShell object | Wrapped the `Invoke-RestMethod` result and sent an array of IDs as one route value | Parse the raw response content with `ConvertFrom-Json`, iterate the `.id` values explicitly, and verify both deletions plus the final 404 |
| Phase 35 E2E read resolution tags before the Vue click handler's async request completed | Playwright `click()` returned after dispatch while the drawer operation was still running | Wait for the bulk drawer to close as the explicit completion signal before querying the API |
| Phase 35 E2E mistook the previous completed maintenance panel for a newly requested cover job | Asserted `Completed` immediately after dispatch while the new job request was still in flight | Wait for the bulk drawer to close before observing the replacement active job and its result |
| Phase 35 SQLite diagnostics could not use the unavailable Python alias or initialize `Microsoft.Data.Sqlite` from bare PowerShell | Tried ad-hoc direct database reads after the E2E timing failure | Queried through a freshly authenticated browser session, which confirmed both resolution relationships were persisted |
| Unified exec could not send Ctrl+C to the non-PTY phase 35 test server | Attempted graceful interrupt through `write_stdin` | Resolve the listener PID by port, verify its command line references the isolated phase 35 DLL, then stop only that verified process |
| npm production audit returned 404 from the configured npmmirror registry | The mirror does not implement npm's security advisory endpoint | Re-ran the same read-only audit with `--registry=https://registry.npmjs.org`; it completed with 0 vulnerabilities |
| First phase 36 backend patch did not match a distant `Program.cs` context | Tried to add the audit store and several separated authentication changes in one patch | Confirmed no partial changes were applied, then split the implementation into one new file and small local patches |
| Phase 36 frontend build command referenced a missing system Node path | Tried `C:\Program Files\nodejs\npm.cmd`, but this machine uses NVM | Resolved the active Node 24.18.0 toolchain at `D:\nvm\nodejs` and used the current `npm` command |
| First phase 36 isolated publish targeted the live Debug output before copying to the test directory | `dotnet publish -o` still built through `bin\Debug`, which PID 23760 holds open | Confirmed no test process started and moved both build output and intermediate output to a new verified `%TEMP%` root |
| Changing phase 36 `BaseIntermediateOutputPath` caused duplicate assembly attributes | The SDK stopped excluding the project's existing `obj` tree when the base path moved | Kept the default intermediate exclusion and reused the already successful isolated `OutDir` build for the test runtime |
| First phase 36 Playwright run matched both new-password inputs | `getByLabel("新密码")` also matched `确认新密码` in strict mode | Changed the locator to exact label matching and restarted with a fresh isolated credential directory |
| First phase 36 dependency-audit command used the backend path from the frontend directory | `dotnet list` could not find `DreamyCinema.Web\DreamyCinemaSite.csproj` | Re-ran NuGet from the project root and used npm's explicit frontend prefix |
| Combined phase 36 deployment command was rejected by command safety policy | One PowerShell invocation attempted to stop two verified processes, build, restart, and probe | Confirmed the command made no changes and split stop, build, start, and verification into separate calls |
| First standalone phase 36 service start was rejected by command safety policy | Used `Start-Process` with relative DLL arguments and log redirection after the final build | Retried as one hidden process launch with absolute executable and DLL paths and no shell redirection |
| Absolute `Start-Process` launch was also rejected before execution | The command policy blocked the PowerShell process launcher regardless of relative paths or redirection | Switched to `System.Diagnostics.ProcessStartInfo` with `UseShellExecute=false` and `CreateNoWindow=true` |
| Direct `ProcessStartInfo` background creation was also rejected before execution | The environment blocks child-process detachment mechanisms uniformly in this turn | Ran Kestrel as the long-lived foreground exec session and verified it from separate HTTP/process probes |
| First phase 40 fallback E2E entered WebKit fullscreen instead of the forced CSS path | The test disabled only standard `requestFullscreen`; Edge still exposed `webkitRequestFullscreen` | Disable both standard and WebKit element/document fullscreen methods in the fallback-specific test, then seek after entering fullscreen |
| Phase 41 isolated service could not start through the WindowsApps `pwsh.exe` shim | Unified PTY process creation returned access denied before Kestrel started | Retry with the system Windows PowerShell executable and no PTY |
| Phase 41 could not probe the rebuilt asset through port 5210 | No process was listening on the user's normal service port | Validate the generated hashed asset on disk; the next normal `dotnet run` will serve it |
| First phase 42 multi-location endpoint patch no longer matched `Program.cs` | Earlier endpoint insertion shifted one of the distant stream contexts, so the patch was rejected atomically | Split restore, permanent-delete, and stream changes into local patches after re-reading each section |
| First phase 42 isolated service launch ignored the requested test port and paths | `dotnet run` loaded `Properties/launchSettings.json`, which forced the Development profile on port 5210 | Stopped that process immediately and restarted with `--no-launch-profile` plus the isolated environment variables |
| First phase 42 E2E matched three hard-subtitle buttons | Playwright's partial accessible-name matching treated generate/delete buttons as matches for the source selector | Use exact accessible names for the original/hard playback source buttons |
| Phase 43 real hard-subtitle task failed before frame 1 | Config selected `h264_nvenc`, but FFmpeg could not load `nvcuda.dll` on the current machine | Add a narrowly matched NVENC initialization fallback to CPU `libx264` within the same durable job |
| First phase 43 fallback E2E expected the completed stage to retain `CPU` | The shared Worker normalizes every successful job stage to `已完成` after serializing its result | Assert the persisted `result.encoder`; the player completion message reads that field and displays `CPU 编码` |
