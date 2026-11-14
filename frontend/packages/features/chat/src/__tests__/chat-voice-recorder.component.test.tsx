import { act } from "react";
import { beforeAll, describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen, waitFor } from "@notrelix/testing";
import { VoiceRecorderPanel } from "../web/components/chat-voice-recorder";
import { en } from "../web/components/chat.copy";
import {
  ChatVoiceRecorderError,
  type ChatVoiceRecorder,
  type VoiceRecorderHandlers,
  type VoiceRecording,
} from "../core/api/chat-voice-recorder";
import { voiceFactsOf } from "../core/model/voice-model";

// jsdom has no object URLs; the panel previews the recording through one.
beforeAll(() => {
  URL.createObjectURL = vi.fn(() => "blob:voice-test");
  URL.revokeObjectURL = vi.fn();
});

/** A fake recorder: the test drives the handlers and resolves the start and stop. */
function fakeRecorder(
  options: { supported?: boolean; startError?: ChatVoiceRecorderError } = {},
) {
  let handlers: VoiceRecorderHandlers | null = null;
  let startResolve: (() => void) | null = null;
  let startReject: ((error: unknown) => void) | null = null;
  let stopResolve: ((recording: VoiceRecording) => void) | null = null;
  const spies = {
    isSupported: vi.fn(() => options.supported ?? true),
    start: vi.fn(
      (next: VoiceRecorderHandlers) =>
        new Promise<void>((resolve, reject) => {
          handlers = next;
          if (options.startError) {
            reject(options.startError);
            return;
          }
          startResolve = resolve;
          startReject = reject;
        }),
    ),
    pause: vi.fn(),
    resume: vi.fn(),
    stop: vi.fn(
      () =>
        new Promise<VoiceRecording>((resolve) => {
          stopResolve = resolve;
        }),
    ),
    cancel: vi.fn(),
  };
  return {
    recorder: spies as unknown as ChatVoiceRecorder,
    spies,
    grant: () => act(() => startResolve?.()),
    refuse: (error: unknown) => act(() => startReject?.(error)),
    tick: (elapsedMs: number) => act(() => handlers?.onElapsed(elapsedMs)),
    level: (value: number) => act(() => handlers?.onLevel(value)),
    finish: (recording: VoiceRecording) => act(() => stopResolve?.(recording)),
  };
}

const RECORDING: VoiceRecording = {
  blob: new Blob(["audio"], { type: "audio/webm" }),
  durationMs: 3200,
  waveform: [10, 200, 90],
};

function renderPanel(
  recorder: ChatVoiceRecorder,
  extra: { maxMs?: number } = {},
) {
  const onSend = vi.fn();
  const onClose = vi.fn();
  render(
    <VoiceRecorderPanel
      recorder={recorder}
      copy={en}
      onSend={onSend}
      onClose={onClose}
      {...extra}
    />,
  );
  return { onSend, onClose };
}

describe("VoiceRecorderPanel (UC-CHT-I04)", () => {
  it("waits for the microphone, then records with the elapsed time and the live level", async () => {
    const fake = fakeRecorder();
    renderPanel(fake.recorder);

    expect(screen.getByText(en.voice.requesting)).toBeTruthy();
    await waitFor(() => expect(fake.spies.start).toHaveBeenCalledTimes(1));
    fake.grant();

    expect(
      await screen.findByRole("img", { name: en.voice.level }),
    ).toBeTruthy();
    fake.tick(65_000);
    fake.level(0.7);
    expect(
      screen.getAllByText("1:05", { exact: false }).length,
    ).toBeGreaterThan(0);
    expect(screen.getByRole("button", { name: en.voice.stop })).toBeTruthy();
  });

  it("pauses and resumes the recording through the port", async () => {
    const fake = fakeRecorder();
    renderPanel(fake.recorder);
    await waitFor(() => expect(fake.spies.start).toHaveBeenCalled());
    fake.grant();

    fireEvent.click(
      await screen.findByRole("button", { name: en.voice.pause }),
    );
    expect(fake.spies.pause).toHaveBeenCalledTimes(1);
    fireEvent.click(screen.getByRole("button", { name: en.voice.resume }));
    expect(fake.spies.resume).toHaveBeenCalledTimes(1);
  });

  it("reviews the stopped recording and sends it as a voice File with its facts", async () => {
    const fake = fakeRecorder();
    const { onSend, onClose } = renderPanel(fake.recorder);
    await waitFor(() => expect(fake.spies.start).toHaveBeenCalled());
    fake.grant();

    fireEvent.click(await screen.findByRole("button", { name: en.voice.stop }));
    fake.finish(RECORDING);

    const send = await screen.findByRole("button", { name: en.voice.send });
    fireEvent.click(send);

    expect(onSend).toHaveBeenCalledTimes(1);
    const file = onSend.mock.calls[0]?.[0] as File;
    expect(file.name.endsWith(".webm")).toBe(true);
    expect(voiceFactsOf(file)).toEqual({
      durationMs: 3200,
      waveform: [10, 200, 90],
    });
    expect(onClose).toHaveBeenCalled();
  });

  it("stops by itself at the limit and moves to review", async () => {
    const fake = fakeRecorder();
    renderPanel(fake.recorder, { maxMs: 5000 });
    await waitFor(() => expect(fake.spies.start).toHaveBeenCalled());
    fake.grant();
    await screen.findByRole("img", { name: en.voice.level });

    fake.tick(5000);

    await waitFor(() => expect(fake.spies.stop).toHaveBeenCalledTimes(1));
  });

  it("discards a recording: the port is cancelled and the panel closes", async () => {
    const fake = fakeRecorder();
    const { onClose, onSend } = renderPanel(fake.recorder);
    await waitFor(() => expect(fake.spies.start).toHaveBeenCalled());
    fake.grant();

    fireEvent.click(
      await screen.findByRole("button", { name: en.voice.cancel }),
    );

    expect(fake.spies.cancel).toHaveBeenCalled();
    expect(onClose).toHaveBeenCalled();
    expect(onSend).not.toHaveBeenCalled();
  });

  it("explains a denied microphone and retries on request", async () => {
    const fake = fakeRecorder({
      startError: new ChatVoiceRecorderError("denied"),
    });
    renderPanel(fake.recorder);

    expect(await screen.findByRole("alert")).toBeTruthy();
    expect(screen.getByText(en.voice.denied)).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: en.voice.tryAgain }));
    await waitFor(() => expect(fake.spies.start).toHaveBeenCalledTimes(2));
  });

  it("says the browser cannot record when the port is unsupported", async () => {
    const fake = fakeRecorder({ supported: false });
    const { onClose } = renderPanel(fake.recorder);

    expect(await screen.findByText(en.voice.unsupported)).toBeTruthy();
    expect(fake.spies.start).not.toHaveBeenCalled();
    expect(onClose).not.toHaveBeenCalled();
  });

  it("offers a retry when recording fails to start", async () => {
    const fake = fakeRecorder({
      startError: new ChatVoiceRecorderError("failed"),
    });
    renderPanel(fake.recorder);

    expect(await screen.findByText(en.voice.failed)).toBeTruthy();
  });

  it("does not send a recording shorter than one second", async () => {
    const fake = fakeRecorder();
    const { onSend } = renderPanel(fake.recorder);
    await waitFor(() => expect(fake.spies.start).toHaveBeenCalled());
    fake.grant();

    fireEvent.click(await screen.findByRole("button", { name: en.voice.stop }));
    fake.finish({ ...RECORDING, durationMs: 400 });

    expect(await screen.findByText(en.voice.tooShort)).toBeTruthy();
    expect(screen.queryByRole("button", { name: en.voice.send })).toBeNull();
    expect(onSend).not.toHaveBeenCalled();
  });
});
