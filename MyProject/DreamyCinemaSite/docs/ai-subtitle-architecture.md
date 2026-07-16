# AI Subtitle Architecture

## Goal

Generate reviewable Chinese subtitles without coupling DreamyCinema to one AI vendor. Speech recognition preserves an original-language timeline; translation works on stable cue IDs and cannot modify timing.

## Runtime Layout

```text
Vue player
  -> ASP.NET Core AI job endpoint
  -> SQLite MediaJobs + AiJobChunks
  -> MediaJobWorker (one job at a time)
     -> ISpeechRecognitionProvider
     -> ISubtitleTranslationProvider
  -> SubtitleTracks + SubtitleCues
  -> WebVTT playback
```

The final home deployment keeps all services on the same computer:

```text
DreamyCinema             http://0.0.0.0:5210
faster-whisper service   http://127.0.0.1:8001
llama.cpp / Qwen service http://127.0.0.1:8080
```

Only DreamyCinema is reachable from the LAN. Model services listen on loopback and are never called directly by a phone or company computer.

## Phase 31 Implementation

- AI configuration is disabled by default.
- Provider-neutral speech and translation interfaces are implemented, including real `FasterWhisper` and `LlamaCpp` adapters.
- Mock providers are available only when explicitly configured.
- FFmpeg extracts mono 16 kHz WAV chunks; the default real chunk length is five minutes, so a two-hour WAV is never uploaded as one request.
- Recognition and translation run as durable `MediaJobs`.
- `MediaJobs.InputJson` stores immutable task input.
- `AiJobChunks` stores completed provider output and usage counters.
- Provider failures and invalid output retry up to the configured limit; retry and service restart reuse completed chunks.
- Translation output must contain every source Cue ID exactly once with non-empty text.
- Generated subtitles copy source cue index and timing from the database.
- The Worker monitors persisted cancellation while a job is running and propagates it to FFmpeg and provider HTTP requests.
- The public status API never returns provider secrets.

The mock provider validates orchestration only. Its output is clearly marked and must not be used as a real subtitle.

## Installed Home Runtime

The repeatable installer keeps all large files outside the repository at `%LOCALAPPDATA%\DreamyCinema\AI`:

- faster-whisper 1.2.1 and CTranslate2 4.8.1;
- maintained CUDA 12 cuBLAS plus cuDNN 9 runtime DLLs, without the full CUDA Toolkit;
- official faster-whisper large-v3, using `int8_float16` on CUDA;
- official llama.cpp Windows CUDA build `b10015` and its CUDA 12.4 runtime;
- official Qwen3-8B Q4_K_M GGUF, SHA256 `d98cdcbd03e17ce47681435b5150e34c1417f50b5c0019dd560e4882c5745785`.

The model services are lazy/idle managed. llama-server uses one slot, an 8192-token context, CUDA offload, non-thinking mode, and sleeps after five idle seconds. The Whisper service refuses to allocate while llama is active and unloads after the final audio chunk. DreamyCinema has one job Worker, so recognition and translation are never intentionally scheduled in parallel.

## Translation Contract

Each provider request receives source and target languages, consecutive stable Cue IDs, the prior source ending, the prior translated ending, character names, glossary, translation style, and a flag requiring explicit language and sensitive expressions to remain faithful.

Provider output contains only Cue ID and translated text. The llama adapter requests a schema-constrained JSON response, uses `/no_think` and disables template thinking, then parses and validates the result again in the application. Missing, duplicated, unknown, empty, malformed, fenced, or thinking-tag output is rejected and retried. The model never supplies timestamps.

Prompts explicitly require faithful translation of profanity, sex, violence, and sensitive language without censorship, moralizing, softening, omission, or euphemistic replacement. Full cues, prompts, cookies, keys, and authorization headers are not logged.

## Subtitle Data Boundaries

`SubtitleTracks.RevisionStage` prevents later review work from overwriting an earlier artifact:

| Stage | Purpose |
| --- | --- |
| `SourceOriginal` | Imported external or embedded source subtitle |
| `RawRecognition` | Immutable first output from speech recognition |
| `SourceCorrected` | Future human-corrected source transcript |
| `ChineseDraft` | Current AI translation output |
| `FinalPolished` | Future reviewed/final Chinese subtitle |

Phase 31 creates `RawRecognition` and `ChineseDraft` as separate tracks. Phase 32 editing and retranslation must create or update only the appropriate review-stage track; it must retain the raw recognition track and the initial Chinese draft.

## Mock Configuration

Use environment variables only in an isolated development runtime:

```powershell
$env:AiSubtitles__Enabled = "true"
$env:AiSubtitles__Speech__Provider = "Mock"
$env:AiSubtitles__Speech__Model = "mock-speech"
$env:AiSubtitles__Translation__Provider = "Mock"
$env:AiSubtitles__Translation__Model = "mock-translation"
```

Real home configuration is copied from `appsettings.Local.example.json` to the ignored `appsettings.Local.json`. API keys must come from protected local configuration or environment variables and must never be returned by `/api/ai-subtitles/status`, logged, or included in backups.

## Operations

```powershell
.\scripts\Setup-LocalAi.ps1
.\scripts\Start-LocalAi.ps1
.\scripts\Stop-LocalAi.ps1
.\scripts\Start-HomeServer.ps1
.\scripts\Test-LocalAiIntegration.ps1
```

The integration test builds an isolated application, database, credentials file, and Videos directory. It forces an application restart during translation, checks checkpoint recovery and exact timing equality, cancels and retries real speech recognition, and verifies that explicit language remains present. It deletes the isolated data after success.

A 600-second isolated benchmark has passed with two production-size 300-second audio chunks: recognition completed in 172.2 seconds (about 3.48x real-time), translation plus forced restart in 8.5 seconds, peak GPU memory at 6893 MiB, and peak total system memory in use at about 15.1 GiB. The remaining Phase 31 acceptance item is manual phone playback using a real household video. Final deployment has an empty production library, so that content-dependent check must wait until one is added; automated tests must not substitute for manual acceptance or modify production media.

## Upstream References

- faster-whisper installation and CUDA requirements: <https://github.com/SYSTRAN/faster-whisper>
- CTranslate2 Windows GPU installation: <https://opennmt.net/CTranslate2/installation.html>
- llama.cpp official Windows releases: <https://github.com/ggml-org/llama.cpp/releases>
- llama-server OpenAI-compatible API and reasoning controls: <https://github.com/ggml-org/llama.cpp/blob/master/tools/server/README.md>
- official Qwen3-8B GGUF files and `/no_think` usage: <https://huggingface.co/Qwen/Qwen3-8B-GGUF>

