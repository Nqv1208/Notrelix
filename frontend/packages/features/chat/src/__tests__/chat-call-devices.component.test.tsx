import { act, fireEvent, render, screen, waitFor } from "@notrelix/testing";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { ChatCallStage } from "../web/calls/chat-call-surfaces";
import {
  readChatCallDevicePreference,
  writeChatCallDevicePreference,
} from "../web/calls/chat-call-devices";
import { en, vi as viCopy } from "../web/components/chat.copy";
import type { ChatCall } from "../core/types/call";
import type {
  ChatCallsController,
  ChatCurrentCall,
} from "../web/calls/use-chat-calls";

const AT = "2026-11-19T04:00:00.000Z";

type Device = { deviceId: string; kind: MediaDeviceKind; label: string };
const mic = (id: string, label: string): Device => ({
  deviceId: id,
  kind: "audioinput",
  label,
});
const speaker = (id: string, label: string): Device => ({
  deviceId: id,
  kind: "audiooutput",
  label,
});
const camera = (id: string, label: string): Device => ({
  deviceId: id,
  kind: "videoinput",
  label,
});

const BUILT_IN_MIC = mic("mic-1", "MacBook Pro Microphone");
const AIRPODS_MIC = mic("mic-2", "AirPods Pro");
const AIRPODS_OUT = speaker("out-2", "AirPods Pro speaker");
const BUILT_IN_OUT = speaker("out-1", "MacBook Pro Speakers");
const FACE_TIME = camera("cam-1", "FaceTime HD Camera");

/** A fake `navigator.mediaDevices`: the list the browser would answer with, and the plug and unplug event. */
function fakeBrowser(
  initial: Device[],
  permission: PermissionState = "granted",
) {
  const target = new EventTarget();
  let devices = initial;
  let gate: Promise<void> | null = null;
  const enumerateDevices = vi.fn(async () => {
    if (gate) await gate;
    return devices as MediaDeviceInfo[];
  });
  Object.defineProperty(navigator, "mediaDevices", {
    configurable: true,
    value: {
      enumerateDevices,
      addEventListener: target.addEventListener.bind(target),
      removeEventListener: target.removeEventListener.bind(target),
    },
  });
  Object.defineProperty(navigator, "permissions", {
    configurable: true,
    value: { query: vi.fn(async () => ({ state: permission })) },
  });
  return {
    enumerateDevices,
    /** The browser answers only once `release` is called. */
    hold() {
      let release!: () => void;
      gate = new Promise<void>((resolve) => (release = resolve));
      return () => {
        gate = null;
        release();
      };
    },
    change(next: Device[]) {
      devices = next;
      target.dispatchEvent(new Event("devicechange"));
    },
    set(next: Device[]) {
      devices = next;
    },
  };
}

function activeCall(kind: "voice" | "video"): ChatCall {
  return {
    id: "call-1",
    spaceId: "space-1",
    kind,
    state: "active",
    endReason: null,
    initiatorId: "quan",
    startedAt: AT,
    answeredAt: AT,
    endedAt: null,
    region: "ap-southeast-1",
    version: 2,
    participants: [
      {
        personId: "me",
        joinedAt: AT,
        leftAt: null,
        audioOn: true,
        videoOn: kind === "video",
        sharingScreen: false,
      },
    ],
  };
}

function held(over: Partial<ChatCurrentCall> = {}): ChatCurrentCall {
  return {
    call: activeCall("voice"),
    media: "connected",
    audioOn: true,
    videoOn: false,
    deviceNotice: null,
    devices: {},
    ...over,
  };
}

function controller(
  over: Partial<ChatCallsController> = {},
): ChatCallsController {
  return {
    enabled: true,
    current: null,
    incoming: [],
    error: null,
    start: vi.fn(),
    answer: vi.fn(),
    decline: vi.fn(),
    hangUp: vi.fn(),
    toggleMicrophone: vi.fn(),
    toggleCamera: vi.fn(),
    switchDevice: vi.fn(async () => null),
    retryConnect: vi.fn(),
    dismiss: vi.fn(),
    remoteVideoRef: vi.fn(),
    localVideoRef: vi.fn(),
    ...over,
  };
}

const NAMES: Record<string, string> = { me: "Thu Hà", quan: "Trần Minh Quân" };

function stage(c: ChatCallsController, h: ChatCurrentCall, copy = en) {
  return (
    <ChatCallStage
      controller={c}
      held={h}
      visible
      onMinimize={vi.fn()}
      viewerId="me"
      nameOf={(id) => NAMES[id] ?? "?"}
      peerOf={() => "quan"}
      copy={copy}
    />
  );
}

async function openSettings(copy = en) {
  fireEvent.click(screen.getByRole("button", { name: copy.calls.devicesOpen }));
  return screen.findByTestId("chat-call-devices");
}

/** Opens a Radix select's list the way the keyboard does, then picks an option by its name. */
function choose(field: HTMLElement, option: string) {
  fireEvent.keyDown(field, { key: "ArrowDown" });
  fireEvent.click(screen.getByRole("option", { name: option }));
}

beforeEach(() => {
  Object.defineProperty(HTMLMediaElement.prototype, "setSinkId", {
    configurable: true,
    value: vi.fn(async () => undefined),
  });
});

afterEach(() => {
  Reflect.deleteProperty(HTMLMediaElement.prototype, "setSinkId");
  Reflect.deleteProperty(navigator, "mediaDevices");
  Reflect.deleteProperty(navigator, "permissions");
});

describe("the call device picker: choosing", () => {
  it("lists the microphones and speakers, system default first, and moves the call to the one picked", async () => {
    fakeBrowser([BUILT_IN_MIC, AIRPODS_MIC, BUILT_IN_OUT, AIRPODS_OUT]);
    const c = controller();
    render(stage(c, held()));
    const dialog = await openSettings();
    const micField = await screen.findByRole("combobox", {
      name: en.calls.microphone,
    });
    expect(dialog.textContent).toContain(en.calls.devicesTitle);
    // Nothing chosen yet: both fields show the system default.
    expect(micField.textContent).toContain(en.calls.deviceDefault);

    fireEvent.keyDown(micField, { key: "ArrowDown" });
    expect(screen.getAllByRole("option").map((o) => o.textContent)).toEqual([
      en.calls.deviceDefault,
      "MacBook Pro Microphone",
      "AirPods Pro",
    ]);
    fireEvent.click(screen.getByRole("option", { name: "AirPods Pro" }));
    expect(c.switchDevice).toHaveBeenCalledWith("audioinput", "mic-2");

    choose(
      screen.getByRole("combobox", { name: en.calls.speaker }),
      "MacBook Pro Speakers",
    );
    expect(c.switchDevice).toHaveBeenCalledWith("audiooutput", "out-1");
  });

  it("shows the device in use and goes back to the system default when that is picked", async () => {
    fakeBrowser([BUILT_IN_MIC, AIRPODS_MIC, BUILT_IN_OUT]);
    const c = controller();
    render(stage(c, held({ devices: { audioinput: "mic-2" } })));
    await openSettings();
    const micField = await screen.findByRole("combobox", {
      name: en.calls.microphone,
    });
    expect(micField.textContent).toContain("AirPods Pro");
    choose(micField, en.calls.deviceDefault);
    expect(c.switchDevice).toHaveBeenCalledWith("audioinput", null);
  });

  it("offers the camera on a Video tab in a video call only", async () => {
    fakeBrowser([BUILT_IN_MIC, FACE_TIME, camera("cam-2", "Logitech Brio")]);
    const voice = controller();
    const { unmount } = render(stage(voice, held()));
    await openSettings();
    await screen.findByRole("combobox", { name: en.calls.microphone });
    expect(
      screen.queryByRole("tab", { name: en.calls.devicesVideoTab }),
    ).toBeNull();
    unmount();

    const video = controller();
    render(stage(video, held({ call: activeCall("video"), videoOn: true })));
    await openSettings();
    fireEvent.mouseDown(
      await screen.findByRole("tab", { name: en.calls.devicesVideoTab }),
    );
    const camField = await screen.findByRole("combobox", {
      name: en.calls.camera,
    });
    choose(camField, "Logitech Brio");
    expect(video.switchDevice).toHaveBeenCalledWith("videoinput", "cam-2");
  });

  it("does not offer a speaker where the browser cannot choose one", async () => {
    Reflect.deleteProperty(HTMLMediaElement.prototype, "setSinkId");
    fakeBrowser([BUILT_IN_MIC, BUILT_IN_OUT]);
    render(stage(controller(), held()));
    await openSettings();
    await screen.findByRole("combobox", { name: en.calls.microphone });
    expect(
      screen.queryByRole("combobox", { name: en.calls.speaker }),
    ).toBeNull();
  });

  it("says so, and keeps the dialog, when the call cannot take the device", async () => {
    fakeBrowser([BUILT_IN_MIC, AIRPODS_MIC]);
    const c = controller({
      switchDevice: vi.fn(async () => "unavailable" as const),
    });
    render(stage(c, held()));
    await openSettings();
    choose(
      await screen.findByRole("combobox", { name: en.calls.microphone }),
      "AirPods Pro",
    );
    expect((await screen.findByRole("alert")).textContent).toContain(
      en.calls.deviceSwitchFailed,
    );
  });

  it("speaks Vietnamese", async () => {
    fakeBrowser([BUILT_IN_MIC]);
    render(stage(controller(), held(), viCopy));
    await openSettings(viCopy);
    expect(
      await screen.findByRole("combobox", { name: viCopy.calls.microphone }),
    ).toBeTruthy();
    expect(screen.getByText(viCopy.calls.devicesTitle)).toBeTruthy();
  });
});

describe("the call device picker: the states around the lists", () => {
  it("shows a loading line and skeleton fields until the browser answers", async () => {
    const browser = fakeBrowser([BUILT_IN_MIC]);
    const release = browser.hold();
    render(stage(controller(), held()));
    await openSettings();
    expect(
      (await screen.findByText(en.calls.devicesLoading)).getAttribute("role"),
    ).toBe("status");
    expect(screen.queryByRole("combobox")).toBeNull();
    await act(async () => release());
    expect(
      await screen.findByRole("combobox", { name: en.calls.microphone }),
    ).toBeTruthy();
    expect(screen.queryByText(en.calls.devicesLoading)).toBeNull();
  });

  it("says no microphone was found, disables the field, and finds one after Check again", async () => {
    const browser = fakeBrowser([BUILT_IN_OUT]);
    render(stage(controller(), held()));
    await openSettings();
    expect(await screen.findByText(en.calls.noMicrophone)).toBeTruthy();
    const micField = screen.getByRole("combobox", {
      name: en.calls.microphone,
    });
    expect((micField as HTMLButtonElement).disabled).toBe(true);
    expect(micField.textContent).toContain(en.calls.deviceNone);

    browser.set([BUILT_IN_MIC, BUILT_IN_OUT]);
    fireEvent.click(screen.getByRole("button", { name: en.calls.recheck }));
    await waitFor(() =>
      expect(screen.queryByText(en.calls.noMicrophone)).toBeNull(),
    );
    expect(
      (
        screen.getByRole("combobox", {
          name: en.calls.microphone,
        }) as HTMLButtonElement
      ).disabled,
    ).toBe(false);
    expect(screen.queryByRole("button", { name: en.calls.recheck })).toBeNull();
  });

  it("says no camera was found on the Video tab of a video call", async () => {
    fakeBrowser([BUILT_IN_MIC, BUILT_IN_OUT]);
    render(stage(controller(), held({ call: activeCall("video") })));
    await openSettings();
    fireEvent.mouseDown(
      await screen.findByRole("tab", { name: en.calls.devicesVideoTab }),
    );
    expect(await screen.findByText(en.calls.noCamera)).toBeTruthy();
    expect(
      (
        screen.getByRole("combobox", {
          name: en.calls.camera,
        }) as HTMLButtonElement
      ).disabled,
    ).toBe(true);
  });

  it("explains how to allow a blocked microphone and camera, with a reload button", async () => {
    fakeBrowser([], "denied");
    render(stage(controller(), held()));
    await openSettings();
    const alert = await screen.findByRole("alert");
    expect(alert.textContent).toContain(en.calls.blockedTitle);
    expect(alert.textContent).toContain(en.calls.blockedStep1);
    expect(alert.textContent).toContain(en.calls.blockedStep3);
    expect(
      screen.getByRole("button", { name: en.calls.reloadPage }),
    ).toBeTruthy();
    const micField = screen.getByRole("combobox", {
      name: en.calls.microphone,
    });
    expect((micField as HTMLButtonElement).disabled).toBe(true);
    expect(micField.textContent).toContain(en.calls.deviceNotAllowed);
  });
});

describe("the call device picker: a device unplugged during the call", () => {
  it("falls back to the system default, says which device left, and clears the line when another is chosen", async () => {
    const browser = fakeBrowser([BUILT_IN_MIC, AIRPODS_MIC, BUILT_IN_OUT]);
    const c = controller();
    render(stage(c, held({ devices: { audioinput: "mic-2" } })));
    // The first list arrives once the room is live.
    await waitFor(() => expect(browser.enumerateDevices).toHaveBeenCalled());
    await act(async () => undefined);

    await act(async () => browser.change([BUILT_IN_MIC, BUILT_IN_OUT]));
    expect(c.switchDevice).toHaveBeenCalledWith("audioinput", null, false);
    const line = await screen.findByText(
      "AirPods Pro was unplugged. The call now uses the system default.",
    );
    expect(line.getAttribute("role")).toBe("status");

    await openSettings();
    choose(
      await screen.findByRole("combobox", { name: en.calls.microphone }),
      "MacBook Pro Microphone",
    );
    await waitFor(() => expect(screen.queryByText(/was unplugged/)).toBeNull());
  });

  it("stays quiet when a device that is not in use leaves", async () => {
    const browser = fakeBrowser([BUILT_IN_MIC, AIRPODS_MIC, BUILT_IN_OUT]);
    const c = controller();
    render(stage(c, held({ devices: { audioinput: "mic-1" } })));
    await waitFor(() => expect(browser.enumerateDevices).toHaveBeenCalled());
    await act(async () => undefined);
    await act(async () => browser.change([BUILT_IN_MIC, BUILT_IN_OUT]));
    expect(c.switchDevice).not.toHaveBeenCalled();
    expect(screen.queryByText(/was unplugged/)).toBeNull();
  });
});

function memoryStore() {
  const data = new Map<string, string>();
  return {
    data,
    getItem: (key: string) => data.get(key) ?? null,
    setItem: (key: string, value: string) => void data.set(key, value),
  };
}

describe("the remembered devices", () => {
  it("keeps the choice per browser and reads it back", () => {
    const store = memoryStore();
    writeChatCallDevicePreference(store, "audioinput", "mic-2");
    writeChatCallDevicePreference(store, "videoinput", "cam-1");
    expect(readChatCallDevicePreference(store)).toEqual({
      audioinput: "mic-2",
      videoinput: "cam-1",
    });
    writeChatCallDevicePreference(store, "audioinput", null);
    expect(readChatCallDevicePreference(store)).toEqual({
      videoinput: "cam-1",
    });
  });

  it("ignores stored text that is not a choice", () => {
    const store = memoryStore();
    store.data.set(
      "notrelix.chat.call-devices",
      JSON.stringify({ audioinput: 7, audiooutput: "", videoinput: "cam-1" }),
    );
    expect(readChatCallDevicePreference(store)).toEqual({
      videoinput: "cam-1",
    });
    store.data.set("notrelix.chat.call-devices", "{not json");
    expect(readChatCallDevicePreference(store)).toEqual({});
  });

  it("works without storage and when the storage throws", () => {
    expect(readChatCallDevicePreference(undefined)).toEqual({});
    expect(() =>
      writeChatCallDevicePreference(undefined, "audioinput", "mic-1"),
    ).not.toThrow();
    const blocked = {
      getItem: () => {
        throw new Error("blocked");
      },
      setItem: () => {
        throw new Error("blocked");
      },
    };
    expect(readChatCallDevicePreference(blocked)).toEqual({});
    expect(() =>
      writeChatCallDevicePreference(blocked, "audioinput", "mic-1"),
    ).not.toThrow();
  });
});
