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

## Current Phase 31 Slice

- AI configuration is disabled by default.
- Provider-neutral speech and translation interfaces are implemented.
- Mock providers are available only when explicitly configured.
- Recognition and translation run as durable `MediaJobs`.
- `MediaJobs.InputJson` stores immutable task input.
- `AiJobChunks` stores completed provider output and usage counters.
- Retry and service restart reuse completed chunks.
- Translation output must contain every source Cue ID exactly once with non-empty text.
- Generated subtitles copy source cue index and timing from the database.
- The public status API never returns provider secrets.

The mock provider validates orchestration only. Its output is clearly marked and must not be used as a real subtitle.

## Translation Contract

Each provider request receives source and target languages, stable Cue IDs, source text, previous translated context, translation style, and a flag requiring explicit language and sensitive expressions to remain faithful.

Provider output contains only Cue ID and translated text. The application rejects missing, duplicated, unknown, or empty Cue results. It never accepts model-generated timestamps.

## Mock Configuration

Use environment variables only in an isolated development runtime:

```powershell
$env:AiSubtitles__Enabled = "true"
$env:AiSubtitles__Speech__Provider = "Mock"
$env:AiSubtitles__Speech__Model = "mock-speech"
$env:AiSubtitles__Translation__Provider = "Mock"
$env:AiSubtitles__Translation__Model = "mock-translation"
```

The real home providers will use the same interfaces and settings but replace `Mock` with the supported local adapter. API keys must come from protected local configuration or environment variables and must never be returned by `/api/ai-subtitles/status`, logged, or included in backups.

## Remaining Phase 31 Work

1. Add segmented audio extraction and the real faster-whisper/OpenAI-compatible speech adapter.
2. Add the llama.cpp OpenAI-compatible structured translation adapter.
3. Persist glossary, character names, rolling context summary, and translation profile.
4. Add automatic retries for invalid provider output and explicit usage-limit errors.
5. Benchmark a ten-minute sample on the home RTX 3060 Ti before processing full videos.

