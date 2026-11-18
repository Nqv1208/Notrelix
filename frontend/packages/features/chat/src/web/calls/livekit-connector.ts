/** The LiveKit side of the media seam. Loaded on demand by `connectChatCallMedia`; nothing else imports `livekit-client`. */
import { Room, RoomEvent, Track, type RemoteTrack } from "livekit-client";
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
  const { audioinput, videoinput, audiooutput } = options.preferred ?? {};
  const room = new Room({
    adaptiveStream: true,
    dynacast: true,
    ...(audioinput ? { audioCaptureDefaults: { deviceId: audioinput } } : {}),
    ...(videoinput ? { videoCaptureDefaults: { deviceId: videoinput } } : {}),
    ...(audiooutput ? { audioOutput: { deviceId: audiooutput } } : {}),
  });
  // Remote audio plays through hidden elements; they leave with the call.
  const audioElements = new Set<HTMLMediaElement>();
  // The picture of each person, kept so a tile that mounts after the track arrived still gets it.
  const videoTracks = new Map<string, RemoteTrack>();

  room.on(RoomEvent.TrackSubscribed, (track, _publication, participant) => {
    if (track.kind === Track.Kind.Video) {
      videoTracks.set(participant.identity, track);
      const el =
        options.participantVideo?.(participant.identity) ??
        options.remoteVideo();
      if (el) track.attach(el);
    } else if (track.kind === Track.Kind.Audio) {
      const el = track.attach();
      el.style.display = "none";
      document.body.appendChild(el);
      audioElements.add(el);
    }
  });
  room.on(RoomEvent.TrackUnsubscribed, (track, _publication, participant) => {
    if (videoTracks.get(participant.identity) === track)
      videoTracks.delete(participant.identity);
    track.detach().forEach((el) => {
      if (audioElements.delete(el)) el.remove();
    });
  });
  room.on(RoomEvent.ActiveSpeakersChanged, (speakers) =>
    options.onSpeakers?.(speakers.map((speaker) => speaker.identity)),
  );
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
    attachVideo(personId, element) {
      videoTracks.get(personId)?.attach(element);
    },
    disconnect() {
      videoTracks.clear();
      audioElements.forEach((el) => el.remove());
      audioElements.clear();
      void room.disconnect();
    },
  };
}
