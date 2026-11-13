import { act, useState } from "react";
import { beforeAll, describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen, waitFor } from "@notrelix/testing";
import { ChatComposer } from "../web/components/chat-composer";
import { en } from "../web/components/chat.copy";
import type {
  ChatVoiceRecorder,
  VoiceRecorderHandlers,
  VoiceRecording,
} from "../core/api/chat-voice-recorder";
import { voiceFactsOf } from "../core/model/voice-model";

beforeAll(() => {
  URL.createObjectURL = vi.fn(() => "blob:composer-voice");
  URL.revokeObjectURL = vi.fn();
});

/** A recorder that grants at once and finishes with a fixed recording when stopped. */
function fakeRecorder() {
  const recording: VoiceRecording = {
    blob: new Blob(["voice"], { type: "audio/webm" }),
    durationMs: 4000,
    waveform: [5, 250, 120],
  };
  const spies = {
    start: vi.fn(async (_handlers: VoiceRecorderHandlers) => undefined),
    stop: vi.fn(async () => recording),
    cancel: vi.fn(),
  };
  const recorder: ChatVoiceRecorder = {
    isSupported: () => true,
    start: spies.start,
    pause: vi.fn(),
    resume: vi.fn(),
    stop: spies.stop,
    cancel: spies.cancel,
  };
  return { recorder, spies };
}

function Harness({
  onSend,
  recorder,
  voice,
}: {
  onSend: (...args: unknown[]) => void;
  recorder: ChatVoiceRecorder;
  voice?: boolean;
}) {
  const [value, setValue] = useState("");
  return (
    <ChatComposer
      channelName="general"
      value={value}
      disabledReason={null}
      copy={en}
      onChange={setValue}
      onSend={onSend}
      attachments={{ draftKey: "c1", voiceRecorder: recorder, voice }}
    />
  );
}

describe("ChatComposer microphone and send (UC-CHT-I04)", () => {
  it("shows the microphone while the input is empty and the send button once there is text", () => {
    const { recorder } = fakeRecorder();
    render(<Harness onSend={vi.fn()} recorder={recorder} />);

    expect(screen.getByRole("button", { name: en.voice.record })).toBeTruthy();
    expect(screen.queryByRole("button", { name: en.composer.send })).toBeNull();

    fireEvent.change(screen.getByRole("textbox"), {
      target: { value: "hello" },
    });

    expect(screen.queryByRole("button", { name: en.voice.record })).toBeNull();
    expect(screen.getByRole("button", { name: en.composer.send })).toBeTruthy();

    fireEvent.change(screen.getByRole("textbox"), { target: { value: "" } });
    expect(screen.getByRole("button", { name: en.voice.record })).toBeTruthy();
  });

  it("records from the microphone button and sends the recording as a voice message with no text", async () => {
    const { recorder, spies } = fakeRecorder();
    const onSend = vi.fn();
    render(<Harness onSend={onSend} recorder={recorder} />);

    fireEvent.click(screen.getByRole("button", { name: en.voice.record }));
    await waitFor(() => expect(spies.start).toHaveBeenCalledTimes(1));
    fireEvent.click(await screen.findByRole("button", { name: en.voice.stop }));
    fireEvent.click(await screen.findByRole("button", { name: en.voice.send }));

    expect(onSend).toHaveBeenCalledTimes(1);
    const [content, payload, files] = onSend.mock.calls[0] as [
      string,
      unknown,
      File[],
    ];
    expect(content).toBe("");
    expect(payload).toBeUndefined();
    expect(files).toHaveLength(1);
    expect(voiceFactsOf(files[0]!)).toEqual({
      durationMs: 4000,
      waveform: [5, 250, 120],
    });
    expect(
      screen.queryByRole("region", { name: en.voice.regionLabel }),
    ).toBeNull();
  });

  it("has no microphone where a voice message cannot be sent (a thread reply)", () => {
    const { recorder } = fakeRecorder();
    render(<Harness onSend={vi.fn()} recorder={recorder} voice={false} />);

    expect(screen.queryByRole("button", { name: en.voice.record })).toBeNull();
    expect(screen.getByRole("button", { name: en.composer.send })).toBeTruthy();
  });

  it("cancels the recording and closes the panel from the discard control", async () => {
    const { recorder, spies } = fakeRecorder();
    render(<Harness onSend={vi.fn()} recorder={recorder} />);

    fireEvent.click(screen.getByRole("button", { name: en.voice.record }));
    fireEvent.click(
      await screen.findByRole("button", { name: en.voice.cancel }),
    );

    await act(async () => undefined);
    expect(spies.cancel).toHaveBeenCalled();
    expect(screen.getByRole("button", { name: en.voice.record })).toBeTruthy();
  });
});
