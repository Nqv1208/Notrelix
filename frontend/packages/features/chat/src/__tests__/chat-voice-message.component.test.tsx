import { beforeAll, beforeEach, describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen, within } from "@notrelix/testing";
import { VoiceMessagePlayer } from "../web/components/chat-voice-message";
import { en } from "../web/components/chat.copy";
import type { ChatAttachment } from "../core/types/chat";
import type { ChatHooks } from "../web/query/create-chat-hooks";

const ATTACHMENT: ChatAttachment = {
  id: "att-1",
  fileName: "voice.webm",
  sizeBytes: 4000,
  contentType: "audio/webm",
  uploadedBy: "u-1",
  createdAt: "2026-11-13T10:00:00Z",
  downloadPath: "/api/x",
  durationMs: 12_000,
  waveform: [10, 200, 90, 40],
};

/** The player reads its bytes through the authenticated content hook; the test gives it a blob and no network. */
function hooksWith(data: Blob | undefined, isError = false) {
  const refetch = vi.fn();
  const hooks = {
    useAttachmentContent: () => ({ data, isError, refetch }),
  } as unknown as ChatHooks;
  return { hooks, refetch };
}

/** jsdom has no media: give each audio element a paused flag and play/pause that change it the way a browser does. */
function makeAudioPlayable(audio: HTMLAudioElement) {
  let paused = true;
  Object.defineProperty(audio, "paused", {
    configurable: true,
    get: () => paused,
  });
  let time = 0;
  Object.defineProperty(audio, "currentTime", {
    configurable: true,
    get: () => time,
    set: (value: number) => {
      time = value;
    },
  });
  Object.defineProperty(audio, "duration", {
    configurable: true,
    get: () => 12,
  });
  audio.play = vi.fn(() => {
    paused = false;
    audio.dispatchEvent(new Event("play"));
    return Promise.resolve();
  });
  audio.pause = vi.fn(() => {
    paused = true;
    audio.dispatchEvent(new Event("pause"));
  });
}

function renderPlayer(messageId = "m-1", id = "att-1") {
  const { hooks, refetch } = hooksWith(
    new Blob(["voice"], { type: "audio/webm" }),
  );
  const view = render(
    <VoiceMessagePlayer
      attachment={{ ...ATTACHMENT, id }}
      workspaceId="ws-1"
      messageId={messageId}
      hooks={hooks}
      copy={en}
    />,
  );
  return { ...view, refetch };
}

function audioOf(container: HTMLElement): HTMLAudioElement {
  const audio = container.querySelector("audio");
  if (!audio) throw new Error("no audio element");
  return audio;
}

beforeAll(() => {
  URL.createObjectURL = vi.fn(() => "blob:voice-player");
  URL.revokeObjectURL = vi.fn();
});

beforeEach(() => {
  vi.clearAllMocks();
});

describe("VoiceMessagePlayer (UC-CHT-I05)", () => {
  it("plays and pauses from its play control, with the label that says what it does", () => {
    const { container } = renderPlayer();
    const audio = audioOf(container);
    makeAudioPlayable(audio);

    fireEvent.click(screen.getByRole("button", { name: en.voice.play }));
    expect(audio.play).toHaveBeenCalledTimes(1);
    expect(
      screen.getByRole("button", { name: en.voice.pausePlayback }),
    ).toBeTruthy();

    fireEvent.click(
      screen.getByRole("button", { name: en.voice.pausePlayback }),
    );
    expect(audio.pause).toHaveBeenCalledTimes(1);
    expect(screen.getByRole("button", { name: en.voice.play })).toBeTruthy();
  });

  it("shows the waveform as a slider with the elapsed and total time, and the total from the attachment", () => {
    renderPlayer();

    const slider = screen.getByRole("slider", { name: en.voice.seek });
    expect(slider.getAttribute("aria-valuemax")).toBe("12");
    expect(slider.getAttribute("aria-valuetext")).toBe("0:00 of 0:12");
    expect(screen.getByText("0:00 / 0:12")).toBeTruthy();
  });

  it("seeks by the arrow keys in five-second steps and to the ends with Home and End", () => {
    const { container } = renderPlayer();
    const audio = audioOf(container);
    makeAudioPlayable(audio);
    const slider = screen.getByRole("slider", { name: en.voice.seek });

    fireEvent.keyDown(slider, { key: "ArrowRight" });
    expect(audio.currentTime).toBe(5);
    fireEvent.keyDown(slider, { key: "ArrowRight" });
    expect(audio.currentTime).toBe(10);
    fireEvent.keyDown(slider, { key: "ArrowRight" });
    expect(audio.currentTime).toBe(12);
    fireEvent.keyDown(slider, { key: "ArrowLeft" });
    expect(audio.currentTime).toBe(7);
    fireEvent.keyDown(slider, { key: "Home" });
    expect(audio.currentTime).toBe(0);
    fireEvent.keyDown(slider, { key: "End" });
    expect(audio.currentTime).toBe(12);
  });

  it("cycles the speed 1x, 1.5x, 2x and sets the playback rate of the audio", () => {
    const { container } = renderPlayer();
    const audio = audioOf(container);

    const speed = () => screen.getByRole("button", { name: /Speed/ });
    expect(speed().textContent).toBe("1x");
    fireEvent.click(speed());
    expect(speed().textContent).toBe("1.5x");
    expect(audio.playbackRate).toBe(1.5);
    fireEvent.click(speed());
    expect(speed().textContent).toBe("2x");
    expect(audio.playbackRate).toBe(2);
    fireEvent.click(speed());
    expect(speed().textContent).toBe("1x");
  });

  it("plays only one voice message at a time: starting another pauses the one playing", () => {
    const first = renderPlayer("m-1", "att-1");
    const second = renderPlayer("m-2", "att-2");
    const firstAudio = audioOf(first.container);
    const secondAudio = audioOf(second.container);
    makeAudioPlayable(firstAudio);
    makeAudioPlayable(secondAudio);

    fireEvent.click(
      within(first.container).getByRole("button", { name: en.voice.play }),
    );
    expect(firstAudio.play).toHaveBeenCalledTimes(1);

    fireEvent.click(
      within(second.container).getByRole("button", { name: en.voice.play }),
    );

    expect(firstAudio.pause).toHaveBeenCalledTimes(1);
    expect(secondAudio.play).toHaveBeenCalledTimes(1);
  });

  it("gives the retry control when the bytes do not load, and retries the read", () => {
    const refetch = vi.fn();
    const hooks = {
      useAttachmentContent: () => ({ data: undefined, isError: true, refetch }),
    } as unknown as ChatHooks;
    render(
      <VoiceMessagePlayer
        attachment={ATTACHMENT}
        workspaceId="ws-1"
        messageId="m-1"
        hooks={hooks}
        copy={en}
      />,
    );

    expect(screen.getByText(en.voice.loadFailed)).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: en.voice.retry }));
    expect(refetch).toHaveBeenCalledTimes(1);
  });
});
