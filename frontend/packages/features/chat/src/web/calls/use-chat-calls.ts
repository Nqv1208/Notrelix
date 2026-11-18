/**
 * The call controller of one Workspace's Chat (one-to-one calls). It starts, answers, declines, cancels and ends calls over REST,
 * keeps the call it holds in step with the call frames and a short safety poll, and drives the media session through the
 * connector seam. The header's voice and video buttons call `start`; the dock renders `current`, `incoming` and `error`.
 */
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import {
  createChatService,
  type ChatApiClient,
  type ChatEndpoints,
} from "../../core/api/chat.service";
import { chatFailureOf } from "../../core/api/chat-failure";
import type { ChatIncomingCall } from "../../core/model/call-model";
import type { ChatCallFramePayload } from "../../core/realtime/chat-frames";
import type {
  ChatCall,
  ChatCallEndReason,
  ChatCallJoin,
  ChatCallKind,
} from "../../core/types/call";
import { chatQueryKeys } from "../../query/keys";
import {
  CALL_RECONNECT_LIMIT_MS,
  connectChatCallMedia,
  type ChatCallMediaConnector,
  type ChatCallMediaSession,
  type ChatCallMediaState,
} from "./chat-call-media";

/** How long a ring that never gets an end frame stays on screen: the server's 45 s ring plus a margin. */
export const INCOMING_RING_TTL_MS = 50_000;
const ENDED_CARD_MS = 8_000;
const SAFETY_POLL_MS = 5_000;

export type ChatCallError =
  "unavailable" | "start-failed" | "you-busy" | "already-active" | "full";

export interface ChatCallsDeps {
  readonly api: ChatApiClient;
  readonly endpoints: ChatEndpoints;
}

export interface ChatCurrentCall {
  readonly call: ChatCall;
  /**
   * The media plane: idle until a join token arrives. `lost` is a connection that stayed down for the whole limit (or whose
   * room closed under an active call); the screen offers a retry and leaving.
   */
  readonly media: "idle" | ChatCallMediaState | "connecting" | "lost";
  /** The reconnect try the media client is on (from 1), or null while the number is not known (offline, waiting). */
  readonly attempt: number | null;
  readonly audioOn: boolean;
  readonly videoOn: boolean;
  /** A device the browser refused; the call goes on without it. */
  readonly deviceNotice: "mic-denied" | "camera-denied" | null;
}

export interface ChatCallsController {
  /** False once the server answered that calls are off. */
  readonly enabled: boolean;
  readonly current: ChatCurrentCall | null;
  readonly incoming: readonly ChatIncomingCall[];
  readonly error: ChatCallError | null;
  readonly start: (spaceId: string, kind: ChatCallKind) => void;
  /** `voiceOnly` joins a video call with the camera off. */
  readonly answer: (callId: string, voiceOnly?: boolean) => void;
  readonly decline: (callId: string) => void;
  /** Cancel while ringing out, end while active. */
  readonly hangUp: () => void;
  readonly toggleMicrophone: () => void;
  readonly toggleCamera: () => void;
  readonly retryConnect: () => void;
  readonly dismiss: () => void;
  readonly remoteVideoRef: (el: HTMLVideoElement | null) => void;
  readonly localVideoRef: (el: HTMLVideoElement | null) => void;
}

export interface UseChatCallsOptions {
  readonly deps: ChatCallsDeps;
  readonly workspaceId: string;
  /** Replaces the media plane; the default loads LiveKit (or connects the mock token at once). */
  readonly connector?: ChatCallMediaConnector;
}

const errorOf = (error: unknown): ChatCallError => {
  const failure = chatFailureOf(error);
  switch (failure.code) {
    case "chat.calls.unavailable":
      return "unavailable";
    case "call-busy":
      return "you-busy";
    case "call-already-active":
      return "already-active";
    case "call-full":
      return "full";
    default:
      return failure.status === 503 ? "unavailable" : "start-failed";
  }
};

const ended = (
  call: ChatCall,
  reason: ChatCallEndReason,
  version: number,
  now: string,
): ChatCall => ({
  ...call,
  state: "ended",
  endReason: reason,
  endedAt: call.endedAt ?? now,
  version: Math.max(call.version, version),
});

export function useChatCalls({
  deps,
  workspaceId: ws,
  connector = connectChatCallMedia,
}: UseChatCallsOptions): ChatCallsController {
  const queryClient = useQueryClient();
  const service = useMemo(
    () => createChatService(deps.api, deps.endpoints),
    [deps.api, deps.endpoints],
  );
  const [current, setCurrent] = useState<ChatCurrentCall | null>(null);
  const [error, setError] = useState<ChatCallError | null>(null);
  const [disabled, setDisabled] = useState(false);
  const currentRef = useRef<ChatCurrentCall | null>(null);
  const sessionRef = useRef<ChatCallMediaSession | null>(null);
  const tokenRef = useRef<{ url: string; token: string } | null>(null);
  const devicesOnRef = useRef(false);
  // The media plane's own word, the browser's network, and the one timer that gives a dropped connection up.
  const sdkStateRef = useRef<ChatCallMediaState | null>(null);
  const offlineRef = useRef(false);
  const giveUpTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  // A session whose events no longer count (it was dropped or replaced).
  const generationRef = useRef(0);
  const leavingRef = useRef(false);
  const remoteEl = useRef<HTMLVideoElement | null>(null);
  const localEl = useRef<HTMLVideoElement | null>(null);

  const update = useCallback((next: ChatCurrentCall | null) => {
    currentRef.current = next;
    setCurrent(next);
  }, []);
  const patch = useCallback(
    (fields: Partial<ChatCurrentCall>) => {
      const held = currentRef.current;
      if (held) update({ ...held, ...fields });
    },
    [update],
  );

  const disarmGiveUp = useCallback(() => {
    if (giveUpTimerRef.current) clearTimeout(giveUpTimerRef.current);
    giveUpTimerRef.current = null;
  }, []);

  /** Closes the media session; what it still reports is ignored. */
  const dropSession = useCallback(() => {
    generationRef.current += 1;
    disarmGiveUp();
    sdkStateRef.current = null;
    devicesOnRef.current = false;
    const session = sessionRef.current;
    sessionRef.current = null;
    session?.disconnect();
  }, [disarmGiveUp]);

  const teardown = useCallback(() => {
    dropSession();
    tokenRef.current = null;
  }, [dropSession]);
  useEffect(() => teardown, [teardown]);

  /** The connection the person is in: the media plane's state, or reconnecting while the browser is offline. */
  const syncMedia = useCallback(() => {
    const held = currentRef.current;
    const sdk = sdkStateRef.current;
    if (!held || held.call.state === "ended" || !sdk) return;
    const media =
      sdk === "connected" && offlineRef.current ? "reconnecting" : sdk;
    patch({
      media,
      attempt: media === "reconnecting" ? held.attempt : null,
    });
  }, [patch]);

  /** The limit passed without a connection: the session is closed (the media server sees the person leave) and the screen offers a retry. */
  const giveUp = useCallback(() => {
    disarmGiveUp();
    const held = currentRef.current;
    if (!held || held.call.state !== "active") return;
    dropSession();
    patch({ media: "lost", attempt: null });
  }, [disarmGiveUp, dropSession, patch]);

  const armGiveUp = useCallback(() => {
    if (!giveUpTimerRef.current)
      giveUpTimerRef.current = setTimeout(giveUp, CALL_RECONNECT_LIMIT_MS);
  }, [giveUp]);

  const settle = useCallback(
    (call: ChatCall) => {
      const held = currentRef.current;
      if (!held || held.call.id !== call.id) return;
      if (call.version < held.call.version) return;
      if (call.state === "ended") teardown();
      update({
        ...held,
        call,
        media: call.state === "ended" ? "idle" : held.media,
      });
    },
    [teardown, update],
  );

  const connect = useCallback(
    async (
      url: string,
      token: string,
      // A first connect says "connecting"; a recovery keeps saying "reconnecting" and falls back to "lost".
      recovering = false,
    ) => {
      tokenRef.current = { url, token };
      const generation = (generationRef.current += 1);
      sdkStateRef.current = null;
      if (!recovering) patch({ media: "connecting" });
      try {
        const session = await connector({
          url,
          token,
          remoteVideo: () => remoteEl.current,
          localVideo: () => localEl.current,
          onState: (state) => {
            if (generation !== generationRef.current) return;
            const held = currentRef.current;
            if (!held || held.call.state === "ended") return;
            sdkStateRef.current = state;
            if (state === "connected") disarmGiveUp();
            else if (state === "reconnecting" && held.call.state === "active")
              armGiveUp();
            // The room closed under a call that goes on: nothing more will come from this session.
            if (state === "disconnected" && held.call.state === "active")
              giveUp();
            else syncMedia();
          },
          onAttempt: (attempt) => {
            if (generation === generationRef.current) patch({ attempt });
          },
        });
        const held = currentRef.current;
        if (
          generation !== generationRef.current ||
          !held ||
          held.call.state === "ended"
        ) {
          session.disconnect();
          return;
        }
        sessionRef.current = session;
        syncMedia();
      } catch {
        if (generation !== generationRef.current) return;
        patch({ media: recovering ? "lost" : "failed" });
      }
    },
    [armGiveUp, connector, disarmGiveUp, giveUp, patch, syncMedia],
  );

  const adopt = useCallback(
    (join: ChatCallJoin, voiceOnly = false) => {
      const { call, joinToken } = join;
      teardown();
      leavingRef.current = false;
      offlineRef.current = false;
      update({
        call,
        media: "idle",
        attempt: null,
        audioOn: true,
        videoOn: call.kind === "video" && !voiceOnly,
        deviceNotice: null,
      });
      if (call.state !== "ended" && joinToken)
        void connect(joinToken.url, joinToken.token);
    },
    [connect, teardown, update],
  );

  // Devices go live once the room is joined and the other person has answered.
  const held = current;
  useEffect(() => {
    if (
      !held ||
      held.media !== "connected" ||
      held.call.state !== "active" ||
      devicesOnRef.current ||
      !sessionRef.current
    )
      return;
    devicesOnRef.current = true;
    const session = sessionRef.current;
    void (async () => {
      const mic = await session.setMicrophone(held.audioOn);
      const cam =
        held.call.kind === "video"
          ? await session.setCamera(held.videoOn)
          : null;
      const latest = currentRef.current;
      if (!latest) return;
      update({
        ...latest,
        audioOn: mic ? false : latest.audioOn,
        videoOn: cam ? false : latest.videoOn,
        deviceNotice: mic ? "mic-denied" : cam ? "camera-denied" : null,
      });
    })();
  }, [held, update]);

  // The newest frame of the held call: an end shows at once, anything else reads the call again.
  const callId = current?.call.id ?? "";
  // Client-only caches: the adapter writes them, nothing fetches them (a fetch would race a frame and overwrite it).
  const frameQuery = useQuery<ChatCallFramePayload | null>({
    queryKey: chatQueryKeys.callFrame(ws, callId),
    queryFn: () => null,
    enabled: false,
  });
  const frame = frameQuery.data;
  useEffect(() => {
    const heldCall = currentRef.current?.call;
    if (!frame || !heldCall || frame.callId !== heldCall.id) return;
    if (frame.version <= heldCall.version) return;
    if (frame.state === "ended") {
      settle(
        ended(
          heldCall,
          (frame.endReason as ChatCallEndReason | null) ?? "completed",
          frame.version,
          new Date().toISOString(),
        ),
      );
      return;
    }
    void service
      .getActiveCall(ws, heldCall.spaceId)
      .then((fresh) => fresh && settle(fresh))
      .catch(() => undefined);
  }, [frame, service, settle, ws]);

  // A safety read for a call whose frames this page does not receive (another channel is open): no live call means it ended.
  const live = current !== null && current.call.state !== "ended";
  const liveCallId = current?.call.id;
  const liveSpaceId = current?.call.spaceId;
  useEffect(() => {
    if (!live || !liveSpaceId) return;
    const timer = setInterval(() => {
      void service
        .getActiveCall(ws, liveSpaceId)
        .then((fresh) => {
          const heldCall = currentRef.current?.call;
          if (!heldCall || heldCall.id !== liveCallId) return;
          if (fresh && fresh.id === heldCall.id) settle(fresh);
          else if (!fresh)
            settle(
              ended(
                heldCall,
                heldCall.state === "active" ? "completed" : "missed",
                heldCall.version + 1,
                new Date().toISOString(),
              ),
            );
        })
        .catch(() => undefined);
    }, SAFETY_POLL_MS);
    return () => clearInterval(timer);
  }, [live, liveCallId, liveSpaceId, service, settle, ws]);

  // The browser's network: offline during a call is a dropped connection, and coming back online ends it when the media plane is still up.
  useEffect(() => {
    if (!live) return;
    const goOffline = () => {
      offlineRef.current = true;
      if (currentRef.current?.call.state === "active") armGiveUp();
      syncMedia();
    };
    const goOnline = () => {
      offlineRef.current = false;
      if (sdkStateRef.current === "connected") disarmGiveUp();
      syncMedia();
    };
    window.addEventListener("offline", goOffline);
    window.addEventListener("online", goOnline);
    return () => {
      window.removeEventListener("offline", goOffline);
      window.removeEventListener("online", goOnline);
    };
  }, [armGiveUp, disarmGiveUp, live, syncMedia]);

  // The ended card leaves by itself.
  const endedId = current?.call.state === "ended" ? current.call.id : null;
  useEffect(() => {
    if (!endedId) return;
    const timer = setTimeout(() => update(null), ENDED_CARD_MS);
    return () => clearTimeout(timer);
  }, [endedId, update]);

  // Incoming rings: the per-person frames fill the list; a ring nobody ended is dropped after its time.
  const incomingKey = chatQueryKeys.incomingCalls(ws);
  const incomingQuery = useQuery<ChatIncomingCall[]>({
    queryKey: incomingKey,
    queryFn: () => [],
    enabled: false,
  });
  const incoming = useMemo(
    () => incomingQuery.data ?? [],
    [incomingQuery.data],
  );
  const dropIncoming = useCallback(
    (id: string) =>
      queryClient.setQueryData<ChatIncomingCall[]>(incomingKey, (list = []) =>
        list.filter((entry) => entry.callId !== id),
      ),
    [incomingKey, queryClient],
  );
  useEffect(() => {
    if (incoming.length === 0) return;
    const now = Date.now();
    const nextDue = Math.min(
      ...incoming.map(
        (entry) => Date.parse(entry.receivedAt) + INCOMING_RING_TTL_MS - now,
      ),
    );
    const timer = setTimeout(
      () => {
        const cutoff = Date.now() - INCOMING_RING_TTL_MS;
        queryClient.setQueryData<ChatIncomingCall[]>(incomingKey, (list = []) =>
          list.filter((entry) => Date.parse(entry.receivedAt) > cutoff),
        );
      },
      Math.max(0, nextDue) + 50,
    );
    return () => clearTimeout(timer);
  }, [incoming, incomingKey, queryClient]);

  /** A call write with the version the caller read; a stale one reads the call again and tries once more. */
  const withFreshVersion = useCallback(
    async <T>(
      spaceId: string,
      id: string,
      version: number,
      run: (version: number) => Promise<T>,
    ): Promise<T> => {
      try {
        return await run(version);
      } catch (cause) {
        if (chatFailureOf(cause).kind !== "stale") throw cause;
        const fresh = await service.getActiveCall(ws, spaceId);
        if (!fresh || fresh.id !== id) throw cause;
        return run(fresh.version);
      }
    },
    [service, ws],
  );

  const start = useCallback(
    (spaceId: string, kind: ChatCallKind) => {
      const heldCall = currentRef.current?.call;
      if (heldCall && heldCall.state !== "ended") {
        setError("you-busy");
        return;
      }
      setError(null);
      void service
        .startCall(ws, spaceId, kind, crypto.randomUUID())
        .then(adopt)
        .catch((cause) => {
          const next = errorOf(cause);
          if (next === "unavailable") setDisabled(true);
          setError(next);
        });
    },
    [adopt, service, ws],
  );

  const answer = useCallback(
    (id: string, voiceOnly = false) => {
      const entry = queryClient
        .getQueryData<ChatIncomingCall[]>(incomingKey)
        ?.find((item) => item.callId === id);
      if (!entry) return;
      const heldCall = currentRef.current?.call;
      if (heldCall && heldCall.state !== "ended") {
        setError("you-busy");
        return;
      }
      setError(null);
      void withFreshVersion(entry.spaceId, id, entry.version, (version) =>
        service.answerCall(ws, id, version),
      )
        .then((join) => {
          dropIncoming(id);
          adopt(join, voiceOnly);
        })
        .catch((cause) => {
          dropIncoming(id);
          setError(errorOf(cause));
        });
    },
    [
      adopt,
      dropIncoming,
      incomingKey,
      queryClient,
      service,
      withFreshVersion,
      ws,
    ],
  );

  const decline = useCallback(
    (id: string) => {
      const entry = queryClient
        .getQueryData<ChatIncomingCall[]>(incomingKey)
        ?.find((item) => item.callId === id);
      dropIncoming(id);
      if (!entry) return;
      void withFreshVersion(entry.spaceId, id, entry.version, (version) =>
        service.changeCall(ws, id, "decline", version),
      ).catch(() => undefined);
    },
    [dropIncoming, incomingKey, queryClient, service, withFreshVersion, ws],
  );

  const hangUp = useCallback(() => {
    const heldCall = currentRef.current?.call;
    if (!heldCall || heldCall.state === "ended") return;
    // A second press while the first is on its way sends nothing more.
    if (leavingRef.current) return;
    leavingRef.current = true;
    const action = heldCall.state === "ringing" ? "cancel" : "end";
    teardown();
    void withFreshVersion(
      heldCall.spaceId,
      heldCall.id,
      heldCall.version,
      (v) => service.changeCall(ws, heldCall.id, action, v),
    )
      .then((call) => settle(call))
      .catch((cause) => {
        // Someone else ended it first, or the network is gone (the media server already sees the person leave): over either way.
        if (
          chatFailureOf(cause).code === "call-ended" ||
          chatFailureOf(cause).kind === "not-found" ||
          chatFailureOf(cause).kind === "network"
        )
          settle(
            ended(
              heldCall,
              heldCall.state === "active" ? "completed" : "cancelled",
              heldCall.version + 1,
              new Date().toISOString(),
            ),
          );
      });
    patch({ media: "idle" });
  }, [patch, service, settle, teardown, withFreshVersion, ws]);

  const syncDevices = useCallback(
    (audioOn: boolean, videoOn: boolean) => {
      const heldCall = currentRef.current?.call;
      if (!heldCall || heldCall.state !== "active") return;
      void withFreshVersion(
        heldCall.spaceId,
        heldCall.id,
        heldCall.version,
        (v) =>
          service.setCallMediaState(ws, heldCall.id, { audioOn, videoOn }, v),
      )
        .then(settle)
        .catch(() => undefined);
    },
    [service, settle, withFreshVersion, ws],
  );

  const toggleMicrophone = useCallback(() => {
    const heldNow = currentRef.current;
    if (!heldNow) return;
    const next = !heldNow.audioOn;
    patch({ audioOn: next });
    void sessionRef.current?.setMicrophone(next);
    syncDevices(next, heldNow.videoOn);
  }, [patch, syncDevices]);

  const toggleCamera = useCallback(() => {
    const heldNow = currentRef.current;
    if (!heldNow || heldNow.call.kind !== "video") return;
    const next = !heldNow.videoOn;
    patch({ videoOn: next });
    void sessionRef.current?.setCamera(next);
    syncDevices(heldNow.audioOn, next);
  }, [patch, syncDevices]);

  /** After a give-up: read the call, and when it still runs ask for a fresh token (a repeat answer returns one) and join the room again. */
  const recover = useCallback(async () => {
    const heldCall = currentRef.current?.call;
    if (!heldCall || heldCall.state !== "active") return;
    patch({ media: "reconnecting", attempt: null });
    armGiveUp();
    const gone = () =>
      settle(
        ended(
          heldCall,
          "completed",
          heldCall.version + 1,
          new Date().toISOString(),
        ),
      );
    try {
      const fresh = await service.getActiveCall(ws, heldCall.spaceId);
      if (!fresh || fresh.id !== heldCall.id || fresh.state === "ended")
        return fresh ? settle(fresh) : gone();
      const join = await service.answerCall(ws, fresh.id, fresh.version);
      settle(join.call);
      if (join.call.state !== "ended" && join.joinToken)
        await connect(join.joinToken.url, join.joinToken.token, true);
    } catch (cause) {
      const failure = chatFailureOf(cause);
      if (failure.code === "call-ended" || failure.kind === "not-found") gone();
      else {
        disarmGiveUp();
        patch({ media: "lost", attempt: null });
      }
    }
  }, [armGiveUp, connect, disarmGiveUp, patch, service, settle, ws]);

  const retryConnect = useCallback(() => {
    if (currentRef.current?.media === "lost") {
      void recover();
      return;
    }
    const ticket = tokenRef.current;
    if (ticket) void connect(ticket.url, ticket.token);
  }, [connect, recover]);

  const dismiss = useCallback(() => {
    setError(null);
    if (currentRef.current?.call.state === "ended") update(null);
  }, [update]);

  const remoteVideoRef = useCallback((el: HTMLVideoElement | null) => {
    remoteEl.current = el;
  }, []);
  const localVideoRef = useCallback((el: HTMLVideoElement | null) => {
    localEl.current = el;
  }, []);

  return {
    enabled: !disabled,
    current,
    incoming,
    error,
    start,
    answer,
    decline,
    hangUp,
    toggleMicrophone,
    toggleCamera,
    retryConnect,
    dismiss,
    remoteVideoRef,
    localVideoRef,
  };
}
