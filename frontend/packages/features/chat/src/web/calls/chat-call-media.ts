/**
 * The seam between a call and its media plane. The call UI never touches an SFU client: it asks a connector for a session.
 * The default connector reads the join token's address: `mock://` (the dev mock backend) gets a session that connects without
 * any network, every other address loads `livekit-client` on demand, so the SFU client never lands in the main Chat bundle.
 */

export type ChatCallMediaState =
  "connected" | "reconnecting" | "disconnected" | "failed";

/** Why a device could not be switched on. `denied` is the browser or the person refusing; `unavailable` is no such device. */
export type ChatCallDeviceFailure = "denied" | "unavailable";

export interface ChatCallMediaSession {
  /** Switches the microphone on or off; resolves with the failure when the device cannot be used. */
  setMicrophone(on: boolean): Promise<ChatCallDeviceFailure | null>;
  setCamera(on: boolean): Promise<ChatCallDeviceFailure | null>;
  disconnect(): void;
}

export interface ChatCallMediaOptions {
  readonly url: string;
  readonly token: string;
  /** The tile elements, read when a track arrives so a remount does not lose the picture. */
  readonly remoteVideo: () => HTMLVideoElement | null;
  readonly localVideo: () => HTMLVideoElement | null;
  readonly onState: (state: ChatCallMediaState) => void;
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
    disconnect: () => options.onState("disconnected"),
  });
}

export const connectChatCallMedia: ChatCallMediaConnector = async (options) => {
  if (options.url.startsWith("mock://")) return connectMock(options);
  const { connectLiveKit } = await import("./livekit-connector");
  return connectLiveKit(options);
};
