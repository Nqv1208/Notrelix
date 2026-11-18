/**
 * The devices of a call, as the browser lists them: microphone, camera and speaker. A person's choice is a per-browser
 * preference kept in the host's key-value storage (optional: without it, or when it throws, the choice lasts one call); the
 * ids never leave the browser. `useChatCallDevices` lists the devices while a call runs and tells the caller when the device
 * in use is unplugged.
 */
import { useCallback, useEffect, useRef, useState } from "react";
import type { KeyValueStorage } from "@notrelix/platform";
import type {
  ChatCallDeviceKind,
  ChatCallDevicePreference,
} from "./chat-call-media";

export const CHAT_CALL_DEVICE_KINDS: readonly ChatCallDeviceKind[] = [
  "audioinput",
  "videoinput",
  "audiooutput",
];

const STORAGE_KEY = "notrelix.chat.call-devices";

/** What the call needs of the host's storage: the web runtime's key-value port satisfies it. */
export type ChatCallDeviceStore = Pick<KeyValueStorage, "getItem" | "setItem">;

export function readChatCallDevicePreference(
  store: ChatCallDeviceStore | undefined,
): ChatCallDevicePreference {
  if (!store) return {};
  try {
    const raw = JSON.parse(store.getItem(STORAGE_KEY) ?? "{}") as Record<
      string,
      unknown
    >;
    const saved: ChatCallDevicePreference = {};
    for (const kind of CHAT_CALL_DEVICE_KINDS) {
      const id = raw[kind];
      if (typeof id === "string" && id) saved[kind] = id;
    }
    return saved;
  } catch {
    return {};
  }
}

/** Remembers one device of a kind; `null` forgets it, so the system default is used again. */
export function writeChatCallDevicePreference(
  store: ChatCallDeviceStore | undefined,
  kind: ChatCallDeviceKind,
  deviceId: string | null,
): void {
  if (!store) return;
  try {
    const next = { ...readChatCallDevicePreference(store) };
    if (deviceId) next[kind] = deviceId;
    else delete next[kind];
    store.setItem(STORAGE_KEY, JSON.stringify(next));
  } catch {
    // Storage is blocked or full: the choice lasts for this call only.
  }
}

/** The id the browser uses for "the system default"; a choice of it is stored as no choice. */
export const CHAT_CALL_DEFAULT_DEVICE = "default";

export interface ChatCallDeviceOption {
  readonly id: string;
  readonly label: string;
}

export type ChatCallDeviceLists = Readonly<
  Record<ChatCallDeviceKind, readonly ChatCallDeviceOption[]>
>;

/** `loading` until the first list arrives; `denied` when the browser refused the microphone or camera. */
export type ChatCallDevicesStatus = "loading" | "ready" | "denied";

const EMPTY_LISTS: ChatCallDeviceLists = {
  audioinput: [],
  videoinput: [],
  audiooutput: [],
};

/** Choosing a speaker needs `setSinkId`, which Safari and Firefox do not have. */
export const supportsSpeakerChoice = (): boolean =>
  typeof HTMLMediaElement !== "undefined" &&
  "setSinkId" in HTMLMediaElement.prototype;

async function permissionDenied(): Promise<boolean> {
  const query = navigator.permissions?.query?.bind(navigator.permissions);
  if (!query) return false;
  for (const name of ["microphone", "camera"]) {
    try {
      const status = await query({ name: name as PermissionName });
      if (status.state === "denied") return true;
    } catch {
      // A browser that does not know this permission name: ask nothing more of it.
    }
  }
  return false;
}

async function listDevices(): Promise<ChatCallDeviceLists> {
  const all = (await navigator.mediaDevices?.enumerateDevices?.()) ?? [];
  const lists: Record<ChatCallDeviceKind, ChatCallDeviceOption[]> = {
    audioinput: [],
    videoinput: [],
    audiooutput: [],
  };
  for (const device of all) {
    // "default" and "communications" are aliases of another entry in the list.
    if (device.deviceId === CHAT_CALL_DEFAULT_DEVICE) continue;
    if (device.deviceId === "communications") continue;
    lists[device.kind].push({ id: device.deviceId, label: device.label });
  }
  return lists;
}

export interface UseChatCallDevicesOptions {
  /** Lists devices only while a call is up. */
  readonly active: boolean;
  /** The ids the call uses now; a kind that is missing uses the system default. */
  readonly inUse: ChatCallDevicePreference;
  /** Called once per device that left while it was in use. */
  readonly onUnplugged: (kind: ChatCallDeviceKind, label: string) => void;
}

export interface ChatCallDevicesState {
  readonly status: ChatCallDevicesStatus;
  readonly lists: ChatCallDeviceLists;
  /** Reads the devices again (the "check again" button). */
  readonly refresh: () => void;
}

export function useChatCallDevices({
  active,
  inUse,
  onUnplugged,
}: UseChatCallDevicesOptions): ChatCallDevicesState {
  const [status, setStatus] = useState<ChatCallDevicesStatus>("loading");
  const [lists, setLists] = useState<ChatCallDeviceLists>(EMPTY_LISTS);
  // Read inside the browser's event handler, which must not be re-registered on every change.
  const latest = useRef({ lists, inUse, onUnplugged });
  latest.current = { lists, inUse, onUnplugged };
  const mounted = useRef(true);
  useEffect(() => {
    mounted.current = true;
    return () => {
      mounted.current = false;
    };
  }, []);

  const read = useCallback(async () => {
    try {
      const [next, denied] = await Promise.all([
        listDevices(),
        permissionDenied(),
      ]);
      if (!mounted.current) return;
      const before = latest.current;
      for (const kind of CHAT_CALL_DEVICE_KINDS) {
        const id = before.inUse[kind];
        if (!id || next[kind].some((d) => d.id === id)) continue;
        const gone = before.lists[kind].find((d) => d.id === id);
        if (gone) before.onUnplugged(kind, gone.label);
      }
      setLists(next);
      setStatus(denied ? "denied" : "ready");
    } catch {
      if (!mounted.current) return;
      setLists(EMPTY_LISTS);
      setStatus("ready");
    }
  }, []);

  useEffect(() => {
    if (!active) return;
    const media = navigator.mediaDevices;
    void read();
    media?.addEventListener?.("devicechange", read);
    return () => media?.removeEventListener?.("devicechange", read);
  }, [active, read]);

  const refresh = useCallback(() => {
    setStatus("loading");
    void read();
  }, [read]);

  return { status, lists, refresh };
}
