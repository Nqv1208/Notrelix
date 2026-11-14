import { describe, expect, it } from "vitest";
import {
  VOICE_LEVEL_BARS,
  VOICE_MAX_MS,
  VOICE_MIN_MS,
  barsFor,
  formatVoiceClock,
  isAudioAttachment,
  nextVoiceSpeed,
  playbackFraction,
  recordingFile,
  reduceVoiceRecorder,
  seekTarget,
  voiceFactsOf,
  waveformFromSamples,
  type VoiceRecorderState,
} from "../core/model/voice-model";
import type { VoiceRecording } from "../core/api/chat-voice-recorder";

const recording = (durationMs: number): VoiceRecording => ({
  blob: new Blob(["audio"], { type: "audio/webm;codecs=opus" }),
  durationMs,
  waveform: [0, 255],
});

describe("voice recorder state machine (UC-CHT-I04)", () => {
  it("asks for the microphone on start and records once it is granted", () => {
    let state = reduceVoiceRecorder({ status: "idle" }, { type: "start" });
    expect(state.status).toBe("requesting");
    state = reduceVoiceRecorder(state, { type: "granted" });
    expect(state).toEqual({
      status: "recording",
      elapsedMs: 0,
      levels: [],
      paused: false,
    });
  });

  it("goes to denied, unsupported or failed when the start is refused", () => {
    const requesting: VoiceRecorderState = { status: "requesting" };
    expect(
      reduceVoiceRecorder(requesting, { type: "refused", reason: "denied" })
        .status,
    ).toBe("denied");
    expect(
      reduceVoiceRecorder(requesting, {
        type: "refused",
        reason: "unsupported",
      }).status,
    ).toBe("unsupported");
    expect(
      reduceVoiceRecorder(requesting, { type: "refused", reason: "failed" })
        .status,
    ).toBe("failed");
  });

  it("ignores a refusal that arrives after recording started", () => {
    const recording: VoiceRecorderState = {
      status: "recording",
      elapsedMs: 1200,
      levels: [],
      paused: false,
    };
    expect(
      reduceVoiceRecorder(recording, { type: "refused", reason: "denied" }),
    ).toBe(recording);
  });

  it("moves to review with the recording when it is at least one second long", () => {
    const state = reduceVoiceRecorder(
      { status: "recording", elapsedMs: 4000, levels: [], paused: false },
      { type: "stopped", recording: recording(4000) },
    );
    expect(state.status).toBe("review");
    expect(state.status === "review" && state.recording.durationMs).toBe(4000);
  });

  it("keeps a recording shorter than one second out of review (too short, nothing to send)", () => {
    expect(VOICE_MIN_MS).toBe(1000);
    const state = reduceVoiceRecorder(
      { status: "recording", elapsedMs: 400, levels: [], paused: false },
      { type: "stopped", recording: recording(400) },
    );
    expect(state).toEqual({ status: "too-short" });
  });

  it("pauses and resumes; elapsed time and levels do not move while paused", () => {
    let state: VoiceRecorderState = {
      status: "recording",
      elapsedMs: 2000,
      levels: [],
      paused: false,
    };
    state = reduceVoiceRecorder(state, { type: "pause" });
    state = reduceVoiceRecorder(state, { type: "elapsed", elapsedMs: 9000 });
    state = reduceVoiceRecorder(state, { type: "level", level: 0.9 });
    expect(state).toEqual({
      status: "recording",
      elapsedMs: 2000,
      levels: [],
      paused: true,
    });
    state = reduceVoiceRecorder(state, { type: "resume" });
    state = reduceVoiceRecorder(state, { type: "elapsed", elapsedMs: 2500 });
    expect(state.status === "recording" && state.elapsedMs).toBe(2500);
  });

  it("keeps only the latest level bars and clamps each level to 0..1", () => {
    let state: VoiceRecorderState = {
      status: "recording",
      elapsedMs: 0,
      levels: [],
      paused: false,
    };
    for (let i = 0; i < VOICE_LEVEL_BARS + 5; i++) {
      state = reduceVoiceRecorder(state, {
        type: "level",
        level: i === 0 ? 4 : 0.5,
      });
    }
    expect(state.status === "recording" && state.levels).toHaveLength(
      VOICE_LEVEL_BARS,
    );
    expect(state.status === "recording" && state.levels.includes(4)).toBe(
      false,
    );
  });

  it("discards, and a late tick after the discard changes nothing", () => {
    const idle: VoiceRecorderState = { status: "idle" };
    expect(reduceVoiceRecorder(idle, { type: "discard" })).toBe(idle);
    expect(
      reduceVoiceRecorder(idle, { type: "elapsed", elapsedMs: 5000 }),
    ).toBe(idle);
  });

  it("fails while recording when the recorder breaks", () => {
    expect(
      reduceVoiceRecorder(
        { status: "recording", elapsedMs: 1, levels: [], paused: false },
        { type: "failed" },
      ).status,
    ).toBe("failed");
  });

  it("uses the five-minute limit the server enforces", () => {
    expect(VOICE_MAX_MS).toBe(300_000);
  });
});

describe("waveform and playback helpers", () => {
  it("makes 64 or fewer peaks scaled so the loudest is 255 and silence is flat zero", () => {
    const samples = Array.from({ length: 1000 }, (_, index) =>
      index < 500 ? 0.1 : 0.4,
    );
    const peaks = waveformFromSamples(samples, 10);
    expect(peaks).toHaveLength(10);
    expect(Math.max(...peaks)).toBe(255);
    expect(peaks[0]).toBe(64);
    expect(waveformFromSamples([0, 0, 0], 4)).toEqual([0, 0, 0, 0]);
    expect(waveformFromSamples(samples, 500)).toHaveLength(64);
  });

  it("draws the stored peaks resampled, or flat bars when there are none", () => {
    expect(barsFor([0, 255], 4)).toEqual([0.08, 0.08, 1, 1]);
    expect(barsFor(null, 3)).toEqual([0.2, 0.2, 0.2]);
  });

  it("cycles the speed 1x, 1.5x, 2x and back", () => {
    expect(nextVoiceSpeed(1)).toBe(1.5);
    expect(nextVoiceSpeed(1.5)).toBe(2);
    expect(nextVoiceSpeed(2)).toBe(1);
  });

  it("formats the clock as m:ss and reads nonsense as zero", () => {
    expect(formatVoiceClock(0)).toBe("0:00");
    expect(formatVoiceClock(65_400)).toBe("1:05");
    expect(formatVoiceClock(null)).toBe("0:00");
    expect(formatVoiceClock(Number.NaN)).toBe("0:00");
  });

  it("clamps a seek to the clip and the fraction to 0..1", () => {
    expect(seekTarget(3, 5, 10)).toBe(8);
    expect(seekTarget(8, 5, 10)).toBe(10);
    expect(seekTarget(2, -5, 10)).toBe(0);
    expect(playbackFraction(5000, 10_000)).toBe(0.5);
    expect(playbackFraction(5000, 0)).toBe(0);
  });

  it("recognises an audio attachment by its content type", () => {
    expect(isAudioAttachment({ contentType: "audio/webm" })).toBe(true);
    expect(isAudioAttachment({ contentType: "image/png" })).toBe(false);
  });
});

describe("recording as an upload", () => {
  it("is a File named by its extension that carries the voice facts", () => {
    const file = recordingFile(
      recording(2500),
      new Date("2026-11-13T10:20:30Z"),
    );

    expect(file.name).toBe("voice-20261113102030.webm");
    expect(file.type).toBe("audio/webm;codecs=opus");
    expect(voiceFactsOf(file)).toEqual({
      durationMs: 2500,
      waveform: [0, 255],
    });
  });

  it("names an mp4 recording m4a", () => {
    const mp4 = {
      blob: new Blob(["a"], { type: "audio/mp4" }),
      durationMs: 1500,
      waveform: [],
    };
    expect(recordingFile(mp4).name.endsWith(".m4a")).toBe(true);
  });

  it("has no voice facts for a plain file", () => {
    expect(voiceFactsOf(new File(["x"], "note.txt"))).toBeNull();
  });
});
