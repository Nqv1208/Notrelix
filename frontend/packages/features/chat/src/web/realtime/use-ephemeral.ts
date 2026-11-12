import {
  useCallback,
  useEffect,
  useMemo,
  useRef,
  useSyncExternalStore,
} from "react";
import {
  createTypingThrottle,
  PRESENCE_DEFAULT_TTL_MS,
  PRESENCE_HEARTBEAT_MS,
  PresenceStore,
  TYPING_DEFAULT_EXPIRY_MS,
  TypingStore,
  type ChatEphemeralPort,
  type ChatPresenceState,
} from "../../core/model/ephemeral-model";

/** A person counts as idle after this long without input while the page is open. */
const IDLE_AFTER_MS = 60_000;

export interface ChatEphemeralState {
  readonly typing: TypingStore;
  readonly presence: PresenceStore;
}

/**
 * The typing and presence stores of one Chat page, fed by the host's port. It
 * listens to the spaces given (the open space for typing and presence, the
 * conversation list for presence dots) and holds nothing in storage.
 */
export function useChatEphemeral(
  port: ChatEphemeralPort | undefined,
  spaceIds: readonly string[],
): ChatEphemeralState {
  const typing = useMemo(() => new TypingStore(), []);
  const presence = useMemo(() => new PresenceStore(), []);
  useEffect(
    () => () => {
      typing.dispose();
      presence.dispose();
    },
    [typing, presence],
  );

  // One stable key per set of spaces, so the subscriptions follow the ids and not the array identity.
  const key = [...new Set(spaceIds)].sort().join("|");
  useEffect(() => {
    if (!port || key === "") return;
    const stops = key.split("|").map((spaceId) =>
      port.subscribe(spaceId, (event) => {
        if (event.kind === "typing") {
          typing.signal(
            spaceId,
            event.userId,
            event.expiresInMs || TYPING_DEFAULT_EXPIRY_MS,
          );
        } else {
          presence.report(
            event.userId,
            event.state,
            event.ttlMs || PRESENCE_DEFAULT_TTL_MS,
          );
        }
      }),
    );
    return () => {
      for (const stop of stops) stop();
    };
  }, [port, key, typing, presence]);

  return { typing, presence };
}

/** The people typing in a space (their ids). A message from one of them clears their indicator. */
export function useTypingPeople(
  typing: TypingStore,
  spaceId: string | null,
  latestMessage: { readonly id: string; readonly authorId: string } | null,
): readonly string[] {
  const version = useSyncExternalStore(typing.subscribe, typing.getVersion);
  const seenMessage = useRef<string | null>(null);
  useEffect(() => {
    if (!spaceId || !latestMessage) return;
    if (seenMessage.current === latestMessage.id) return;
    seenMessage.current = latestMessage.id;
    typing.clear(spaceId, latestMessage.authorId);
  }, [typing, spaceId, latestMessage]);
  return useMemo(
    () => (spaceId ? typing.typingIn(spaceId) : []),
    // `version` changes whenever the store does; the expiry timer bumps it too.
    [typing, spaceId, version],
  );
}

/** The presence of a person; offline when unknown or expired. */
export function usePresenceOf(
  presence: PresenceStore,
): (userId: string) => ChatPresenceState {
  const version = useSyncExternalStore(presence.subscribe, presence.getVersion);
  return useCallback(
    (userId: string) => presence.stateOf(userId),
    [presence, version],
  );
}

/**
 * The caller's typing signal for a space: call it on each change of a
 * non-empty draft; the signal is sent at most once per 3 seconds per space.
 */
export function useTypingSignal(
  port: ChatEphemeralPort | undefined,
  spaceId: string | null,
): () => void {
  const throttle = useMemo(() => createTypingThrottle(), []);
  return useCallback(() => {
    if (!port || !spaceId) return;
    if (throttle(spaceId)) port.sendTyping(spaceId);
  }, [port, spaceId, throttle]);
}

/**
 * The caller's presence while the page is open: online with recent input, idle
 * after a minute without input or while the tab is hidden, a heartbeat every
 * 30 seconds, and offline when the page closes.
 */
export function usePresenceHeartbeat(
  port: ChatEphemeralPort | undefined,
  enabled: boolean,
): void {
  useEffect(() => {
    if (!port || !enabled || typeof document === "undefined") return;
    let lastInput = Date.now();
    let last: ChatPresenceState | null = null;

    const current = (): ChatPresenceState =>
      document.visibilityState === "hidden" ||
      Date.now() - lastInput > IDLE_AFTER_MS
        ? "idle"
        : "online";
    const report = () => {
      last = current();
      port.sendPresence(last);
    };
    const onInput = () => {
      lastInput = Date.now();
      // Coming back from idle is reported at once, not at the next heartbeat.
      if (last === "idle") report();
    };
    const onVisibility = () => report();

    report();
    const interval = window.setInterval(report, PRESENCE_HEARTBEAT_MS);
    document.addEventListener("visibilitychange", onVisibility);
    window.addEventListener("pointerdown", onInput, { passive: true });
    window.addEventListener("keydown", onInput, { passive: true });
    return () => {
      window.clearInterval(interval);
      document.removeEventListener("visibilitychange", onVisibility);
      window.removeEventListener("pointerdown", onInput);
      window.removeEventListener("keydown", onInput);
      port.sendPresence("offline");
    };
  }, [port, enabled]);
}
