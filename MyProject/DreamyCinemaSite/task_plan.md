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
31. [in_progress] Add optional speech recognition plus provider-neutral AI subtitle translation with chunk validation, glossary, usage limits, and resume.
32. [pending] Add subtitle review/editing, partial retranslation, bilingual tracks, quality warnings, and export.
33. [pending] Add consistent metadata/cover/subtitle backup packages and backup management.
34. [pending] Add validated restore with pre-restore snapshots and rollback.
35. [pending] Add mobile batch organization, media re-analysis, cover regeneration, and codec compatibility warnings.
36. [pending] Add administrator password rotation, session invalidation, and management audit logs.
37. [pending] Add Windows production publishing, automatic startup, health checks, and structured logs.
38. [pending] Complete integration/E2E/performance/security validation and release documentation.

## Decisions

### Phase 31 execution plan (home server)

1. [complete] Audit the Windows/NVIDIA/Python/.NET/Node/FFmpeg environment and preserve the existing working tree.
2. [complete] Implement configurable FFmpeg audio chunking plus the real loopback faster-whisper HTTP provider with cancellation and per-chunk resume.
3. [complete] Implement the real loopback llama.cpp OpenAI-compatible provider with structured Cue-ID output, context, glossary/character names, validation, and retry.
4. [complete] Add non-committed home-server configuration and repeatable PowerShell setup/start scripts for services bound only to `127.0.0.1`.
5. [complete] Install and verify the pinned faster-whisper/CTranslate2 runtime and official llama.cpp CUDA Windows release with Qwen3-8B Q4_K_M.
6. [complete] Validate providers and restart/cancel/resume behavior against isolated data, then run the full build, type check, Playwright, and dependency audits.
7. [complete] Benchmark a ten-minute isolated video with production 300-second speech chunks and record recognition speed, GPU memory, and system memory.
8. [pending] Perform final manual subtitle acceptance with a real household video after one is added to the currently empty production library.

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
