/**
 * The browser adapter of the voice recorder port (UC-CHT-I04): the microphone through getUserMedia, the encoder through MediaRecorder
 * and a live level and waveform through an AnalyserNode. The composer uses it unless a host passes another recorder. It is not run in
 * unit tests (the tests use a fake port); the state machine it feeds is tested in core/model/voice-model.test.
 */

import {
  ChatVoiceRecorderError,
  type ChatVoiceRecorder,
  type VoiceRecorderHandlers,
  type VoiceRecording,
} from "../../core/api/chat-voice-recorder";
import { waveformFromSamples } from "../../core/model/voice-model";

/** The sampling tick of the level meter and the waveform, in milliseconds. */
const TICK_MS = 100;
/** Encoder bit rate: 32 kbit/s Opus is clear for speech and keeps five minutes under the server's byte ceiling. */
const AUDIO_BITS_PER_SECOND = 32_000;
const MIME_CANDIDATES = [
  "audio/webm;codecs=opus",
  "audio/webm",
  "audio/mp4",
] as const;

type WindowWithAudio = Window & typeof globalThis;

function supportedMime(): string | undefined {
  return MIME_CANDIDATES.find((type) => MediaRecorder.isTypeSupported(type));
}

function denialOf(error: unknown): "denied" | "failed" {
  const name =
    error instanceof DOMException
      ? error.name
      : error instanceof Error
        ? error.name
        : "";
  return name === "NotAllowedError" || name === "SecurityError"
    ? "denied"
    : "failed";
}

/** A recorder over the browser APIs. One instance records one message at a time. */
export function createBrowserVoiceRecorder(): ChatVoiceRecorder {
  let recorder: MediaRecorder | null = null;
  let stream: MediaStream | null = null;
  let context: AudioContext | null = null;
  let timer: ReturnType<typeof setInterval> | null = null;
  let chunks: Blob[] = [];
  let samples: number[] = [];
  let elapsedMs = 0;
  let lastTick = 0;
  let paused = false;

  function teardown(): void {
    if (timer !== null) clearInterval(timer);
    timer = null;
    stream?.getTracks().forEach((track) => track.stop());
    stream = null;
    void context?.close();
    context = null;
    recorder = null;
  }

  return {
    isSupported() {
      const win = globalThis as WindowWithAudio;
      return (
        typeof win.MediaRecorder !== "undefined" &&
        typeof navigator !== "undefined" &&
        typeof navigator.mediaDevices?.getUserMedia === "function"
      );
    },

    async start(handlers: VoiceRecorderHandlers) {
      if (!this.isSupported()) throw new ChatVoiceRecorderError("unsupported");
      try {
        stream = await navigator.mediaDevices.getUserMedia({ audio: true });
      } catch (error) {
        throw new ChatVoiceRecorderError(denialOf(error));
      }

      const mime = supportedMime();
      chunks = [];
      samples = [];
      elapsedMs = 0;
      paused = false;
      try {
        recorder = new MediaRecorder(stream, {
          ...(mime ? { mimeType: mime } : {}),
          audioBitsPerSecond: AUDIO_BITS_PER_SECOND,
        });
        recorder.ondataavailable = (event) => {
          if (event.data.size > 0) chunks.push(event.data);
        };
        context = new AudioContext();
        const analyser = context.createAnalyser();
        analyser.fftSize = 512;
        context.createMediaStreamSource(stream).connect(analyser);
        const buffer = new Float32Array(analyser.fftSize);
        recorder.start(250);
        lastTick = performance.now();
        timer = setInterval(() => {
          const now = performance.now();
          if (!paused) elapsedMs += now - lastTick;
          lastTick = now;
          analyser.getFloatTimeDomainData(buffer);
          let sum = 0;
          for (const value of buffer) sum += value * value;
          const rms = Math.sqrt(sum / buffer.length);
          const level = Math.min(1, rms * 4);
          if (!paused) {
            samples.push(level);
            handlers.onLevel(level);
            handlers.onElapsed(elapsedMs);
          }
        }, TICK_MS);
      } catch {
        teardown();
        throw new ChatVoiceRecorderError("failed");
      }
    },

    pause() {
      if (recorder?.state === "recording") {
        recorder.pause();
        paused = true;
      }
    },

    resume() {
      if (recorder?.state === "paused") {
        recorder.resume();
        paused = false;
        lastTick = performance.now();
      }
    },

    stop(): Promise<VoiceRecording> {
      const active = recorder;
      if (!active) return Promise.reject(new ChatVoiceRecorderError("failed"));
      return new Promise<VoiceRecording>((resolve, reject) => {
        active.onstop = () => {
          const blob = new Blob(chunks, {
            type: active.mimeType || "audio/webm",
          });
          const recording: VoiceRecording = {
            blob,
            durationMs: Math.round(elapsedMs),
            waveform: waveformFromSamples(samples),
          };
          teardown();
          resolve(recording);
        };
        active.onerror = () => {
          teardown();
          reject(new ChatVoiceRecorderError("failed"));
        };
        if (active.state !== "inactive") active.stop();
        else active.onstop?.(new Event("stop"));
      });
    },

    cancel() {
      if (recorder && recorder.state !== "inactive") {
        recorder.onstop = null;
        recorder.stop();
      }
      chunks = [];
      samples = [];
      teardown();
    },
  };
}
