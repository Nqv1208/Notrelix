/** The LiveKit side of the media seam. Loaded on demand by `connectChatCallMedia`; nothing else imports `livekit-client`. */
import { Room, RoomEvent, Track } from "livekit-client";
import {
  CALL_RECONNECT_ATTEMPTS,
  CALL_RECONNECT_LIMIT_MS,
  type ChatCallDeviceFailure,
  type ChatCallMediaOptions,
  type ChatCallMediaSession,
} from "./chat-call-media";

const RETRY_DELAYS_MS = [0, 1_000, 2_000, 4_000, 8_000];

function failureOf(error: unknown): ChatCallDeviceFailure {
  const name = (error as { name?: string } | null)?.name ?? "";
  return name === "NotAllowedError" || name === "SecurityError"
    ? "denied"
    : "unavailable";
}

export async function connectLiveKit(
  options: ChatCallMediaOptions,
): Promise<ChatCallMediaSession> {
  const { audioinput, videoinput, audiooutput } = options.preferred ?? {};
  const room = new Room({
    adaptiveStream: true,
    dynacast: true,
    ...(audioinput ? { audioCaptureDefaults: { deviceId: audioinput } } : {}),
    ...(videoinput ? { videoCaptureDefaults: { deviceId: videoinput } } : {}),
    ...(audiooutput ? { audioOutput: { deviceId: audiooutput } } : {}),
    // The call screen says "try n of 5" and gives up with the server's seat: the SDK default would retry 7 times, silently.
    reconnectPolicy: {
      nextRetryDelayInMs: ({ retryCount, elapsedMs }) => {
        if (
          retryCount >= CALL_RECONNECT_ATTEMPTS ||
          elapsedMs >= CALL_RECONNECT_LIMIT_MS
        )
          return null;
        options.onAttempt?.(retryCount + 1);
        return RETRY_DELAYS_MS[retryCount] ?? 8_000;
      },
    },
  });
  // Remote audio plays through hidden elements; they leave with the call.
  const audioElements = new Set<HTMLMediaElement>();

  room.on(RoomEvent.TrackSubscribed, (track) => {
    if (track.kind === Track.Kind.Video) {
      const el = options.remoteVideo();
      if (el) track.attach(el);
    } else if (track.kind === Track.Kind.Audio) {
      const el = track.attach();
      el.style.display = "none";
      document.body.appendChild(el);
      audioElements.add(el);
    }
  });
  room.on(RoomEvent.TrackUnsubscribed, (track) => {
    track.detach().forEach((el) => {
      if (audioElements.delete(el)) el.remove();
    });
  });
  room.on(RoomEvent.Reconnecting, () => options.onState("reconnecting"));
  room.on(RoomEvent.Reconnected, () => options.onState("connected"));
  room.on(RoomEvent.Disconnected, () => options.onState("disconnected"));

  await room.connect(options.url, options.token);
  options.onState("connected");

  return {
    async setMicrophone(on) {
      try {
        await room.localParticipant.setMicrophoneEnabled(on);
        return null;
      } catch (error) {
        return failureOf(error);
      }
    },
    async setCamera(on) {
      try {
        const publication = await room.localParticipant.setCameraEnabled(on);
        const el = options.localVideo();
        if (on && el && publication?.track) publication.track.attach(el);
        return null;
      } catch (error) {
        return failureOf(error);
      }
    },
    async switchDevice(kind, deviceId) {
      try {
        return (await room.switchActiveDevice(kind, deviceId))
          ? null
          : "unavailable";
      } catch (error) {
        return failureOf(error);
      }
    },
    disconnect() {
      audioElements.forEach((el) => el.remove());
      audioElements.clear();
      void room.disconnect();
    },
  };
}
