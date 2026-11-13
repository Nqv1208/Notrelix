/**
 * The port the composer records a voice message through (UC-CHT-I04). The browser adapter wraps MediaRecorder and the
 * microphone; tests pass a fake. Nothing else in the feature touches the microphone or MediaRecorder.
 */

export interface VoiceRecording {
  /** The encoded audio, as the recorder produced it (Opus in WebM, or MP4 where WebM is not recorded). */
  readonly blob: Blob;
  /** The length the recorder measured, in milliseconds. A hint for the server, never the policy. */
  readonly durationMs: number;
  /** Up to 64 peaks, each 0 to 255, for the waveform bars. */
  readonly waveform: readonly number[];
}

export interface VoiceRecorderHandlers {
  /** A live input level from 0 to 1, a few times a second. */
  readonly onLevel: (level: number) => void;
  /** The elapsed recording time in milliseconds, a few times a second (paused time does not count). */
  readonly onElapsed: (elapsedMs: number) => void;
}

/** Why a recording could not start or finish. The reason decides the copy the composer shows. */
export type VoiceRecorderFailure = "denied" | "unsupported" | "failed";

export class ChatVoiceRecorderError extends Error {
  constructor(readonly reason: VoiceRecorderFailure) {
    super(`voice recorder ${reason}`);
    this.name = "ChatVoiceRecorderError";
  }
}

export interface ChatVoiceRecorder {
  /** False when the host cannot record at all (no MediaRecorder, no microphone API). */
  isSupported(): boolean;
  /**
   * Asks for the microphone and starts. Resolves once recording runs; rejects with ChatVoiceRecorderError ("denied" when the person
   * refused, "unsupported" when the browser cannot, "failed" otherwise). `maxMs` stops the recording by itself at that length.
   */
  start(handlers: VoiceRecorderHandlers, maxMs: number): Promise<void>;
  pause(): void;
  resume(): void;
  /** Stops and resolves with the recording. */
  stop(): Promise<VoiceRecording>;
  /** Stops and throws the recording away. */
  cancel(): void;
}
