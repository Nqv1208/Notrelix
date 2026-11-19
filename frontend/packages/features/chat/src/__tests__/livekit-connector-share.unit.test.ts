import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { ChatCallMediaOptions } from "../web/calls/chat-call-media";

// A fake room: it records the handlers the connector registers and what it was asked to publish.
const room = vi.hoisted(() => {
  const handlers = new Map<string, (...args: unknown[]) => void>();
  return {
    handlers,
    shareCalls: [] as Array<[boolean, unknown]>,
    shareError: null as null | { name: string },
    emit: (event: string, ...args: unknown[]) => handlers.get(event)?.(...args),
  };
});

vi.mock("livekit-client", () => {
  class Room {
    localParticipant = {
      setScreenShareEnabled: async (on: boolean, options?: unknown) => {
        room.shareCalls.push([on, options]);
        if (room.shareError) throw room.shareError;
        // Stopping unpublishes the track, as the real client does.
        if (!on) room.emit("localTrackUnpublished", { source: "screen_share" });
      },
    };
    on(event: string, handler: (...args: unknown[]) => void) {
      room.handlers.set(event, handler);
      return this;
    }
    connect = async () => undefined;
    disconnect = async () => undefined;
  }
  return {
    Room,
    RoomEvent: {
      TrackSubscribed: "trackSubscribed",
      TrackUnsubscribed: "trackUnsubscribed",
      LocalTrackUnpublished: "localTrackUnpublished",
      Reconnecting: "reconnecting",
      Reconnected: "reconnected",
      Disconnected: "disconnected",
    },
    Track: {
      Kind: { Video: "video", Audio: "audio" },
      Source: { ScreenShare: "screen_share", Camera: "camera" },
    },
  };
});

import { connectLiveKit } from "../web/calls/livekit-connector";

function options(
  over: Partial<ChatCallMediaOptions> = {},
): ChatCallMediaOptions {
  return {
    url: "wss://sfu.example",
    token: "t",
    remoteVideo: () => null,
    localVideo: () => null,
    onState: () => undefined,
    ...over,
  };
}

const track = (source: string, kind = "video") => ({
  kind,
  source,
  attach: vi.fn(),
  detach: vi.fn(() => []),
});

describe("the LiveKit connector: screen share", () => {
  beforeEach(() => {
    room.handlers.clear();
    room.shareCalls.length = 0;
    room.shareError = null;
    vi.stubGlobal("navigator", {
      mediaDevices: { getDisplayMedia: () => undefined },
    });
  });
  afterEach(() => vi.unstubAllGlobals());

  it("puts a shared screen on the screen tile and a camera on the camera tile", async () => {
    const remote = { id: "camera-tile" } as unknown as HTMLVideoElement;
    const screenTile = { id: "screen-tile" } as unknown as HTMLVideoElement;
    await connectLiveKit(
      options({ remoteVideo: () => remote, screenVideo: () => screenTile }),
    );

    const screen = track("screen_share");
    const camera = track("camera");
    room.emit("trackSubscribed", screen, {}, { identity: "quan" });
    room.emit("trackSubscribed", camera, {}, { identity: "quan" });

    expect(screen.attach).toHaveBeenCalledWith(screenTile);
    expect(screen.attach).not.toHaveBeenCalledWith(remote);
    expect(camera.attach).toHaveBeenCalledWith(remote);
  });

  it("keeps a person's camera and screen apart: the camera on their own tile, the screen on the screen tile", async () => {
    const personTile = { id: "quan-tile" } as unknown as HTMLVideoElement;
    const screenTile = { id: "screen-tile" } as unknown as HTMLVideoElement;
    const session = await connectLiveKit(
      options({
        participantVideo: (id) => (id === "quan" ? personTile : null),
        screenVideo: () => screenTile,
      }),
    );
    const quan = { identity: "quan" };
    const camera = track("camera");
    const screen = track("screen_share");

    room.emit("trackSubscribed", camera, {}, quan);
    room.emit("trackSubscribed", screen, {}, quan);

    expect(camera.attach).toHaveBeenCalledWith(personTile);
    expect(screen.attach).toHaveBeenCalledWith(screenTile);
    expect(screen.attach).not.toHaveBeenCalledWith(personTile);

    // A tile that mounts later asks for the camera and gets the camera, never the screen.
    const later = { id: "later-tile" } as unknown as HTMLVideoElement;
    session.attachVideo?.("quan", later);
    expect(camera.attach).toHaveBeenCalledWith(later);
    expect(screen.attach).not.toHaveBeenCalledWith(later);
  });

  it("gives a screen tile that mounts later the track that already arrived", async () => {
    const session = await connectLiveKit(options());
    const screen = track("screen_share");
    room.emit("trackSubscribed", screen, {}, { identity: "quan" });
    expect(screen.attach).not.toHaveBeenCalled();

    const late = { id: "late" } as unknown as HTMLVideoElement;
    session.attachScreen?.(late);
    expect(screen.attach).toHaveBeenCalledWith(late);
  });

  it("shares and stops through the browser's picker, and a stop it asked for is not reported as the browser's", async () => {
    const ended = vi.fn();
    const session = await connectLiveKit(options({ onScreenEnded: ended }));

    expect(await session.setScreenShare?.(true)).toBeNull();
    expect(await session.setScreenShare?.(false)).toBeNull();

    expect(room.shareCalls.map(([on]) => on)).toEqual([true, false]);
    expect(ended).not.toHaveBeenCalled();
  });

  it("reports an end the browser caused", async () => {
    const ended = vi.fn();
    await connectLiveKit(options({ onScreenEnded: ended }));

    room.emit("localTrackUnpublished", { source: "screen_share" });
    room.emit("localTrackUnpublished", { source: "camera" });

    expect(ended).toHaveBeenCalledTimes(1);
  });

  it("answers denied for a refused permission or a closed picker, unavailable for anything else", async () => {
    const session = await connectLiveKit(options());
    room.shareError = { name: "NotAllowedError" };
    expect(await session.setScreenShare?.(true)).toBe("denied");
    room.shareError = { name: "NotFoundError" };
    expect(await session.setScreenShare?.(true)).toBe("unavailable");
  });

  it("offers no share where the browser has no screen picker", async () => {
    vi.stubGlobal("navigator", { mediaDevices: {} });
    const session = await connectLiveKit(options());

    expect(session.setScreenShare).toBeUndefined();
  });
});
