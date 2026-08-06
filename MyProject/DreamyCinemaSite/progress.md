# Progress

## 2026-07-31 - Phase 45

- Started the native Android client while preserving the existing uncommitted server/Vue worktree.
- Confirmed the local Android SDK, ADB, API 35/36 platforms, and build-tools 36 are installed. No device is currently attached.
- A .NET Android smoke restore succeeded, but compilation stopped with `XA5300` because no Java SDK is installed; selected Kotlin + Media3 as the product architecture and will provision JDK 17 for the reproducible Gradle build.
- Defined the first vertical slice: configurable LAN server, Cookie/CSRF login, paged/tag-filtered library, authenticated cover loading, and native original/hard-subtitle/WebVTT playback.
- Added the AGP 9.0.1/API 36 Android project, Material dark theme, trusted-LAN network security policy, core API models, and server-address validation.
- Installed Microsoft OpenJDK 17.0.20 through winget after two timed-out curl processes corrupted a resumable local archive; those verified processes were stopped and the abandoned archive remains under ignored `.build` only.
- Added Keystore-backed AES-GCM persistence for authentication Cookies plus the existing `/api/auth/session` CSRF handshake, login/logout, tag, paged video, detail, and authenticated binary-fetch API clients.
- Added the native login screen, paged single-column video library, authenticated cover loader, search/sort, categorized bottom-sheet tag intersection filter, session-expiry routing, and logout.
- Added the Media3/ExoPlayer player with authenticated Range streaming, original/hard-subtitle source selection, selected WebVTT side-loading, readable native caption style, position-preserving source changes, player errors, system-back handling, and landscape/fullscreen presentation.
- Added Gradle Wrapper files, reproducible PowerShell build/install scripts, Android/root README documentation, and roadmap architecture/status updates.
- The first wrapper build stalled on a zero-byte Gradle download. After using the official Gradle 9.1 distribution locally, dependency resolution exposed one final Kotlin inheritance declaration and one invalid Material style; both were fixed.
- `testDebugUnitTest assembleDebug` passes. `lintDebug` initially found the API-27-only navigation-bar style and an unescaped Windows SDK drive separator; both were fixed and the offline rerun passes.
- APK manifest inspection confirms package `com.dreamycinema.android`, version `0.1.0`, minSdk 26, target/compile SDK 36, and an approximately 10.8 MiB Debug APK at `DreamyCinema.Android/app/build/outputs/apk/debug/app-debug.apk`.
- ADB reports no attached device. Real Xiaomi login, cover loading, source switching, seeking, rotation, system back, and fullscreen WebVTT remain the explicit manual acceptance checklist; phase 45 implementation is complete.
- The final `Build-Android.ps1` run passed both build/test and lint stages. Three JVM tests passed with zero failures/errors, `git diff --check` found no whitespace errors in the tracked scope, and `apksigner` verified the APK's expected Android Debug v2 signature.

## 2026-07-31 - Phase 46

- Started Android functional-parity work after real Xiaomi testing exposed status-bar overlap, non-interactive card tags, and the absence of mobile management workflows.
- Screenshot evidence confirms the library content starts behind the status bar and tag taps currently invoke the root card playback action.
- Added explicit safe-drawing Insets to the login, library, management, and player roots so Android 15/16 edge-to-edge defaults no longer place app content beneath the status/navigation bars.
- Replaced the card's combined tag text with independent Material Chips. Tapping a card tag now appends that tag to the selected-tag intersection and reloads the library; tapping the rest of the card still opens playback.
- Added a native management center for durable sync progress/cancel/retry, category/tag create/rename/delete, recycle-bin restore/permanent-delete, missing-record cleanup, and database backup create/import/download/restore/delete.
- Backup import and download stream through `HttpURLConnection` and Android document providers instead of loading the complete ZIP into memory. Existing Cookie/CSRF and unauthorized-session behavior is preserved for every write.
- Added player-side AI Provider status, speech recognition, source-track translation, durable task progress/cancel/retry, hard-subtitle track/style/generate/delete controls, and exact-number confirmation before deleting the original video.
- Added a player action to move the current video into the server recycle bin, completing the Android recycle-bin workflow.
- Final `testDebugUnitTest`, `assembleDebug`, and `lintDebug` all pass. The updated APK is `DreamyCinema.Android/app/build/outputs/apk/debug/app-debug.apk`.
- ADB reports no attached device on this workstation. Phase 46 code is complete; status-bar, tag-touch, management operations, AI/hard-subtitle tasks, and fullscreen captions remain a real Xiaomi installation acceptance pass rather than an automated-device claim.

## 2026-07-09

- Read current implementation and project instructions.
- Established plan for explicit local sync: `Videos` root to `Videos/originals/yyyy/MM`.
- Added EF Core SQLite package and upgraded SQLitePCLRaw bundle to avoid the reported transitive vulnerability warning.
- Reworked backend around a `Video` table, `/api/videos`, `/api/videos/sync`, and ID-based streaming.
- Added web UI sync button and updated README usage.
- Isolated sync test imported one temp mp4, then exposed a SQLite `DateTimeOffset` ORDER BY limitation; changed timestamps to `DateTime`.
- Verified `dotnet build`, `/api/videos/sync`, `/api/videos`, homepage loading, and Range streaming with an isolated temp video root.
- Verified `dotnet list package --vulnerable` reports no vulnerable packages.
- Confirmed real `Videos` mp4 files were not moved during validation.
- Redesigned mobile-first page with fixed filter panel and detailed video cards. Fixed EF query by mapping `Video` to `VideoItem` after materialization.
- Browser visual QA for `http://127.0.0.1:5057` was blocked by enterprise policy, so validation used build/API/static checks.
- Started implementation of selected generated UI: real categorized tags, many-to-many data model, and mobile two-panel video cards.
- Added `TagCategories`, `Tags`, and `VideoTags` schema with in-place SQLite upgrade for the existing database.
- Seeded default categories/tags and backfilled existing videos with unique numbers, descriptions, `年份:2026`, and `状态:未看`.
- Rebuilt the mobile UI around categorized multi-select tags and two-panel video cards with preview/cover areas.
- Verified build, tag category API, tag-filtered video API, homepage structure, vulnerable packages, and HTTP Range playback.
- Added `GET /api/videos/{id}` and `PUT /api/videos/{id}` with unique-number and tag-id validation.
- Added mobile editing drawer for number, title, description, and categorized multi-select tags.
- Verified reversible metadata save/restore, duplicate-number conflict handling, page structure, and inline JavaScript parsing.
- Started the metadata editing phase: video detail/update API plus a mobile editing drawer.
- Diagnosed the always-open editor as an author CSS override of the native `hidden` attribute.
- Added `.sheet-backdrop[hidden] { display: none; }` and verified the running page, inline JavaScript, and clean build.

## 2026-07-13

- Diagnosed same-category multi-tag filtering as category-grouped OR logic in `/api/videos`.
- Changed filtering so each returned video must contain every selected tag ID.
- Initial build verification was blocked because the existing development server locked `DreamyCinemaSite.exe`; proceeding with a controlled server restart.
- Stopped the old server, rebuilt successfully with 0 warnings and 0 errors, and restarted it at `http://127.0.0.1:5057`.
- Verified the live API: `悬疑` returned 1 video, `喜剧` returned 2 videos, and selecting both returned only `abc-123`; every combined result contained both selected tag IDs.
- Started the next phase: real cover upload, local storage, serving, removal, and mobile editor integration.
- Added cover APIs, image format/signature/size validation, local `Videos/covers/yyyy/MM` storage, and stale-file cleanup.
- Added real cover rendering and cover select/preview/remove controls to the mobile editor.
- Verified a reversible cover lifecycle on `abc-123`: upload 200, image read 200 with `image/png`, delete 204, database URL cleared, and cover file count restored.
- Verified a fake `.png` payload is rejected with HTTP 400 based on its file signature.
- Started tag administration: CRUD APIs plus a mobile tag-management drawer.
- Changed default tag seeding to run only on first database creation so user deletions and assignment changes persist across restarts.
- Added category/tag create, rename, and delete APIs with trimmed names, length limits, case-insensitive duplicate checks, and cascade cleanup.
- Added the mobile tag-management drawer and automatic cleanup of deleted selected-tag IDs.
- Verified reversible CRUD: create/rename category and tag, duplicate conflict 409, category delete 204, and cascaded tag lookup 404.
- Verified startup persistence by temporarily renaming default tag `伦理`, restarting the server, confirming the old name was not re-seeded, and restoring the same tag ID.
- Started server-side text search and sort controls for the mobile library.
- Added server-side search across number, title, description, and original filename with escaped SQLite LIKE patterns and a 120-character limit.
- Added imported/date/number/size sorting and combined it with selected-tag intersection filtering.
- Added debounced mobile search, a compact sort selector, stale-request protection, and dynamic fixed-header height measurement.
- Verified case-insensitive number search, literal wildcard handling, combined search/tag filtering, all sort modes, and the long-query 400 response.
- Started safe video deletion with trash/restore, permanent deletion, and missing-record maintenance.
- Added `Trashed` status, filesystem moves under `Videos/trash/{videoId}`, restore conflict protection, and permanent deletion limited to trashed/missing records.
- Added a mobile maintenance drawer for restoring trash items and removing trash or missing records.
- Verified the complete lifecycle in an isolated temporary library; corrected a PowerShell empty-array assertion and confirmed missing detection/deletion from raw JSON.
- Verified available-video permanent deletion is blocked with 409 and trashed streams return 404.
- Removed the isolated test library and restarted the real application at `http://127.0.0.1:5057`.
- Started single-admin authentication with local first-run setup, protected APIs, CSRF, and login throttling.
- Added cookie authentication, PBKDF2 credential storage, loopback-only first setup, fallback authorization, antiforgery middleware, and a login rate-limit policy.
- Added a mobile setup/login page, logout control, centralized CSRF-aware API requests, and session-expiry redirects.
- Auth testing exposed API cookie redirects; added explicit `/api` behavior returning 401/403 instead of redirects.
- Verified in an isolated environment: loopback setup, short-password rejection, no plaintext in credentials, persisted login after restart, protected API access, CSRF rejection/success, logout invalidation, explicit API 401, and login rate limiting.
- Removed all temporary auth data and restarted the real service without creating a test administrator password.
- Fixed a final login redirect loop by moving static file serving before fallback authorization while retaining the explicit unauthenticated index redirect.

## 2026-07-14

- Changed the default HTTP launch profile from loopback-only `localhost:5210` to all local interfaces on port 5210.
- Restarted the real service and confirmed it listens on `0.0.0.0:5210`.
- Verified both `http://127.0.0.1:5210/` and `http://192.168.28.50:5210/` reach the application and redirect to login.
- Added Windows Firewall rule `DreamyCinemaSite TCP 5210`, restricted to the private profile and local subnet.
- Documented local and same-LAN access in `README.md`; phase 15 is complete.
- Started phase 16 after mobile testing showed the inline player rendering above the fixed filter panel while scrolling.
- Replaced the invalid video-inside-button structure with a normal preview container, separate play button, and inline native video controls.
- Added explicit content/header stacking and preview isolation so the active player scrolls with its card below the fixed filter panel.
- Verified inline JavaScript syntax, player structure assertions, and `dotnet build` with 0 warnings and 0 errors; phase 16 is complete.
- Started phase 17 after Xiaomi Browser testing confirmed its native video surface still renders above the fixed filter panel.
- Added a scroll/resize fallback that pauses and hides the active native video as soon as its card reaches the fixed filter panel; tapping it again after scrolling back resumes from the retained position.
- Added active-player cleanup when the video list rerenders.
- Verified JavaScript syntax, fallback structure assertions, and `dotnet build` with 0 warnings and 0 errors; phase 17 is complete pending handset refresh verification.
- Started Vue migration: preserve the ASP.NET Core API and same-origin 5210 deployment while replacing legacy static HTML/JS with a typed Vue application.
- Installed Node.js 24.18 LTS because the previous Node 16 runtime cannot run current Vite.
- Created `DreamyCinema.Web` with Vue 3.5, TypeScript 7, Vite 8, Vue Router, Pinia, typed API models, authentication state, and library state.
- Migrated login, library, categorized intersection filters, search/sort, metadata and cover editing, tag management, and maintenance into Vue views and components.
- Added a dedicated `/videos/:id/play` route so Android native video rendering no longer overlaps the library filter panel.
- Switched the existing NVM installation from Node 16.20.2 to Node 24.18.0 and removed the duplicate winget Node installation.
- Completed the first Vue production build and connected Vue Router fallback plus automatic frontend builds to ASP.NET Core.
- Added Playwright mobile end-to-end coverage using an isolated database and video directory; verified first-run setup, sync, library rendering, and navigation to the dedicated player.
- Captured and inspected mobile library/player screenshots; corrected the `fileName` API type mapping found during visual QA.
- Removed the isolated test data after verification; the real database and video library were not modified by the migration test.
- Verified npm production dependencies and NuGet dependencies report 0 known vulnerabilities.
- Completed the integrated `dotnet build` with automatic Vue type checking/build: 0 warnings and 0 errors.
- Restarted the real service on `0.0.0.0:5210`; root, `/login`, player deep links, and `192.168.28.50:5210` all return the Vue application successfully. Phase 21 is complete.
- Started phase 22 to merge the cover/video panels and support both inline playback and an explicit dedicated-player action.
- Replaced the separate square preview/cover panels with one 16:9 media panel that uses the cover as its poster and plays inline from the center action.
- Added a labeled “播放页” action beside “编辑”; it preserves the dedicated Vue playback route while the center media action stays on the library page.
- Updated Playwright coverage to assert merged media, inline playback without navigation, and explicit navigation to the player route; the mobile test passed.
- Inspected the inline mobile screenshot, removed isolated test data, and completed the integrated build with 0 warnings and 0 errors.
- Restarted the real service on `0.0.0.0:5210` and verified the updated Vue asset is served through `192.168.28.50:5210`; phases 22-23 are complete.
- Started phase 24 after handset testing showed cover actions and the number input overlapping the cover preview in the editor drawer.
- Wrapped cover preview/actions in one explicit editor section, replaced aspect-ratio-only sizing with a concrete responsive height, and made drawer rows max-content tracks.
- Changed editor labels to explicit text elements so mobile layout does not depend on anonymous grid text nodes.
- Added Playwright geometry assertions proving preview, cover actions, and number field occupy non-overlapping vertical ranges; mobile E2E passed and screenshot inspection confirmed the corrected layout.
- Removed isolated test data and verified the updated CSS asset is live at `192.168.28.50:5210`; phase 24 is complete.
- Started phase 25 after desktop mobile emulation showed the editor could not scroll far enough to reach its save actions.
- Split the editor into a constrained flex form, independently scrollable body, and fixed footer with safe-area padding; the save action now remains visible at mobile viewport heights.
- Added mobile E2E assertions for content overflow, effective scrolling, visible save action, and footer viewport bounds; the full Playwright workflow passed.
- Rebuilt the Vue application, visually inspected the mobile editor screenshot, removed isolated test data, and confirmed the updated CSS is served by the real service on port 5210. Phase 25 is complete.
- Started phase 26: add sync-time ffprobe metadata extraction, FFmpeg cover generation, existing-video backfill, and mobile metadata display.
- Installed FFmpeg 8.1.2 and added configurable, timeout-limited ffprobe/FFmpeg process execution with safe argument handling and non-blocking warning results.
- Added duration, resolution, codec, and automatic-cover preference fields with incremental SQLite column creation for the existing database.
- New imports now extract media information and generate midpoint JPEG covers; sync also backfills existing available videos with missing information or eligible missing covers.
- Updated list cards, the dedicated player, sync status, TypeScript contracts, tests, configuration, and README documentation for the new media data.
- Verified a real H.264 1920x1080 sample produced metadata and an automatic cover in an isolated runtime; mobile Playwright E2E passed, and `dotnet build` completed with 0 warnings and 0 errors.
- Removed isolated test data, migrated the real database in place, and restarted the service on `0.0.0.0:5210`; loopback and LAN URLs return 200. Phase 26 is complete.
- Started phase 27 to remove the permanent automatic-cover opt-out and make every coverless video eligible during sync.
- Removed the automatic-cover opt-out from the model and sync query; deleting a cover now leaves it eligible for regeneration, while any existing manual cover remains untouched.
- Added an E2E regression that removes an automatic cover, saves, synchronizes again, and verifies the cover reappears; the mobile workflow passed.
- Rebuilt with 0 warnings and 0 errors, removed isolated test data, and restarted the real service on port 5210. Phase 27 is complete.
- Started phase 28: server-side pagination, database-level tag intersection, mobile infinite loading, stale-request protection, and lazy cover loading.
- Changed `/api/videos` to return paged `items/total/page/pageSize/hasMore` data, capped pages at 50 items, and moved selected-tag intersection into the EF query before count and pagination.
- Added Pinia page state, append/deduplication behavior, stale-request suppression, and a mobile IntersectionObserver sentinel with a manual load-more fallback.
- Updated the header to show loaded/total counts, marked list covers for browser lazy loading, and documented the API and scrolling behavior.
- Verified the pagination contract, mobile count, lazy cover attribute, editing, cover regeneration, inline playback, and dedicated playback route through Playwright; E2E passed and visual inspection confirmed the mobile layout.
- Completed `dotnet build` with 0 warnings and 0 errors, removed isolated test data, and restarted the real service on `0.0.0.0:5210`. Phase 28 is complete.
- Consolidated the final product scope, completed capabilities, required phases 29-35, optional extensions, acceptance criteria, and overall completion definition in `ROADMAP.md`.
- Corrected the roadmap omission: promoted AI subtitles to core scope and added phases for subtitle import/playback, durable jobs, optional speech recognition, provider-neutral AI translation, review, bilingual output, export, backup, and validation.
- Started phase 29: subtitle tracks/cues, external and embedded import, browser WebVTT delivery, and player track selection.
- Added the phase 29 subtitle parser dependency and began the complete storage, sync, API, and Vue player integration.
- The first backend-only build found an incorrect `.Value` access on a null-checked reference record; corrected it before continuing validation.
- Vue type checking and Vite transformation passed; the backend rebuild was temporarily blocked by the live PID 20252 executable, which was verified and stopped for the deployment build.
- Added `SubtitleTracks`/`SubtitleCues`, incremental SQLite setup, external subtitle matching and storage, embedded text-track probing/extraction, and database-generated WebVTT endpoints.
- Added standard HTML subtitle tracks plus an explicit mobile subtitle selector to the Vue inline and dedicated players.
- Integrated build passed with 0 warnings and 0 errors. The isolated mobile Playwright flow passed with SRT import, API track metadata, VTT content/timing, inline track, dedicated track selector, and visual screenshot checks.
- Removed the isolated test runtime, migrated the real database on startup, and restarted the real service on `0.0.0.0:5210`; loopback and LAN URLs both return 200. Phase 29 is complete.

## 2026-07-15

- Started phase 30: durable SQLite media jobs, background sync execution, progress, cancellation, retry, and restart recovery.
- First background-job E2E run imported the video but exposed PascalCase persisted result fields, causing Vue result formatting to fail before refreshing the list; standardized new results on web camelCase and added legacy result compatibility.
- A parallel validation build raced while Vite replaced hashed `wwwroot` assets; frontend validation passed and remaining integrated builds are run serially.
- Added `MediaJobs`, job query/cancel/retry APIs, a single hosted Worker, duplicate-enqueue protection, progress/current-file reporting, cooperative cancellation, and restart recovery.
- Added the mobile task panel with progress, cancel/retry actions, polling, completed result summaries, and page-refresh reconnection.
- Mobile E2E passed cancellation, retry, completed result refresh, persisted page reload, video/subtitle sync, editing, and playback.
- Forced an isolated server process to stop while a 20-file sync job was `Running`; after restart the same job completed with `AttemptCount = 2` and imported the remaining files.
- Final integrated build passed with 0 warnings and 0 errors; the post-concurrency-guard mobile E2E passed again.
- Removed isolated data, migrated the real database, and restarted the real service on `0.0.0.0:5210`; loopback and LAN URLs return 200. Phase 30 is complete.
- Started phase 31: provider-neutral AI settings, speech transcription, stable-Cue translation, chunk checkpoints, usage limits, and player actions.
- Resumed phase 31 from Codex task `019f64c0-b6e7-7de3-b1c6-a0d13e8cf7f7`: implement configurable provider boundaries and an isolated mock end-to-end path before installing real models on the home GPU machine.
- Added the first backend AI subtitle slice: disabled-by-default configuration, provider interfaces, mock providers, durable AI job inputs/chunks, Cue-ID validation, and worker handlers for recognition and translation.
- The first compile reached the output-copy stage but was blocked by the live PID 23632 locking the Debug executable; switched verification to an isolated build output so the real library stays running during development.
- A project-local isolated intermediate path was incorrectly included by SDK source globs and caused duplicate assembly attributes; removed it and moved subsequent isolated outputs outside the repository.
- Added player-page AI controls, sanitized provider status, recognition/translation job polling, cancel/retry actions, and automatic selection of a newly generated subtitle track.
- Restricted the library store to sync jobs so completed AI result JSON is never parsed as a sync result.
- Added `docs/ai-subtitle-architecture.md` and documented disabled-by-default configuration plus the final home-server loopback layout.
- Integrated build passed with 0 warnings and 0 errors. An isolated mobile Playwright run passed sync, existing subtitle playback, mock contextual translation, unchanged VTT timing, generated Chinese playback, and mock speech recognition.
- Visually inspected the mobile player screenshot with the Chinese AI track and task panel; layout is readable and non-overlapping.
- Stopped the old real service only for the final build, migrated the real database at startup, and restarted on `0.0.0.0:5210`. Root and built assets return 200; anonymous AI API access returns 401 as required.
- Final dependency checks report 0 known vulnerabilities for NuGet transitive packages and npm production dependencies.
- Resumed phase 31 on the final home server and re-read the repository instructions, plan, findings, progress, roadmap, architecture, AI provider code, startup code, and configuration.
- Confirmed the DreamyCinema subtree is clean while preserving unrelated uncommitted changes in sibling project `SkipDrama_YuanShen`.
- Audited the home runtime: RTX 3060 Ti 8 GB with driver 596.21, Python 3.11.9, .NET 10.0.201, Node 22.16.0; DreamyCinema listens on 5210 and model ports 8001/8080 are free.
- Checked current official faster-whisper/CTranslate2, llama.cpp, and Qwen documentation before installation. Selected isolated CUDA 12/cuDNN 9 runtime deployment for faster-whisper and the official llama.cpp CUDA 12 Windows build with Qwen3-8B Q4_K_M; no full CUDA Toolkit will be installed unless an actual runtime test proves it necessary.
- Implemented the real `FasterWhisper` Provider, configurable five-minute FFmpeg mono/16 kHz WAV chunking, per-audio-chunk checkpoints, global timestamp offsets, cancellation propagation, retry, and temporary-file cleanup.
- Implemented the real `LlamaCpp` Provider with loopback-only URL validation, configurable timeout/model/limits, rolling source and translation context, names/glossary profile snapshots, `/no_think`, schema-constrained Cue-ID JSON, strict validation, automatic retry, and cumulative usage limits.
- Added repeatable setup/start/stop/home-server PowerShell scripts, a loopback FastAPI faster-whisper service with lazy GPU loading and cancellation/disconnect checks, a safe local configuration template, and ignored `appsettings.Local.json` loading with environment/command-line overrides preserved.
- Installed FFmpeg 8.1.2, 7-Zip 26.02, faster-whisper 1.2.1, CTranslate2 4.8.1, CUDA 12.8/cuDNN 9.8 runtime DLLs, official llama.cpp `b10015`, official large-v3, and official Qwen3-8B Q4_K_M outside the repository.
- Fixed CUDA archive extraction by switching from Windows bsdtar to 7-Zip, fixed partial-download/release-publication races with size-checked resume and complete-release selection, and fixed Python DLL discovery with `os.add_dll_directory`.
- Verified ports 8001/8080 bind only loopback, llama sleeps and unloads at idle, Qwen fits at 8192 context, structured translation preserves Cue IDs and explicit language, and real large-v3 `int8_float16` returns segment timestamps then releases GPU memory.
- Added a repeatable isolated real-provider integration script covering a forced application restart during translation, exact Cue/timeline checks, explicit-language preservation, in-flight speech cancellation, retry, and chunk recovery.
- Corrected llama.cpp `b10015` schema compatibility after the first real application request produced HTTP 400: removed the duplicate top-level grammar and request-level reasoning format while keeping schema-constrained `response_format` and non-thinking controls.
- The real integration then passed: translation resumed on attempt 2 with 12/12 cues and identical timings; speech recognition cancelled while running and resumed on attempt 2 with 12 cues. The isolated database and Videos directory were removed after success.
- Added `SubtitleTracks.RevisionStage` with incremental SQLite migration and API/TypeScript exposure. Imported source, raw recognition, Chinese draft, future source correction, and future final polish now have explicit non-overwriting data boundaries.
- Verified invalid model-output retry deterministically: missing and duplicate Cue responses were rejected and checkpointed before a complete third response was accepted.
- Re-ran the real-provider integration with revision-stage assertions; `SourceOriginal`, `RawRecognition`, and `ChineseDraft` were persisted correctly while restart/cancel/resume, timing equality, and explicit-language checks still passed.
- Updated README, roadmap, and architecture documentation with the installed versions, repeatable scripts, loopback/serial GPU layout, prompt/validation contract, revision boundaries, and current real-video acceptance dependency.
- Final integrated `dotnet build` (including Vue type checking/Vite) passed with 0 warnings and 0 errors; final Mock mobile Playwright passed 1/1 after the revision-stage migration.
- NuGet transitive packages, npm production dependencies, and the full npm dependency tree report 0 known vulnerabilities; the faster-whisper virtual environment reports no broken requirements.
- Started the final home deployment through `Start-HomeServer.ps1`: DreamyCinema listens on `0.0.0.0:5210`, faster-whisper/llama remain on loopback, first-run admin setup is required, and anonymous AI status is protected with HTTP 401. The new production library is empty, so no real media was modified.
- Parameterized the real integration script for long-video benchmarking and ran a 600-second isolated sample with production 300-second audio chunks. It passed with 132 recognized cues, 172.2-second recognition retry, 8.5-second translation/restart path, 6893 MiB peak GPU memory, and about 15.1 GiB peak system memory usage.
- Located real local MP4 candidates read-only but did not import personal game recordings without a user selection. Final phone-based human acceptance remains the only open Phase 31 item.
- Inspected the exported production `SubtitleCues.sql` and live database metadata after the user's first real translation. Confirmed a severe semantic hallucination despite correct Japanese metadata, Cue count, IDs, and timing; reproduced good output with the same inputs and isolated the missing semantic-quality/retranslation safeguards as the remaining Phase 31 defect.
- Confirmed the second website run was a real new model call, not cache reuse: it created a new 47-cue track but still hallucinated, with 4126/4133-token prompt counts. Traced this to Japanese being embedded in the prompt as literal `\uXXXX` sequences.
- Fixed CJK prompt serialization, reduced chunks to 12 cues, set temperature 0 and output cap 1536, added per-batch semantic review, versioned generated-track identity, and added a force-retranslation UI/API path that preserves old drafts.
- Re-ran the real integration and the user's full 47-cue Japanese source through the corrected C# Provider. All four batches passed semantic review with coherent weather translations and no cross-batch hallucination.
- Extended mobile E2E to create a second draft through “重新翻译为中文”; Playwright passed 1/1. Deployed the corrected build with DreamyCinema on `0.0.0.0:5210` and both model services healthy on loopback ports 8001/8080.

## 2026-07-16

- User completed real-video recognition, corrected translation, and phone playback acceptance without identifying a remaining blocking issue; phase 31 is complete.
- Started phase 32: subtitle comparison/review, manual editing, partial retranslation, bilingual output, quality warnings, and export.
- Added the phase 32 backend foundation: paired review documents, immutable draft-to-final review cloning, manual-edit markers, deterministic reading warnings, protected selected-Cue retranslation, bilingual generation, and UTF-8 SRT/WebVTT export.
- Added the dedicated mobile subtitle review route with sticky video, source/Chinese track selection, Cue seeking, text search, warning/manual filters, source correction, Chinese editing, selected-Cue retranslation, bilingual generation, and export actions.
- Added device-local subtitle preference so the dedicated player selects the most recent reviewed track of the remembered language after navigation or refresh.
- Extended mobile E2E to prove source and AI draft immutability, source correction, Chinese manual-edit protection, selected-Cue retranslation, deterministic warnings, bilingual SRT content, WebVTT export, search, playback return, and subtitle preference persistence; Playwright passed 1/1.
- Inspected the mobile review screenshot: controls remain scannable, Cue editors fit the viewport, warnings and manual state are clear, and no overlap or clipping is visible. Phase 32 is complete.
- Final integrated build passed with 0 warnings and 0 errors; NuGet transitive packages plus npm production/full dependency trees report 0 known vulnerabilities, and the DreamyCinema-scoped `git diff --check` passed.
## 2026-07-24

- Started phase 33 consistent backup packages and backup management.
- Preserving the existing phase 32 working tree; no prior subtitle-review changes will be reverted.
- Initial scope: SQLite online snapshot, covers and subtitle/configuration assets, versioned manifest/checksums, authenticated create/list/download/delete workflow, and explicit exclusion of original videos and secrets.
- Added the first backup backend and mobile management UI; backend and Vue production builds passed.
- The first isolated live-server test reached backup creation but exposed a pooled SQLite snapshot handle that blocked ZIP packaging; backup-specific SQLite connections were changed to `Pooling=false` before repeating validation.
- The repeated isolated test created/listed/downloaded a valid package with one video record, one cover, one subtitle file, no warnings, and matching per-file hashes.
- External ZIP inspection found SQLite `-wal`/`-shm` sidecars produced by the copied journal mode; snapshot creation now switches to `journal_mode=DELETE` and removes sidecars so restore receives one self-contained database file.
- Final external package inspection verified four hashed payload files plus `manifest.json`: one database, portable settings, one cover, and one subtitle. No original/trash video, credential, local config, provider endpoint/key, or SQLite sidecar was present.
- Extended the existing Pixel 7 Playwright flow with backup scope disclosure, creation, counts, archive download, ZIP signature, checksum shape, and deletion; the full E2E passed 1/1 and visual inspection found no overlap or clipping.
- API security/lifecycle checks passed: anonymous list `401`, authenticated write without CSRF `400`, video byte-range playback `206`, both isolated packages deleted, empty final list, and deleted download `404`.
- Updated README and roadmap with package layout, configuration, exclusions, APIs, and the completed phase 33 behavior.
- Final integrated `dotnet build` passed with 0 warnings and 0 errors; NuGet transitive, npm production, and full npm dependency audits report 0 known vulnerabilities; DreamyCinema-scoped `git diff --check` passed.
- Started the updated real service on `0.0.0.0:5210` (PID 19224). Root and the new hashed Vue asset return 200, while anonymous backup API access returns 401. Phase 33 is complete.
- Started phase 34 validated restore and disaster protection while preserving all uncommitted phase 32/33 work.
- Restore design selected reverse SQLite online backup, an atomic write/job maintenance coordinator, server backup selection plus validated ZIP import, pre-restore snapshots, exact cover/subtitle directory swaps, portable settings, post-restore missing-file refresh, and rollback of every changed layer.
- Implemented strict restore-package validation, bounded ZIP upload/import, schema/count/path/integrity checks, server-side restore, pre-restore snapshots, reverse SQLite backup, cover/subtitle swaps, portable settings, missing-original normalization, and full changed-layer rollback.
- Added an application-wide restore coordinator: authenticated API writes and background media jobs hold writer leases, while restore starts only with no active writer and blocks new writes until completion.
- Added mobile ZIP import and restore controls with scope disclosure, destructive confirmation, restore-purpose badges, result refresh, and retained recovery snapshot visibility.
- Backend plus Vue production compilation passed after the restore/API/UI integration; isolated success and injected-failure runtime verification remains in progress.
- Extended the Pixel 7 E2E into a complete disaster-recovery round trip. The first restore failed after database application through the one-shot test hook and retained the mutated title, absent cover, and added subtitle tracks after rollback; the second restore recovered the original title, cover, one-track subtitle state, and a working `206` original-video stream.
- The same E2E deleted the original server package, re-imported its downloaded ZIP through the mobile file control, rejected a corrupt ZIP, and verified two retained pre-restore snapshots plus an imported package. Visual inspection found no overlap or clipping.
- Post-test disk inspection found no `.partial`, `.restore-*`, or asset-swap leftovers. The isolated runtime retained one original video, one restored cover, one restored subtitle file, two pre-restore snapshots, and one imported package; the imported ZIP contained no originals or secrets.
- Final `dotnet build` passed with 0 warnings and 0 errors, `npm ls --all` completed, and DreamyCinema-scoped `git diff --check` passed. Fresh NuGet/npm vulnerability queries could not reach their official registries because TLS connections were closed before handshake completion; neither command returned vulnerability data.
- Deployed the final phase 34 build to the real service on `0.0.0.0:5210` (PID 26544). Loopback, LAN, and the current hashed Vue asset return 200; anonymous backup access returns 401. Phase 34 is complete.
- Started phase 35 bulk organization and media reprocessing while preserving all uncommitted phase 32-34 work.
- Phase 35 tooling note: the first source-location command used Bash-style quote escaping that PowerShell rejected; the corrected command then found the bundled WindowsApps `rg.exe` was denied execution. Switched source discovery to PowerShell `Select-String` instead of repeating either failure.
- Implemented bounded batch tag add/remove, all-items-preflight batch trash with reverse file rollback, and server-derived resolution suggestions/confirmed resolution-tag replacement.
- Added the durable `MediaMaintenance` job type for metadata reanalysis and automatic-cover regeneration. Jobs reuse existing persistence, progress, cancellation, retry, restart recovery, and restore writer coordination; generated covers are swapped only after a successful replacement.
- Extended ffprobe/ffmpeg execution with linked external cancellation so cancelling a maintenance job terminates the active child process. The isolated backend build passed with 0 warnings and 0 errors.
- Added explicit codec compatibility warnings to the API, video cards, and player details. H.264 stays quiet; HEVC, AV1, unknown, and other codecs receive targeted mobile-browser guidance.
- Added mobile selection mode, select-all-loaded/clear/finish controls, a fixed bottom action bar, and a scrollable bulk organizer for tag changes, resolution confirmation, media jobs, and reversible batch trash.
- Updated the Pinia library job flow to resume and present both sync and media-maintenance jobs while leaving per-video AI subtitle jobs on their existing screens. The complete .NET + Vue build passed with 0 warnings and 0 errors.
- The first phase 35 E2E run reached resolution confirmation, then read the API before the Vue async click handler completed. A separate authenticated browser query confirmed `1080P` and `720P` were both persisted; the test now waits for the drawer-close completion signal before reading.
- The second isolated E2E run passed tag changes, resolution confirmation, injected reanalysis failure, and retry. Its cover assertion initially observed the previous completed job before the new async request finished; the cover path now uses the same drawer-close completion signal.
- The final phase 35 isolated Pixel 7 E2E passed end to end: three generated videos, explicit codec warning, two-video tag add, 1080P/720P suggestion and confirmation, injected reanalysis failure plus retry (`AttemptCount = 2`), two-cover regeneration, and two-file reversible trash movement.
- Pixel 7 viewport screenshot inspection confirmed the independent drawer scroll, controls, tag groups, warnings, and two-column media actions are readable with no overlap or horizontal clipping.
- Re-ran the existing full mobile regression in a separate fresh runtime with mock AI and injected restore rollback. Sync cancellation/retry, editing, playback, subtitles, translation/review/export, backup, failure rollback, successful restore, and import all passed 1/1.
- Final integrated .NET/Vue build passed with 0 warnings and 0 errors; scoped `git diff --check` passed (line-ending notices only), `npm ls --all` completed, NuGet transitive vulnerability audit reported 0, and the official npm registry production audit reported 0. The configured npmmirror audit endpoint returned 404 before the official-registry retry.
- Deployed the final phase 35 build to the real service on `0.0.0.0:5210` (PID 23760). Loopback, LAN `192.168.28.50`, and the new `index-DBe4ZdUt.js` asset return 200; anonymous `/api/videos` returns 401. Phase 35 is complete.
- Started phase 36 account and security maintenance while preserving all uncommitted phase 32-35 work.
- Audited the current authentication boundary: PBKDF2 credentials are version 1 without a security stamp, Cookie lifetime is fixed at 12 hours, all API writes already pass through CSRF validation, and fallback authorization protects every non-anonymous endpoint.
- Selected a security design with credential-stamped authentication tickets, an authenticated password-change endpoint that signs out the caller and rejects every old ticket, explicit Vue re-login reasons, and a bounded JSONL management audit kept outside backup/restore payloads.
- Added the phase 36 backend foundation: version-2 credential migration, random security stamps in authentication tickets, per-request principal validation, authenticated password change with current-password verification, session expiry metadata, and a protected audit-read endpoint.
- Added write-API audit middleware and a 2 MiB rotating JSONL store. Entries contain no request body, password, CSRF token, Cookie, or provider secret and remain outside restorable backup payloads.
- Added the mobile account/security drawer with session and password timestamps, explicit trusted-LAN boundary text, password change validation, and the latest 30 sanitized management events.
- Updated the API client and login page to distinguish expired/invalidated sessions from the mandatory re-login after a password change.
- Phase 36 integrated .NET, Vue type-check, and Vite build passed with 0 warnings and 0 errors. The current frontend assets are `index-4rrG6lfz.js` and `index-BGSB6860.css`.
- The final isolated Pixel 7 security E2E passed 1/1. It proved anonymous library reads return 401, missing-CSRF password changes return 400, incorrect current passwords are rejected, a successful rotation invalidates both active browser sessions, the old password fails, the new password succeeds, and stale pages receive the explicit session-expired login reason.
- The same E2E queried the latest 100 audit entries and confirmed neither test password, the CSRF header name, nor the authentication Cookie name appears in the response. Visual inspection found the account/security drawer readable and non-overlapping with its footer fixed inside the mobile viewport.
- Re-ran the complete existing mobile-library regression in a fresh Testing environment with one generated H.264/AAC video, an external SRT, Mock AI, and injected first-restore failure. Sync, subtitles, review/export, backup rollback, successful restore, import, and `206` playback all passed 1/1.
- Updated README and the roadmap with credential-stamp invalidation, audit scope/rotation, API endpoints, excluded backup content, and the completed phase 36 acceptance evidence.
- Final dependency checks found 0 vulnerable NuGet packages and 0 npm vulnerabilities in both production and full dependency trees. `npm ls --all` completed with only platform/provider optional dependencies absent, and scoped `git diff --check` passed with line-ending notices only.
- Stopped only the verified isolated test process and previous real PID 23760, then completed the final integrated .NET/Vue build with 0 warnings and 0 errors.
- Deployed phase 36 on `0.0.0.0:5210` as PID 33440. Loopback, physical LAN `192.168.28.50`, and `index-4rrG6lfz.js` return 200; anonymous `/api/videos` returns 401. The real credential migrated from version 1 to version 2 with a security stamp, so existing browser Cookies require one re-login with the unchanged password. Phase 36 is complete.

## 2026-07-28

- Started phase 39 mobile-browser-compatible subtitle rendering while preserving the existing uncommitted phase 32-36 working tree.
- Confirmed the dedicated player already selects native text tracks programmatically, but inline playback only relies on the browser's default-track rendering.
- Selected a three-mode design: browser-native captions, Vue-rendered captions over the picture, and a below-video compatibility bar that cannot be covered by an inline native video surface.
- Added shared device-local subtitle preferences and `useSubtitleDisplay`, which keeps the selected WebVTT track hidden for self-rendering and reads its active Cues without parsing subtitle text manually.
- Added overlay and below-video caption output to both `VideoCard` inline playback and `PlayerView`; touch devices default to the below-video mode, while desktop defaults to the in-picture overlay.
- Added an explicit dedicated-player display selector for browser-native, in-picture, and below-video modes. The selected mode is reused by later inline playback on the same device.
- Added focused Pixel 7 Playwright coverage that imports real SRT/WebVTT data, seeks into an active Cue, checks the inline and dedicated compatibility bars, switches to the self-rendered overlay, verifies native track `showing`, and switches back.
- The focused subtitle E2E passed 1/1 twice. The final Vue type-check/Vite build passed, and the isolated .NET build passed with 0 warnings and 0 errors.
- Inspected the mobile screenshot: the native controls, compatibility subtitle bar, two display selectors, and following AI panel remain separate and readable without clipping.
- Verified the running service on `127.0.0.1:5210` serves the new `index-Bo-SQXTH.js` asset with HTTP 200. The isolated port 5254 test service was stopped after validation. Phase 39 is complete; Xiaomi handset compositing is the remaining manual acceptance check.
- Started phase 40 after Xiaomi handset validation confirmed native `<video>` fullscreen excludes the compatible DOM subtitle bar.
- Preserving the phase 32-39 working tree and limiting this change to the dedicated playback page, shared fullscreen styling, and focused subtitle E2E.
- Added standards and WebKit-prefixed container fullscreen handling to `PlayerView`, including system-back/fullscreen-change state recovery and an in-stage exit action.
- Added a CSS viewport fallback that fixes the complete player stage over the browser viewport, locks background scrolling, gives the video the remaining height, and retains the compatibility subtitle bar.
- Added `controlslist="nofullscreen"` as a best-effort vendor hint and an explicit “带字幕全屏” action. Entering from browser-native subtitle mode switches to the below-video compatibility mode.
- Vue type checking and the Vite production build pass after the fullscreen implementation.
- The first fallback-specific E2E unexpectedly exercised Edge's WebKit fullscreen path because only the standard method was disabled. Its failure screenshot still confirmed the complete player stage and exit action entered real fullscreen; the test now disables both API variants and seeks after entry so the active Cue remains visible.
- Visual QA of the forced CSS fallback found the over-video exit button was hidden by the native video surface. Moved exit into the compatibility subtitle bar and changed fullscreen entry to temporarily force below-video captions, restoring the previous mode after exit.
- The corrected Pixel 7 fallback E2E passes 1/1. It verifies viewport coverage, active subtitle text, caption-bar exit, scroll-lock cleanup, restoration of the prior native display mode, and browser `TextTrack.mode = showing`.
- Inspected `mobile-subtitle-fullscreen.png`: the native video controls, external subtitle text, and caption-bar exit control remain separate and visible with no overlap. Xiaomi Browser remains the required real-device acceptance check.

## 2026-07-30

- Started phase 41 rollback after real Xiaomi Browser testing showed the phase 40 container/CSS fullscreen direction still loses DOM captions in fullscreen.
- The rollback is limited to phase 40. Phase 39 browser-native, in-picture self-rendered, and below-video compatibility subtitle modes remain intact.
- Removed phase 40 Fullscreen API/WebKit/CSS fallback state, controls, styles, documentation, and focused assertions. A source scan confirms none of the phase 40 runtime identifiers remain.
- The rollback Vue type-check/Vite build passed. The restored phase 39 Pixel 7 subtitle regression passed 1/1 against the isolated library on port 5254.
- The final isolated .NET/Vue build passed with 0 warnings and 0 errors. The generated `index-CeOEIsLO.js` contains the normal subtitle display control and no phase 40 fullscreen action; scoped diff checking reports only existing line-ending notices.
- Stopped the verified isolated port 5254 process. Port 5210 was not running during final probing, so the rebuilt disk asset will be served on the next normal application start.
- Evaluated the replacement direction: retain originals and generate versioned, offline H.264 hard-subtitle derivatives as durable background jobs after subtitle review. Soft subtitle muxing is not considered a reliable Xiaomi Browser fix.

## 2026-07-30 - Phase 42

- Started selective hard-subtitle derivative generation. The requested workflow is per-video generation, original/derived playback selection, and optional original deletion after a playable derivative exists.
- Preserving the uncommitted phase 32-41 worktree and treating original deletion as a guarded destructive operation rather than part of generation.
- Added persisted original/derived availability metadata, the durable `HardSubtitle` media-job type, H.264 FFmpeg burning with progress/cancellation/validation, and guarded replacement of an older derived version only after the new file succeeds.
- Added source-aware range streaming, original/hard-subtitle playback selection, per-track generation, retry/cancel/delete controls, stale-subtitle detection, and exact-number confirmation before permanent original removal.
- Original deletion now moves the file into a same-volume staging directory before saving the database and restores it if that save fails. Backup restore clears derived pointers whose files are absent and recalculates availability from both playable sources.
- The first integrated .NET/Vue build after backend and player integration passed with 0 warnings and 0 errors. Isolated FFmpeg runtime and destructive-flow verification remain in progress.
- The final fresh Pixel 7 E2E passed 1/1 with a generated six-second H.264/AAC fixture and Chinese SRT. It verified original/hard source switching, derivative deletion and recreation, real-CSRF wrong-number rejection, exact-number original deletion, default/derived `206` Range playback, and explicit original `404`.
- Extracting the one-second output frame confirmed `硬字幕合并测试` is rendered into video pixels. Final isolated storage contains 0 originals, 1 hard-subtitle MP4, and 0 `.work` files.
- Updated README and the roadmap with configuration, destructive-operation rules, API routes, storage paths, stale-version handling, and the fact that metadata backups exclude `Videos/derived`.
- Final integrated .NET/Vue build passed with 0 warnings and 0 errors. DreamyCinema-scoped `git diff --check` found no whitespace errors, NuGet and npm production audits reported 0 known vulnerabilities, and `npm ls --all` completed with only expected platform/tool-provider optional dependencies absent. Phase 42 is complete.

## 2026-07-30 - Phase 43

- Started the NVENC compatibility repair after a real task failed with `Cannot load nvcuda.dll` on the current computer. The intended behavior is NVENC-first on the home server and same-job CPU fallback elsewhere.
- Added narrow CUDA/NVENC initialization-error detection, same-job `libx264` retry with an independent `CpuPreset`, and persisted completion/result text identifying the actual encoder.
- Added a Testing-only one-shot NVENC-unavailable hook and focused E2E assertions for CPU completion. Isolated backend compilation passed with 0 warnings and 0 errors; Vue type checking passed.
- The first fallback E2E completed the generated video but its extra stage assertion observed the Worker's standard terminal label `已完成`; the durable result remains the authoritative actual-encoder field, so the regression now asserts `result.encoder`.
- The corrected fresh Pixel 7 E2E passed 1/1 after injecting the exact NVENC-unavailable class of error. It confirmed `result.encoder=libx264`, retained all derivative/original safety behavior, produced a validated 640x360 H.264 file, removed the test original, and left 0 work files.
- Final integrated .NET/Vue build passed with 0 warnings and 0 errors; scoped diff checking found no whitespace errors beyond existing LF/CRLF notices.
- Stopped the verified old port-5210 process, built the updated application, and deployed it on `0.0.0.0:5210` as PID 31452. The root page and `index-D_v_cPG1.js` both return 200. Phase 43 is complete.

## 2026-07-30 - Phase 44

- Started per-generation hard-subtitle style controls while preserving all existing uncommitted phase 32-43 work.
- Selected a compact mobile settings drawer with real-cover preview. The editable contract covers font, size, text opacity/color, outline color/width, and bottom margin; the new default font size is 18 with 92% text opacity.
- Added a validated `HardSubtitleStyle` snapshot to the request, durable job input, result, generated-derivative database record, and API response. FFmpeg now converts CSS colors and opacity to libass `&HAABBGGRR` values.
- Added the mobile `HardSubtitleSettings` drawer with a real-cover preview, fixed footer, independent scrolling, reset-to-default action, and per-generation controls. Vue type checking and the first Vite build pass.
- The fresh Pixel 7 E2E passed 1/1. It verified the new 18 px / 92% defaults, changed every style dimension, generated and regenerated a real H.264 derivative, and confirmed identical normalized style values in both the completed job and video record.
- Extracted a generated frame and visually confirmed the custom small semitransparent text plus outline. ffprobe reports H.264 640x360, and the derivative work directory contains 0 residual files.
- Updated configuration examples, README, API notes, storage documentation, and the roadmap. Final integrated .NET/Vue build passed with 0 warnings and 0 errors; scoped diff checking found no whitespace errors beyond existing line-ending notices.
- Deployed phase 44 on `0.0.0.0:5210` as PID 33676. Loopback, physical LAN `192.168.28.50`, and `index-D3gl_6FQ.js` return 200; anonymous style-options access returns 401. Phase 44 is complete.
