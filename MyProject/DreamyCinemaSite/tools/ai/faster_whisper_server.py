"""Loopback-only, OpenAI-shaped faster-whisper service for DreamyCinema."""

from __future__ import annotations

import asyncio
import gc
import json
import os
import tempfile
import threading
import time
import urllib.error
import urllib.request
from contextlib import asynccontextmanager
from pathlib import Path
from typing import Annotated

# Python 3.8+ no longer searches PATH for dependent DLLs loaded by extension modules.
# Keep these handles alive for the process lifetime so CTranslate2 can load cuBLAS/cuDNN.
_dll_directory_handles = []
if os.name == "nt":
    for _directory in os.environ.get("DREAMY_CUDA_DLL_DIRS", "").split(os.pathsep):
        if _directory and os.path.isdir(_directory):
            _dll_directory_handles.append(os.add_dll_directory(_directory))

import uvicorn
from fastapi import FastAPI, File, Form, HTTPException, Request, UploadFile
from faster_whisper import WhisperModel


MODEL_PATH = os.environ.get("DREAMY_WHISPER_MODEL", "large-v3")
DEVICE = os.environ.get("DREAMY_WHISPER_DEVICE", "cuda")
COMPUTE_TYPE = os.environ.get("DREAMY_WHISPER_COMPUTE_TYPE", "int8_float16")
IDLE_UNLOAD_SECONDS = max(10, int(os.environ.get("DREAMY_WHISPER_IDLE_UNLOAD_SECONDS", "60")))
MAX_UPLOAD_BYTES = max(1, int(os.environ.get("DREAMY_WHISPER_MAX_UPLOAD_MB", "64"))) * 1024 * 1024
LLAMA_PROPS_URL = os.environ.get("DREAMY_LLAMA_PROPS_URL", "http://127.0.0.1:8080/props")
LLAMA_SLEEP_WAIT_SECONDS = max(10, int(os.environ.get("DREAMY_LLAMA_SLEEP_WAIT_SECONDS", "120")))
BEAM_SIZE = max(1, int(os.environ.get("DREAMY_WHISPER_BEAM_SIZE", "5")))

_model: WhisperModel | None = None
_model_lock = asyncio.Lock()
_last_used = time.monotonic()


def _llama_is_sleeping() -> bool:
    try:
        with urllib.request.urlopen(LLAMA_PROPS_URL, timeout=2) as response:
            payload = json.load(response)
        return bool(payload.get("is_sleeping", False))
    except (urllib.error.URLError, TimeoutError, json.JSONDecodeError):
        # No llama-server means there is no competing model allocation.
        return True


async def _wait_for_llama_sleep() -> None:
    deadline = time.monotonic() + LLAMA_SLEEP_WAIT_SECONDS
    while not await asyncio.to_thread(_llama_is_sleeping):
        if time.monotonic() >= deadline:
            raise HTTPException(
                status_code=503,
                detail="llama.cpp did not enter sleep; start it with --sleep-idle-seconds before speech recognition",
            )
        await asyncio.sleep(1)


def _unload_model() -> None:
    global _model
    _model = None
    gc.collect()


async def _get_model() -> WhisperModel:
    global _model
    if _model is None:
        await _wait_for_llama_sleep()
        _model = await asyncio.to_thread(
            WhisperModel,
            MODEL_PATH,
            device=DEVICE,
            compute_type=COMPUTE_TYPE,
        )
    return _model


def _transcribe(
    model: WhisperModel,
    audio_path: str,
    language: str | None,
    cancelled: threading.Event,
) -> dict:
    segments, info = model.transcribe(
        audio_path,
        language=language or None,
        beam_size=BEAM_SIZE,
        vad_filter=True,
        condition_on_previous_text=True,
    )
    result_segments: list[dict] = []
    for index, segment in enumerate(segments):
        if cancelled.is_set():
            raise InterruptedError("client disconnected")
        text = segment.text.strip()
        if not text:
            continue
        result_segments.append(
            {
                "id": index,
                "start": float(segment.start),
                "end": float(segment.end),
                "text": text,
            }
        )
    return {
        "task": "transcribe",
        "language": info.language or language or "und",
        "duration": float(info.duration),
        "segments": result_segments,
    }


async def _idle_unloader() -> None:
    global _last_used
    while True:
        await asyncio.sleep(5)
        if _model is None or _model_lock.locked():
            continue
        if time.monotonic() - _last_used < IDLE_UNLOAD_SECONDS:
            continue
        async with _model_lock:
            if time.monotonic() - _last_used >= IDLE_UNLOAD_SECONDS:
                await asyncio.to_thread(_unload_model)


@asynccontextmanager
async def lifespan(_: FastAPI):
    task = asyncio.create_task(_idle_unloader())
    try:
        yield
    finally:
        task.cancel()
        await asyncio.gather(task, return_exceptions=True)
        await asyncio.to_thread(_unload_model)


app = FastAPI(title="DreamyCinema faster-whisper", docs_url=None, redoc_url=None, lifespan=lifespan)


@app.get("/health")
@app.get("/v1/health")
async def health() -> dict:
    return {
        "status": "ok",
        "model": Path(MODEL_PATH).name,
        "device": DEVICE,
        "compute_type": COMPUTE_TYPE,
        "loaded": _model is not None,
    }


@app.get("/v1/models")
async def models() -> dict:
    return {"object": "list", "data": [{"id": Path(MODEL_PATH).name, "object": "model"}]}


@app.post("/v1/audio/transcriptions")
async def transcriptions(
    request: Request,
    file: Annotated[UploadFile, File()],
    model: Annotated[str, Form()],
    language: Annotated[str | None, Form()] = None,
    response_format: Annotated[str, Form()] = "verbose_json",
    release_model: Annotated[bool, Form()] = False,
) -> dict:
    del model
    if response_format != "verbose_json":
        raise HTTPException(status_code=400, detail="only verbose_json is supported")

    suffix = Path(file.filename or "chunk.wav").suffix or ".wav"
    temp_path = ""
    size = 0
    try:
        with tempfile.NamedTemporaryFile(prefix="dreamy-whisper-", suffix=suffix, delete=False) as output:
            temp_path = output.name
            while data := await file.read(1024 * 1024):
                size += len(data)
                if size > MAX_UPLOAD_BYTES:
                    raise HTTPException(status_code=413, detail="audio chunk exceeds configured upload limit")
                output.write(data)
        if size == 0:
            raise HTTPException(status_code=400, detail="audio file is empty")

        async with _model_lock:
            global _last_used
            whisper = await _get_model()
            cancelled = threading.Event()
            worker = asyncio.create_task(
                asyncio.to_thread(_transcribe, whisper, temp_path, language, cancelled)
            )
            while not worker.done():
                if await request.is_disconnected():
                    cancelled.set()
                await asyncio.sleep(0.25)
            try:
                result = await worker
            except InterruptedError as exc:
                raise HTTPException(status_code=499, detail="client cancelled transcription") from exc
            finally:
                _last_used = time.monotonic()
                if release_model or cancelled.is_set():
                    await asyncio.to_thread(_unload_model)
            return result
    finally:
        await file.close()
        if temp_path:
            try:
                os.unlink(temp_path)
            except FileNotFoundError:
                pass


if __name__ == "__main__":
    uvicorn.run(app, host="127.0.0.1", port=8001, access_log=False, log_level="info")
