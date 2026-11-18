/**
 * The seam between a call and its media plane. The call UI never touches an SFU client: it asks a connector for a session.
 * The default connector reads the join token's address: `mock://` (the dev mock backend) gets a session that connects without
 * any network, every other address loads `livekit-client` on demand, so the SFU client never lands in the main Chat bundle.
 */

export type ChatCallMediaState =
  "connected" | "reconnecting" | "disconnected" | "failed";

/** Why a device could not be switched on. `denied` is the browser or the person refusing; `unavailable` is no such device. */
export type ChatCallDeviceFailure = "denied" | "unavailable";

/** The three kinds of device a call uses, named as the browser names them. */
export type ChatCallDeviceKind = "audioinput" | "videoinput" | "audiooutput";

/** The device ids a person chose, per kind; a missing kind uses the system default. Client-side only, never sent to the server. */
export type ChatCallDevicePreference = Partial<
  Record<ChatCallDeviceKind, string>
>;

export interface ChatCallMediaSession {
  /** Switches the microphone on or off; resolves with the failure when the device cannot be used. */
  setMicrophone(on: boolean): Promise<ChatCallDeviceFailure | null>;
  setCamera(on: boolean): Promise<ChatCallDeviceFailure | null>;
  /** Moves the call to another device of that kind; resolves with the failure when the device cannot be used. */
  switchDevice(
    kind: ChatCallDeviceKind,
    deviceId: string,
  ): Promise<ChatCallDeviceFailure | null>;
  /** Puts a person's picture on a tile that mounted after their track arrived (group calls). */
  attachVideo?(personId: string, element: HTMLVideoElement): void;
  disconnect(): void;
}

export interface ChatCallMediaOptions {
  readonly url: string;
  readonly token: string;
  /** The tile elements, read when a track arrives so a remount does not lose the picture. */
  readonly remoteVideo: () => HTMLVideoElement | null;
  readonly localVideo: () => HTMLVideoElement | null;
  /** A group call's tile element for one person (their id is the media identity); without it a video lands on `remoteVideo`. */
  readonly participantVideo?: (personId: string) => HTMLVideoElement | null;
  /** The people the room hears speaking now (ids), loudest first; sent on every change. */
  readonly onSpeakers?: (personIds: readonly string[]) => void;
  readonly onState: (state: ChatCallMediaState) => void;
  /** The devices this browser remembers; a device that is gone falls back to the default. */
  readonly preferred?: ChatCallDevicePreference;
}

export type ChatCallMediaConnector = (
  options: ChatCallMediaOptions,
) => Promise<ChatCallMediaSession>;

/** A session that connects at once and carries no media: the mock backend's join token (`mock://`). */
function connectMock(
  options: ChatCallMediaOptions,
): Promise<ChatCallMediaSession> {
  queueMicrotask(() => options.onState("connected"));
  return Promise.resolve({
    setMicrophone: () => Promise.resolve(null),
    setCamera: () => Promise.resolve(null),
    switchDevice: () => Promise.resolve(null),
    disconnect: () => options.onState("disconnected"),
  });
}

export const connectChatCallMedia: ChatCallMediaConnector = async (options) => {
  if (options.url.startsWith("mock://")) return connectMock(options);
  const { connectLiveKit } = await import("./livekit-connector");
  return connectLiveKit(options);
};
