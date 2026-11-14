/**
 * Voice messages (UC-CHT-I04, I05): the recorder state machine, the playback helpers and the waveform shape. Pure, so the states
 * are tested without a browser. The server limits (length, byte ceiling, waveform size) are enforced in the backend; the constants
 * here mirror them for the UI and are never the only check.
 */

import type { VoiceRecording } from "../api/chat-voice-recorder";
import type { ChatAttachment } from "../types/chat";

/** The longest voice message, in milliseconds (the Workspace limit, default 5 minutes). */
export const VOICE_MAX_MS = 300_000;
/** A recording shorter than this is discarded, not sent (UC-CHT-I04: too short). */
export const VOICE_MIN_MS = 1_000;
/** The most waveform peaks a voice message carries (the server refuses more). */
export const VOICE_WAVEFORM_PEAKS = 64;

export type VoiceRecorderState =
  | { readonly status: "idle" }
  | { readonly status: "requesting" }
  | {
      readonly status: "recording";
      readonly elapsedMs: number;
      readonly levels: readonly number[];
      readonly paused: boolean;
    }
  | { readonly status: "review"; readonly recording: VoiceRecording }
  | { readonly status: "too-short" }
  | { readonly status: "denied" }
  | { readonly status: "unsupported" }
  | { readonly status: "failed" };

export type VoiceRecorderEvent =
  | { readonly type: "start" }
  | { readonly type: "granted" }
  | {
      readonly type: "refused";
      readonly reason: "denied" | "unsupported" | "failed";
    }
  | { readonly type: "elapsed"; readonly elapsedMs: number }
  | { readonly type: "level"; readonly level: number }
  | { readonly type: "pause" }
  | { readonly type: "resume" }
  | { readonly type: "stopped"; readonly recording: VoiceRecording }
  | { readonly type: "discard" }
  | { readonly type: "failed" };

/** How many level bars the live meter keeps. */
export const VOICE_LEVEL_BARS = 24;

/**
 * The state after one event. A stop before VOICE_MIN_MS is "too-short" (nothing to send, the person is told), not a review. Events that do not apply to the
 * current state are ignored, so a late timer tick after a discard changes nothing.
 */
export function reduceVoiceRecorder(
  state: VoiceRecorderState,
  event: VoiceRecorderEvent,
): VoiceRecorderState {
  switch (event.type) {
    case "start":
      return state.status === "idle" ||
        state.status === "review" ||
        state.status === "too-short" ||
        state.status === "denied" ||
        state.status === "unsupported" ||
        state.status === "failed"
        ? { status: "requesting" }
        : state;
    case "granted":
      return state.status === "requesting"
        ? { status: "recording", elapsedMs: 0, levels: [], paused: false }
        : state;
    case "refused":
      if (state.status !== "requesting") return state;
      return {
        status:
          event.reason === "denied"
            ? "denied"
            : event.reason === "unsupported"
              ? "unsupported"
              : "failed",
      };
    case "elapsed":
      return state.status === "recording" && !state.paused
        ? { ...state, elapsedMs: event.elapsedMs }
        : state;
    case "level":
      return state.status === "recording" && !state.paused
        ? {
            ...state,
            levels: [...state.levels, clampLevel(event.level)].slice(
              -VOICE_LEVEL_BARS,
            ),
          }
        : state;
    case "pause":
      return state.status === "recording" && !state.paused
        ? { ...state, paused: true }
        : state;
    case "resume":
      return state.status === "recording" && state.paused
        ? { ...state, paused: false }
        : state;
    case "stopped":
      if (state.status !== "recording") return state;
      return event.recording.durationMs < VOICE_MIN_MS
        ? { status: "too-short" }
        : { status: "review", recording: event.recording };
    case "discard":
      return state.status === "idle" ? state : { status: "idle" };
    case "failed":
      return state.status === "recording" || state.status === "requesting"
        ? { status: "failed" }
        : state;
  }
}

function clampLevel(level: number): number {
  return Number.isFinite(level) ? Math.min(1, Math.max(0, level)) : 0;
}

/** The waveform peaks of an array of samples in [-1, 1] or [0, 1]: `count` bars, each the peak of its slice, scaled to 0..255. */
export function waveformFromSamples(
  samples: ArrayLike<number>,
  count: number = VOICE_WAVEFORM_PEAKS,
): number[] {
  const bars = Math.min(Math.max(1, Math.floor(count)), VOICE_WAVEFORM_PEAKS);
  const peaks = new Array<number>(bars).fill(0);
  if (samples.length === 0) return peaks;
  const slice = samples.length / bars;
  let top = 0;
  for (let bar = 0; bar < bars; bar++) {
    const from = Math.floor(bar * slice);
    const to = Math.max(from + 1, Math.floor((bar + 1) * slice));
    let peak = 0;
    for (let at = from; at < to && at < samples.length; at++) {
      peak = Math.max(peak, Math.abs(samples[at] ?? 0));
    }
    peaks[bar] = peak;
    top = Math.max(top, peak);
  }
  // Scale so the loudest bar fills the range; a silent clip stays flat at zero.
  return peaks.map((peak) => (top === 0 ? 0 : Math.round((peak / top) * 255)));
}

/** Bars to draw for a waveform: the stored peaks resampled to `count`, or an even flat row when there are none. */
export function barsFor(
  waveform: readonly number[] | null | undefined,
  count: number,
): number[] {
  if (!waveform || waveform.length === 0)
    return new Array<number>(count).fill(0.2);
  return Array.from({ length: count }, (_, bar) => {
    const at = Math.min(
      waveform.length - 1,
      Math.floor((bar * waveform.length) / count),
    );
    return Math.max(0.08, (waveform[at] ?? 0) / 255);
  });
}

/** The playback speeds in the order the speed control cycles them. */
export const VOICE_SPEEDS = [1, 1.5, 2] as const;

export function nextVoiceSpeed(current: number): number {
  const index = VOICE_SPEEDS.indexOf(current as (typeof VOICE_SPEEDS)[number]);
  return VOICE_SPEEDS[(index + 1) % VOICE_SPEEDS.length] ?? 1;
}

/** `m:ss` for a duration in milliseconds (a negative or unknown value reads as 0:00). */
export function formatVoiceClock(ms: number | null | undefined): string {
  const total =
    Number.isFinite(ms) && ms !== null && ms !== undefined && ms > 0
      ? Math.floor(ms / 1000)
      : 0;
  const minutes = Math.floor(total / 60);
  const seconds = total % 60;
  return `${minutes}:${String(seconds).padStart(2, "0")}`;
}

/** The fraction of the way through a clip (0 to 1) for an elapsed time, clamped. */
export function playbackFraction(
  elapsedMs: number,
  durationMs: number,
): number {
  if (!(durationMs > 0)) return 0;
  return Math.min(1, Math.max(0, elapsedMs / durationMs));
}

/** The seek target in seconds for a key press: `delta` seconds from `current`, kept inside the clip. */
export function seekTarget(
  current: number,
  delta: number,
  duration: number,
): number {
  const limit =
    Number.isFinite(duration) && duration > 0
      ? duration
      : Number.POSITIVE_INFINITY;
  return Math.min(limit, Math.max(0, current + delta));
}

/** Whether an attachment is a voice message's audio (any audio file shows the voice player). */
export function isAudioAttachment(
  attachment: Pick<ChatAttachment, "contentType">,
): boolean {
  return attachment.contentType.toLowerCase().startsWith("audio/");
}

/** The voice facts of a recording that is about to be uploaded, kept beside its File (never sent as a field of the message). */
export interface VoiceUploadFacts {
  readonly durationMs: number;
  readonly waveform: readonly number[];
}

const voiceFacts = new WeakMap<File, VoiceUploadFacts>();

/** Marks a File as a voice message's audio, so the send and the upload know to send the voice fields. */
export function markVoiceFile(file: File, facts: VoiceUploadFacts): File {
  voiceFacts.set(file, facts);
  return file;
}

export function voiceFactsOf(file: File): VoiceUploadFacts | null {
  return voiceFacts.get(file) ?? null;
}

/** A recording as the File the upload sends: the file name carries the recorder's extension. */
export function recordingFile(
  recording: VoiceRecording,
  now: Date = new Date(),
): File {
  const extension = recording.blob.type.includes("mp4")
    ? "m4a"
    : recording.blob.type.includes("ogg")
      ? "ogg"
      : "webm";
  const stamp = now.toISOString().slice(0, 19).replace(/[-:T]/g, "");
  const file = new File([recording.blob], `voice-${stamp}.${extension}`, {
    type: recording.blob.type || "audio/webm",
  });
  return markVoiceFile(file, {
    durationMs: Math.round(recording.durationMs),
    waveform: [...recording.waveform].slice(0, VOICE_WAVEFORM_PEAKS),
  });
}
