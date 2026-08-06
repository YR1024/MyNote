# Findings

- Existing MVP scans `Videos` top-level mp4 files on every `/api/videos` request.
- The local `Videos` directory already contains several top-level mp4 files, so validation must avoid calling the real sync endpoint against the real folder unless the user wants those files moved now.
- Project-level `AGENTS.md` requests `task_plan.md`, `findings.md`, and `progress.md` for multi-step work.
- Current DB already contains imported videos under `Videos/originals/2026/07`; schema changes must migrate in place rather than recreate the SQLite database.
- User confirmed the project is still in development; future architecture changes do not need compatibility with old local database data.
- `.sheet-backdrop { display: flex; }` overrode the browser's `[hidden] { display: none; }`, forcing the edit drawer visible before any video was selected.
- `/api/videos` grouped selected tags by category and used `Any` inside each group, so selecting both `悬疑` and `喜剧` matched videos containing either tag instead of requiring both.
- The data model and `VideoItem` already expose `CoverRelativePath`/`coverUrl`, but no cover-serving endpoint or editing workflow exists; the current cover panel is only a text placeholder.
- EF relationships already cascade `TagCategory -> Tag -> VideoTag`; category/tag deletion removes assignments but does not delete videos.
- Previous startup initialization re-added missing seeded tags and default video tags, which would undo user deletions; seeding must only run when the database is first created.
- `RefreshMissingStatusAsync` currently recalculates every video from its original path; it must skip trashed records or the next sync would overwrite `Trashed` with `Missing`.
- Current APIs and static index have no authentication; all reads and writes are reachable by any client that can access the server.
- The cover upload endpoint explicitly disables antiforgery and must be brought under the same CSRF policy as JSON writes.
- Default `dotnet run` bound only `localhost:5210`; the confirmed Ethernet address is `192.168.28.50/24`, so LAN requests were refused before reaching the app.
- The active Ethernet connection is a Windows `Private` network. A scoped inbound rule now allows TCP 5210 only for the private profile and `LocalSubnet` clients.
- After rebinding, Kestrel listens on `0.0.0.0:5210`; both loopback and `192.168.28.50:5210` return the expected login redirect locally.
- The mobile player is currently a `<video controls>` element nested inside a `<button>`, which is invalid interactive-content nesting and can produce native video-layer stacking behavior that ignores the fixed filter panel.
- The player now stays in a normal isolated preview container, while a separate overlay button starts playback and is removed from interaction once native controls are active.
- Xiaomi Browser still composites the active video surface above the fixed HTML filter panel after the DOM nesting fix, so CSS `z-index` is not a reliable solution on this handset.
- Chromium documents that Android video rendering can use `SurfaceView`, `MediaCodec`, and platform overlays; this supports treating the handset behavior as a native compositor issue rather than ordinary CSS stacking.
- Hiding the native `<video>` element when it crosses the fixed header boundary is more reliable than trying higher `z-index` values against a platform video overlay.
- The existing machine used NVM with Node 16.20.2; current Vite requires Node 20.19+ or 22.12+, so NVM now selects Node 24.18.0 from the official Node.js mirror.
- The Vue production build can remain same-origin: source is separated under `DreamyCinema.Web`, while ASP.NET Core serves Vite output from `wwwroot` on port 5210 and preserves cookie/CSRF behavior.
- A dedicated playback route removes native `<video>` elements from the sticky-filter library view, avoiding the Xiaomi Browser overlay conflict by page structure rather than scroll-triggered pausing.
- Visual QA caught that the backend `VideoItem` serializes the original filename as `fileName`; the initial Vue type used `originalFileName`, leaving the player detail row blank.
- The merged card can mount one native `<video>` in place without changing routes; normal scrolling does not pause it. Starting another video, changing the result set, or opening the editor disposes the previous inline player.
- Xiaomi Browser renders the cover preview at its aspect-ratio size but appears not to reserve the same CSS Grid row height inside the scrolling drawer, allowing later controls to overlap the preview.
- Grouping the preview and cover actions into one grid item, using `grid-auto-rows: max-content`, and assigning a concrete responsive preview height prevents later form controls from sharing the cover's visual area.
- The editor form is itself the overflow element but remains a flex child with the default `min-height: auto`; its full content height is clipped by `.drawer` instead of shrinking into a scroll container.
- A dedicated flex form shell with `min-height: 0` lets the editor body become the only scroll container; keeping the footer outside that body makes save/destructive actions reachable without depending on page scroll behavior.
- FFmpeg and ffprobe were not initially available on PATH; `winget` exposes the current `Gyan.FFmpeg` package for installing both tools.
- Existing videos need a sync-time backfill path, otherwise automatic media information would apply only to newly dropped files.
- FFmpeg 8.1.2 was installed through WinGet; the application resolves its stable WinGet command links even when the parent process has not refreshed PATH.
- The isolated sample was identified as H.264, 1920x1080, 41.211 seconds, and produced a valid midpoint JPEG cover.
- The user's sync model treats cover absence as an incomplete managed state, so explicit cover removal should not permanently opt the video out of future automatic generation.
- The current video endpoint materializes every matching row before applying selected-tag intersection filtering, so pagination requires moving each selected-tag predicate into the EF query before counting and slicing.
- Applying one correlated `Any` predicate per selected tag preserves intersection semantics in SQLite while allowing `COUNT`, `Skip`, and `Take` to operate before materialization.
- An IntersectionObserver sentinel with a manual button fallback supports automatic mobile loading without making data retrieval depend solely on observer availability.
- The remaining work is primarily data safety, long-running sync reliability, bulk administration, credential maintenance, Windows production operation, and release validation rather than basic library browsing.
- The phase 35 credential file stores only a PBKDF2 hash and has no version or security-stamp value that an authentication ticket can be checked against, so changing a password cannot currently invalidate already issued Cookies.
- Authentication tickets expire after 12 hours without sliding renewal, but the session API does not expose that timestamp and the Vue API client redirects a 401 to `/login` without preserving an expiration reason.
- Security audit events should remain outside the restorable media-library database: a bounded JSONL file can survive library restores, stay excluded from backup packages, and record only action, route, status, time, and remote address without request bodies or authentication secrets.
- The first Pixel 7 security-drawer screenshot shows a stable full-height scrolling body, a fixed reachable footer, readable session/security facts, and audit rows without overlap or horizontal clipping.
- The real version-1 credential migrated atomically to version 2 on startup with a security stamp while retaining the existing password hash; this intentionally invalidates pre-deployment Cookies once.
- Backups should protect metadata and covers by default without duplicating the large original video directory; video-file availability is re-evaluated after restore.
- No subtitle model, parser, storage, playback track, speech recognition, or translation code currently exists; AI subtitles are a new core subsystem rather than a small addition to the player.
- AI translation must preserve stable cue identity and timing independently from translated text; otherwise chunked retries or model output drift can corrupt subtitle synchronization.
- `SubtitlesParserV2` 2.4.0 targets .NET 10 and provides structured parsers for SRT, WebVTT, ASS, and SSA, while FFmpeg can remain responsible for extracting embedded text subtitle streams.
- Browser subtitle behavior varies across mobile vendors, so the dedicated player exposes its own subtitle selector and also mounts standard HTML `<track>` elements for native controls.
- The isolated phase 29 fixture imported two English SRT cues, persisted one default track, and generated WebVTT with the original 500-2500 ms timing intact.
- Persisted job result JSON must use web camelCase independently of ASP.NET response serialization because a stored `JsonElement` preserves its original property names.
- A running sync job interrupted by forcibly stopping the isolated server was recovered from SQLite on restart and completed on attempt 2, proving recovery is not dependent on browser memory.
- OpenAI segment timestamps require `verbose_json` with `timestamp_granularities[]=segment`; `whisper-1` is the compatibility default because the official guide limits timestamp granularities to that model.
- Translation output will use JSON schema when supported and still requires application-side exact Cue ID validation; provider output must never be allowed to alter subtitle timing.
- The company development machine should not host the final models. Phase 31 must default AI processing to disabled, expose only sanitized provider status, and support a mock provider in isolated tests; the final ASP.NET service will call home-machine model services through loopback configuration.
- Durable AI resume needs job input separate from job results plus per-chunk checkpoints. Completed translation chunks must retain exact source Cue IDs so retry/restart can skip paid work without changing the subtitle timeline.
- The isolated phase 31 run translated the two imported English cues into a two-cue `zh-CN` track, preserved the original 500-2500 ms first-cue timing, and generated a separate three-cue mock speech-recognition track.
- Mobile visual QA shows the selected Chinese AI track rendered in the native player and the AI task panel fits between the subtitle selector and video metadata without overlap.
- Direct PowerShell inspection of the SQLite checkpoint table was abandoned because the standalone PowerShell host could not resolve the bundled native `e_sqlite3` dependency; the application itself opened the same database successfully and the E2E job completion/retry path verified persisted behavior through public APIs.
- The home server exposes an RTX 3060 Ti with 8192 MiB VRAM (compute capability 8.6); NVIDIA driver 596.21 reports CUDA 13.2 driver compatibility, but no CUDA Toolkit, cuBLAS, or cuDNN installation was found. Driver compatibility is not a substitute for the runtime DLLs required by CTranslate2.
- The current environment has Python 3.11.9, .NET SDK 10.0.201, Node 22.16.0, and the existing FFmpeg integration; DreamyCinema is already listening on LAN port 5210 while ports 8001 and 8080 are unused.
- The current faster-whisper README requires cuBLAS for CUDA 12 and cuDNN 9 for CUDA 12 with current CTranslate2. A full CUDA Toolkit install is unnecessary if the exact runtime DLLs are deployed beside the isolated service and added only to that process PATH.
- Official llama.cpp Windows releases provide a CUDA 12 build and a matching CUDA 12.4 runtime archive. The official Qwen3-8B GGUF publishes `Qwen3-8B-Q4_K_M.gguf` at about 5.03 GB and documents `/no_think`; this is the first translation model configuration to test with an 8192-token context.
- The Git working tree is clean for `DreamyCinemaSite`; unrelated uncommitted files exist only under sibling project `SkipDrama_YuanShen` and must not be touched.
- The installed home runtime is faster-whisper 1.2.1 with CTranslate2 4.8.1, CUDA 12.8 cuBLAS plus cuDNN 9.8 runtime DLLs, official llama.cpp `b10015` with its CUDA 12.4 runtime, and official Qwen3-8B Q4_K_M SHA256 `d98cdcbd03e17ce47681435b5150e34c1417f50b5c0019dd560e4882c5745785`.
- Windows bsdtar 3.5.2 cannot extract the maintained CUDA runtime's LZMA-compressed 7z archive. The repeatable setup now uses 7-Zip 26.02 and validates download sizes so partial files resume instead of being mistaken for complete downloads.
- Python 3.8+ does not use PATH alone for dependent extension DLL discovery. Calling `os.add_dll_directory` before importing faster-whisper is required even when `cublas64_12.dll` and `cudnn64_9.dll` are present on PATH.
- llama.cpp `b10015` accepts schema-constrained `response_format`. Sending that field together with top-level `json_schema` creates conflicting grammars, and request-level `reasoning_format=deepseek` introduces an assistant thinking prefix that the grammar rejects with HTTP 400. The compatible combination is `response_format` alone plus server-level `--reasoning off`, `enable_thinking=false`, and `/no_think`; application-side Cue validation remains mandatory.
- Qwen3-8B Q4_K_M used about 6.7 GB total GPU memory at 8192 context and entered automatic sleep after five idle seconds. The verified sensitive-language sample retained profanity as “他妈” and kept gunshot/blood content without filtering.
- faster-whisper large-v3 `int8_float16` used about 3.1 GB total GPU memory and accurately recognized three timestamped sentences from an 8.73-second test WAV. Releasing the model after the final chunk returned total GPU usage to about 1.1 GB.
- Both model services bind only `127.0.0.1`; llama sleep and Whisper load coordination prevent simultaneous model allocation while DreamyCinema's single worker serializes recognition and translation jobs.
- The isolated real-provider integration translated 12 cues after a forced ASP.NET restart (`AttemptCount = 2`), preserved all Cue IDs and byte-for-byte timing lines, retained explicit language, cancelled an in-flight Whisper job, and completed the retry from chunk checkpoints (`AttemptCount = 2`, 12 recognized cues).
- A deterministic invalid-output test returned a missing Cue set, then a duplicate Cue set, then a valid set. The pipeline rejected and recorded the first two attempts and accepted only call 3, confirming validation errors use the configured automatic retry path.
- Before final deployment startup, no production `Videos` or `Data` directory existed in this checkout. No real source video is present after creation of the empty production structure, so a ten-minute household-video benchmark and phone viewing cannot be truthfully completed until one is added.
- Final deployment startup created the previously absent empty production directories and database, with first-run administrator setup still required. No video was imported or moved. DreamyCinema listens on `0.0.0.0:5210`; anonymous AI status returns 401 while both model services remain on `127.0.0.1`.
- The isolated 600-second benchmark used two 300-second Whisper chunks. Recognition retry completed in 172.2 seconds (about 3.48x real-time) with 132 cues; translation plus forced application restart completed in 8.5 seconds. Peak GPU memory was 6893 MiB and peak total system memory in use was about 15.1 GiB.
- A read-only scan of the current user's standard Videos/Downloads folders found several 15-23 minute game recordings that could serve as real-media acceptance candidates. They were not copied or imported because selecting personal media and completing phone-based human acceptance requires the user's choice and first-run administrator setup.
- The first real household translation exposed a semantic-quality gap. Its Japanese source track was correctly tagged `ja`, both source and translated tracks contained the same 47 Cue indexes/timings, but most Chinese text was unrelated hallucination (for example, rain-cloud content became hair/iceberg content). Structural Cue validation therefore worked, but semantic fidelity validation did not exist.
- Direct A/B calls with the same first 5 and then full 30 Japanese cues produced correct translations with schema-constrained, JSON-object, and prompt-only output. This rules out deterministic database misalignment, UTF-8 corruption, language metadata, and JSON grammar as the cause. The stored track came from an unstable model generation that remained structurally valid; its first bad batch then contaminated the next batch through rolling translated context.
- The affected translation job completed in one job attempt, with its first chunk recording two provider attempts and its second chunk one. Reusing the same source/profile currently returns the existing generated track by `SourceKey`, so clicking translate again cannot repair this bad track without an explicit retranslation path.
- The root cause was nested JSON serialization: `JsonSerializerOptions.Web` encoded Japanese inside the prompt as literal `\uXXXX` sequences. The same first 30 cues required 4126 prompt tokens through the application but about 1118 tokens when sent as real Unicode; the model produced correct translations in the latter form.
- After relaxed Unicode prompt encoding, 12-cue chunks, temperature 0, and semantic review were implemented, the real C# Provider translated all 47 exported Japanese cues coherently in four batches with 6196 total translation-plus-review tokens. No hair/hat/iceberg drift remained.
- The existing Qwen3-8B Q4_K_M model is not the immediate blocker: the corrected Provider produced coherent translations for the same real source, so changing models before fixing prompt transport would only mask the application defect. Model replacement should be reconsidered only if manual review still finds systematic errors after corrected retranslation.
- User reports the corrected deployed real-video and phone subtitle acceptance found no material issue, satisfying the remaining phase 31 manual gate.
- Phase 32 must never edit `SourceOriginal`, `RawRecognition`, or `ChineseDraft` in place. Human changes belong to `SourceCorrected` or `FinalPolished`; partial retranslation must preserve unrelated cues and explicit manual edits outside the selected range.
- Creating a review-stage track lazily on the first edit keeps normal playback uncluttered while preserving complete provenance. `IsManuallyEdited` at Cue level is sufficient for selected-Cue retranslation to protect human work deterministically.
- Deterministic quality warnings use missing text, exact timing comparison, 32-character length, and 15 characters/second thresholds. These warnings identify review candidates without asking an AI model to judge every page load.
## 2026-07-24 - Phase 33 backup scope

- Backups should be created by the server, not by copying the live SQLite file directly, so the database state is transactionally consistent while the site is running.
- The default package must remain small and portable: include metadata, covers, subtitle state, glossary, and non-secret settings; exclude `Videos/originals`, trash media, credentials, cookies, API keys, and machine-specific runtime endpoints.
- Backup packages need a format version plus per-file size and SHA-256 metadata so phase 34 restore can validate a package before touching live data.
- The stable phase 33 package layout is `manifest.json`, one `database/dreamy-cinema.db`, `configuration/portable-settings.json`, and referenced files below `assets/covers` and `assets/subtitles`.
- Copying the live database's WAL journal mode can leave `-wal`/`-shm` sidecars in a snapshot directory even after a successful online backup. Force the destination snapshot to `journal_mode=DELETE`, close non-pooled connections, and remove sidecars before hashing/package creation.
- Portable AI settings are an explicit whitelist of translation policy, limits, names, and glossary. Provider URLs, model runtime paths, API keys, credentials, cookies, `appsettings.Local.json`, original videos, and trash videos are excluded.
- The backup drawer passed Pixel 7 Playwright visual inspection without overlap or clipping; sub-megabyte archives should display as KB rather than `0.0 MB`.

## 2026-07-24 - Phase 34 restore boundaries

- Restore should use the SQLite online backup API in the reverse direction instead of replacing an open database file; this avoids Windows file-handle replacement failures and remains compatible with live read connections.
- A restore coordinator must atomically block new HTTP writes and background media jobs before the pre-restore snapshot is taken. Restore should reject immediately when a write/job is already active rather than waiting behind a long AI task.
- Original and trash videos are never part of the package and must not be moved during restore. Exact restore scope is the database, `Videos/covers`, `Videos/subtitles`, and the portable non-secret AI settings file.
- Covers/subtitles need directory-level swap with rollback copies on the same volume. A database rollback snapshot and previous portable-settings bytes must remain available until post-restore integrity and missing-video status checks pass.
- Uploaded packages are untrusted input: reject unsupported versions, duplicate/unlisted ZIP entries, traversal/rooted paths, unexpected roots, original-video content, checksum/size mismatch, unsafe database paths, missing schema, failed foreign-key checks, and manifest count mismatches before maintenance mode changes live state.
- Restore validation permits database references to assets that were already missing when a backup was created, because phase 33 records those omissions as warnings; it still rejects any packaged cover/subtitle file that the database does not reference.
- Portable settings load before `appsettings.Local.json`, environment variables, and command-line arguments. This restores translation policy/glossary while keeping machine-local provider URLs, models, paths, and secrets authoritative.
- A one-shot failure injected immediately after reverse SQLite application proved the rollback order restores the safety database and prior cover/subtitle directories before releasing maintenance mode; a second restore in the same process then succeeded.
- ZIP expansion is limited globally, while `manifest.json` and portable settings have tighter 16 MB and 4 MB limits so malformed administrator uploads cannot force unbounded JSON parsing.

## 2026-07-24 - Phase 35 bulk-maintenance boundaries

- Bulk selection applies only to explicitly selected, currently loaded video IDs; it does not silently operate on unloaded search/filter results.
- Existing single-video trash already preflights file locks and reverses a move after a database failure; the batch endpoint must extend that invariant across every selected file by preflighting the complete set before the first move and reversing all completed moves on failure.
- `MediaJob.InputJson`, retry, cancellation polling, restart recovery, and writer coordination already support a durable maintenance job without a new database table. Phase 35 only needs a new job type/handler and an idempotent per-video runner.
- `MediaAnalyzer` currently owns the ffprobe/ffmpeg timeout but does not accept an external cancellation token. Adding an optional token to analyzer operations lets job cancellation terminate the child process instead of waiting for the tool timeout.
- Automatic sync covers use a deterministic `-auto.jpg` path. Explicit regeneration should generate a unique replacement first, update the database, and only then delete the previous cover so a failed ffmpeg/database operation cannot destroy the usable cover.
- The mobile header already contains four 40px commands beside the title, so adding a fifth bulk button there would compress the first viewport. Selection mode belongs in a compact library-content action row, with a sticky bottom selection bar and a dedicated drawer.
- The Pinia library store currently resumes and formats only `Sync` jobs. `MediaMaintenance` can reuse the same active-job panel after type-aware result formatting; subtitle jobs remain owned by the player/review screens.
- Pixel 7 viewport inspection shows the bulk drawer header, close control, add/remove segmented control, categorized tag chips, primary action, resolution action, and media buttons remain readable without horizontal clipping. Lower destructive actions are intentionally reached through the drawer's independent vertical scroll.
- Batch tag edits are immediate relational database operations, while FFprobe/FFmpeg reprocessing belongs in the existing durable single-worker queue so it remains cancellable and retryable.
- Batch trash must preflight every source/destination before moving the first file, then reverse all completed file moves if the database transaction fails.
- Resolution labels are suggestions, not automatic edits. The server derives groups from the shorter video dimension and only replaces tags in the `清晰度` category after a separate administrator confirmation.

## 2026-07-28 - Phase 39 mobile subtitle rendering

- `VideoCard.vue` currently relies only on the `<track default>` attribute, while `PlayerView.vue` explicitly changes `TextTrack.mode`; the inline player therefore has the weaker vendor-browser path.
- A DOM subtitle overlay can cover standards-composited inline video but cannot be guaranteed above a vendor browser's separate native video surface. A sibling caption bar below the video remains visible while the page stays inline and is the required Xiaomi compatibility mode.
- The compatibility implementation should keep WebVTT as the source, set the selected browser track to `hidden`, and render `activeCues` as escaped Vue text. Native mode remains available for browsers that already render tracks correctly.
- Touch/coarse-pointer devices should default to below-video captions, while desktop defaults to the self-rendered in-picture overlay. The selected mode and language/track preference should remain device-local.
- The isolated Pixel 7/Edge run loaded a real generated H.264 test video, imported two SRT Cues, sought to one second, and rendered the expected Cue in the below-video bar. It also switched successfully through overlay, native `TextTrack.mode = showing`, and back to below-video.
- Visual inspection of `mobile-subtitle-display.png` shows the 46px compatibility bar directly below the native video controls with no overlap, clipping, or horizontal overflow. Actual Xiaomi Browser compositing remains a handset-only acceptance check.

## 2026-07-28 - Phase 40 caption-preserving fullscreen

- Xiaomi handset verification confirmed the below-video compatibility bar works inline but disappears when the browser's native video fullscreen control is used. That control promotes only `<video>`, so sibling DOM captions cannot follow it.
- The dedicated player must request fullscreen on `.player-stage`, which owns both video and captions. A separate CSS fixed-position viewport mode is required when element fullscreen is unavailable or rejected.
- `controlsList="nofullscreen"` is only a browser hint. The product must expose an unambiguous “带字幕全屏” action and retain an in-stage exit action because vendor controls may ignore the hint.
- Pixel 7 visual QA of the CSS fallback showed that an exit button positioned over the video frame can be covered by the same native video surface even though Playwright considers the DOM button visible. Fullscreen controls must therefore live in the sibling compatibility caption bar, not over the picture.
- Caption-preserving fullscreen should temporarily force the below-video mode and restore the prior native/overlay/below preference on exit. This guarantees both subtitle text and the exit action remain outside the vendor video surface.
- The final forced-fallback Pixel 7 E2E fills the complete 1080 x 2400 viewport, renders the active Cue and exit action in the sibling caption strip, and restores the prior native mode plus `TextTrack.mode = showing` after exit.

## 2026-07-30 - Phase 40 handset result and rollback

- Real Xiaomi Browser validation failed even after requesting fullscreen on the whole player stage and retaining a CSS viewport fallback. The vendor playback layer remains authoritative in fullscreen, so a sibling DOM subtitle solution is not reliable on this target browser.
- Phase 40 is rolled back. Phase 39 remains useful for inline and non-fullscreen playback, but fullscreen compatibility now requires either subtitles decoded by the native player or subtitle pixels rendered into a derived video.
- Merely muxing a subtitle track into MP4 avoids video re-encoding but still depends on Xiaomi Browser subtitle-track support. The reliable fallback is an offline FFmpeg hard-subtitle derivative, which decodes the original, renders subtitle glyphs into each frame, and re-encodes the video while usually stream-copying compatible audio.
- Hard-subtitle generation should be a durable background job after subtitle review, never an on-play transcode. Keep the original canonical, write a versioned derived file, invalidate it when the selected subtitle revision changes, and serve the completed derivative with normal HTTP Range seeking.
- For the home 5600X/32 GB/RTX 3060 Ti machine, start with H.264 NVENC, a quality-target mode around CQ 19-21, `yuv420p`, AAC-compatible audio, and `faststart`. Benchmark a representative 10-minute segment before fixing production defaults.

## 2026-07-30 - Phase 42 hard-subtitle derivatives

- A generated hard-subtitle file is a playable derivative, not a replacement until the administrator explicitly deletes the original. Generation writes to `Videos/derived/.work`, validates H.264 output, moves the completed file into `Videos/derived/hard-subtitles/{videoId}`, commits its database pointer, and only then removes the previous derivative.
- Playback source selection must be explicit when both files exist. The unsuffixed stream URL prefers the original and falls back to the hard-subtitle file only when the original is absent; explicit `source=original|hard-subtitle` URLs make UI selection and failure behavior testable.
- Editing the selected subtitle track after generation marks the derivative stale. A stale derivative remains playable, but original deletion is blocked until it is regenerated so the only retained video cannot silently contain obsolete subtitles.
- Permanent original deletion requires a fresh nonempty derivative, no active hard-subtitle job, and exact video-number confirmation. The original is first renamed into a same-volume work directory so a failed database save can restore it.
- Metadata backups continue to exclude all large media, including `Videos/derived`. Restoring or importing a database clears a hard-subtitle pointer if the corresponding derived file is absent; moving to another machine therefore requires copying the media directories separately.
- The isolated libx264 run burned two Chinese SRT Cues into a six-second H.264/AAC MP4. A frame extracted at one second visibly contains `硬字幕合并测试`; Pixel 7 E2E verified source switching, derivative deletion/recreation, exact-confirmation rejection, derived/default `206` range playback, explicit-original `404` after deletion, and no work-file residue.

## 2026-07-30 - Phase 43 encoder fallback

- The reported FFmpeg log reaches the `subtitles` filter successfully and fails at `h264_nvenc` with `Cannot load nvcuda.dll`, before any output packet is produced. The subtitle path and font provider are not the cause.
- `h264_nvenc` must remain preferred for the home RTX 3060 Ti server, but a development machine without an available NVIDIA driver needs an automatic `libx264` retry. The fallback matcher must be limited to encoder/CUDA initialization signatures so invalid subtitle syntax, source corruption, disk failures, and ordinary cancellation are not hidden.
- The deterministic fallback test injects the NVENC initialization error only in the `Testing` environment, then runs the real FFmpeg `libx264` path. Its completed job reports `encoder=libx264`; ffprobe confirms the output is H.264 640x360 and the work directory is empty.

## 2026-07-30 - Phase 44 hard-subtitle style

- Current hard-subtitle size is fixed by `HardSubtitle:FontSize` and passed to libass through FFmpeg's `force_style`; the user cannot change it per generated video.
- Styling must be captured in the durable job input and stored with the generated derivative. Reading mutable global configuration only when the worker starts would make queued jobs and regeneration non-reproducible.
- ASS/libass colors use `&HAABBGGRR`: channels are reversed from CSS `#RRGGBB`, and alpha is inverted (`00` opaque, `FF` transparent). The API should accept familiar CSS colors plus an opacity percentage and perform this conversion server-side.
- The settings UI can preview against the video's real cover, but the persisted values and actual FFmpeg output remain authoritative because browser text rendering is not pixel-identical to libass.
- A real 640x360 fixture generated with `16 px`, `#F2E7C4`, 80% text opacity, `#101820` one-pixel outline, and 18-pixel bottom margin visibly burned the intended smaller styled text into the extracted frame. The durable job result and video API returned the same normalized style.
# 2026-07-31 - Phase 45 Android client

- The Android client will share the current ASP.NET Core API, SQLite database, media directories, authentication policy, and background jobs. It is a third presentation client, not a separate server or datastore.
- The implementation target is a native Kotlin Android app with Media3/ExoPlayer rather than a WebView. A native player can keep side-loaded WebVTT captions in the player surface during Android fullscreen and avoids the Xiaomi Browser native-video/DOM-caption boundary.
- The workstation has Android SDK platforms 35/36, build-tools 36.0.0, platform-tools/ADB, and the .NET Android workload, but no usable Java SDK or Gradle installation was found. Kotlin/Gradle builds therefore need a project-local or installed JDK 17 plus a checked-in Gradle wrapper.
- The first release should remain a trusted-LAN client: configurable `http://<server-ip>:5210`, cleartext enabled only because the existing home deployment is HTTP, no credentials in URLs, and no public-Internet exposure assumption.
- The Android app must implement the existing anti-forgery flow for login and future writes: first obtain the anonymous session/CSRF response, retain every response Cookie, then send the CSRF header and Cookie jar with the login request.
- The completed client uses API 36/minSdk 26, AGP 9.0.1, Gradle 9.1.0, Microsoft OpenJDK 17.0.20, Material 3 Views, coroutines, and Media3 1.10.1.
- Server Cookies are encrypted at rest with an Android Keystore AES-GCM key. Changing the configured server clears the old Cookie jar; the administrator password is used only for the login request and is never persisted.
- Media3 receives the same authenticated Cookie header for the MP4 stream and side-loaded WebVTT. Original/hard-subtitle source changes rebuild the `MediaItem` at the current position, while the subtitle selector attaches exactly one chosen VTT track or disables text.
- Android 9 and later block cleartext by default. Because the personal server address is an arbitrary LAN IP and currently uses HTTP, the App intentionally opts into cleartext globally and labels the connection as trusted-LAN-only; this must be revisited if HTTPS or VPN deployment becomes the default.
- There is no emulator/system image or attached ADB device on the workstation. The Debug APK and static/toolchain checks are complete, but Xiaomi fullscreen-caption behavior is not claimed as handset-verified.

# 2026-07-31 - Phase 46 Android parity

- The Xiaomi screenshot shows the Android library toolbar drawing underneath the status bar; API 36 edge-to-edge defaults require explicit system-bar Insets on each top-level Activity instead of relying on the theme background.
- The first Android card rendered all tags into one `TextView` and assigned only a root-card click listener. Consequently, tapping a visible tag bubbles to card playback. Tags need individual clickable Chip/TextView targets and a callback into the library's selected-tag intersection set.
- The first Android milestone intentionally omitted management workflows, but that scope does not match the user's expected mobile product. Native Android now needs the existing web capabilities rather than a browse-only subset.
- The Android client should remain a presentation and control layer. Sync, tag mutations, recycle-bin moves, backup validation/restore, AI inference, and FFmpeg hard-subtitle generation continue to execute on the ASP.NET Core server against the shared SQLite/media storage.
- Android document-provider streams are required for backup import/export. Reading a complete ZIP into a `ByteArray` would create avoidable out-of-memory risk as cover/subtitle collections grow.
- The server's durable `MediaJob` contract is sufficient for both clients: Android can discover the latest relevant job, render its stage/progress/error, request cancellation or retry, and refresh the video only after terminal completion.
- The code build cannot prove Xiaomi status-bar or fullscreen behavior. An attached handset is required to validate OEM Insets, touch target dispatch, Media3 fullscreen captions, and long-running task recovery after backgrounding the App.
