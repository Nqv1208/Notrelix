/** The LiveKit side of the media seam. Loaded on demand by `connectChatCallMedia`; nothing else imports `livekit-client`. */
import { Room, RoomEvent, Track } from "livekit-client";
import type {
  ChatCallDeviceFailure,
  ChatCallMediaOptions,
  ChatCallMediaSession,
} from "./chat-call-media";

function failureOf(error: unknown): ChatCallDeviceFailure {
  const name = (error as { name?: string } | null)?.name ?? "";
  return name === "NotAllowedError" || name === "SecurityError"
    ? "denied"
    : "unavailable";
}

export async function connectLiveKit(
  options: ChatCallMediaOptions,
): Promise<ChatCallMediaSession> {
  const room = new Room({ adaptiveStream: true, dynacast: true });
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
    disconnect() {
      audioElements.forEach((el) => el.remove());
      audioElements.clear();
      void room.disconnect();
    },
  };
}
