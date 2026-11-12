/**
 * MockRealtimeTransport — deterministic, zero-network dev realtime transport.
 *
 * Implements RealtimeTransport without opening any WebSocket or network sockets.
 *
 * Plan: 01-FREEZE-SPEC.md §FZ-S05, 02-IMPLEMENTATION-PLAN.md §MFB-FZ-01
 */

import {
  REALTIME_PRESENCE_MIN_INTERVAL_MS,
  REALTIME_PRESENCE_TTL_MS,
  REALTIME_TYPING_EXPIRY_MS,
  REALTIME_TYPING_MIN_INTERVAL_MS,
  type RealtimeEphemeralEvent,
  type RealtimeEphemeralListener,
  type RealtimePresenceState,
} from "@notrelix/realtime";
import type {
  RealtimeTransport,
  RealtimeConnectContext,
  RealtimeEnvelope,
  RealtimeSubscriptionFilter,
  RealtimeEventListener,
  RealtimeRecoverySignal,
  RealtimeStateListener,
  RealtimeRecoveryListener,
  RealtimeConnectionState,
} from "@notrelix/realtime";

/**
 * A scope the gateway streams beside the workspace and board streams: a Chat
 * channel (`channel:<spaceId>`) or the caller's per-user stream (`user:<userId>`).
 */
export interface MockScopeTarget {
  readonly kind: "channel" | "user";
  readonly id: string;
}

/** Every transport created and not disposed: a scope frame reaches all of them. */
const liveTransports = new Set<MockRealtimeTransport>();

/** The persona a transport stands for when no viewer id is given. */
export const MOCK_VIEWER_ID = "mock-viewer";

/** At most this many personas keep a presence entry in the mock (the real gateway also bounds its state). */
const MAX_MOCK_PRESENCE_ENTRIES = 200;

let ephemeralNow: () => number = () => performance.now();

/** Test helper: the (monotonic, millisecond) clock the mock uses for the typing/presence rate limits and expiry. */
export function setMockEphemeralClock(now: (() => number) | null): void {
  ephemeralNow = now ?? (() => performance.now());
}

interface MockPresenceEntry {
  state: RealtimePresenceState;
  reportedAt: number;
  acceptedAt: number;
}

/**
 * The mock gateway's ephemeral memory: last typing frame per (persona, channel)
 * for the rate limit, and the last presence per persona. In memory only, bounded,
 * and cleared with `resetMockEphemeral`. Same rules as the gateway: a typing
 * frame sooner than 2 s after the last is dropped, a presence frame with an
 * unchanged state sooner than 5 s is dropped, and a presence state with no
 * heartbeat for 75 s counts as offline.
 */
const lastTypingAt = new Map<string, number>();
const personaPresence = new Map<string, MockPresenceEntry>();

export function resetMockEphemeral(): void {
  lastTypingAt.clear();
  personaPresence.clear();
}

function deliverEphemeral(
  event: RealtimeEphemeralEvent,
  except?: MockRealtimeTransport,
): void {
  for (const transport of liveTransports) {
    if (transport === except) continue;
    transport.deliverEphemeral(event);
  }
}

/**
 * A mock persona (not the viewer) types in a channel: every live transport's
 * listeners of that channel hear it, unless the rate limit drops the frame.
 * Returns whether the frame was delivered.
 */
export function simulateMockTyping(
  channelId: string,
  userId: string,
  except?: MockRealtimeTransport,
): boolean {
  const key = `${userId}|${channelId}`;
  const now = ephemeralNow();
  const last = lastTypingAt.get(key);
  if (last !== undefined && now - last < REALTIME_TYPING_MIN_INTERVAL_MS) {
    return false;
  }
  if (lastTypingAt.size >= MAX_MOCK_PRESENCE_ENTRIES * 5) {
    const oldest = lastTypingAt.keys().next().value;
    if (oldest !== undefined) lastTypingAt.delete(oldest);
  }
  lastTypingAt.set(key, now);
  deliverEphemeral(
    {
      kind: "typing",
      channelId,
      userId,
      expiresInMs: REALTIME_TYPING_EXPIRY_MS,
    },
    except,
  );
  return true;
}

/**
 * A mock persona reports its presence to the channels given (the spaces they
 * share with the viewer). Returns whether the frame was accepted.
 */
export function simulateMockPresence(
  channelIds: readonly string[],
  userId: string,
  state: RealtimePresenceState,
  except?: MockRealtimeTransport,
): boolean {
  const now = ephemeralNow();
  const known = personaPresence.get(userId);
  if (
    known &&
    known.state === state &&
    now - known.acceptedAt < REALTIME_PRESENCE_MIN_INTERVAL_MS
  ) {
    return false;
  }
  if (!known && personaPresence.size >= MAX_MOCK_PRESENCE_ENTRIES) {
    const oldest = personaPresence.keys().next().value;
    if (oldest !== undefined) personaPresence.delete(oldest);
  }
  personaPresence.set(userId, { state, reportedAt: now, acceptedAt: now });
  for (const channelId of channelIds) {
    deliverEphemeral(
      {
        kind: "presence",
        channelId,
        userId,
        state,
        ttlMs: REALTIME_PRESENCE_TTL_MS,
      },
      except,
    );
  }
  return true;
}

/** Presence of the personas that has not expired (what a new subscriber is told). */
function livePresence(): [string, MockPresenceEntry][] {
  const now = ephemeralNow();
  return [...personaPresence].filter(
    ([, entry]) =>
      entry.state !== "offline" &&
      now - entry.reportedAt < REALTIME_PRESENCE_TTL_MS,
  );
}

export class MockRealtimeTransport implements RealtimeTransport {
  private state: RealtimeConnectionState = "idle";
  private readonly stateListeners = new Set<RealtimeStateListener>();
  private readonly recoveryListeners = new Set<RealtimeRecoveryListener>();
  private readonly subscriptions = new Map<
    string,
    { filter: RealtimeSubscriptionFilter; listener: RealtimeEventListener }
  >();
  private subscriptionCounter = 0;
  /** Listeners of the scope streams, by wire subscription id (`channel:<id>`, `user:<id>`). */
  private readonly scopeListeners = new Map<
    string,
    Set<RealtimeEventListener>
  >();

  private readonly ephemeralListeners = new Set<{
    channelId: string;
    workspaceId: string;
    listener: RealtimeEphemeralListener;
  }>();
  private readonly viewerId: string;

  constructor(options: { readonly viewerId?: string } = {}) {
    this.viewerId = options.viewerId ?? MOCK_VIEWER_ID;
    // A browser test drives the connection through `globalThis.__notrelixMockRealtime.simulateState("reconnecting")`.
    (
      globalThis as { __notrelixMockRealtime?: MockRealtimeTransport }
    ).__notrelixMockRealtime = this;
    liveTransports.add(this);
  }

  /** Test/dev helper: the connection state the app shows (reconnecting, offline, connected). */
  simulateState(next: RealtimeConnectionState): void {
    this.setState(next);
  }

  async connect(_context: RealtimeConnectContext): Promise<void> {
    this.setState("connecting");
    // Deterministic in-memory transition to connected — no network
    this.setState("connected");
  }

  disconnect(_reason?: string): void {
    this.setState("closed");
  }

  subscribe(
    filter: RealtimeSubscriptionFilter,
    listener: RealtimeEventListener,
  ): () => void {
    // Chat scopes (channel, per-user) live on the scope streams, as the gateway streams them.
    if (filter.channelId || filter.userId) {
      const target: MockScopeTarget = filter.channelId
        ? { kind: "channel", id: filter.channelId }
        : { kind: "user", id: filter.userId! };
      return this.subscribeScope(target, (envelope) => {
        if (envelope.workspaceId === filter.workspaceId) listener(envelope);
      });
    }
    const id = `mock-sub-${++this.subscriptionCounter}`;
    this.subscriptions.set(id, { filter, listener });
    return () => {
      this.subscriptions.delete(id);
    };
  }

  subscribeEphemeral(
    filter: { readonly workspaceId: string; readonly channelId: string },
    listener: RealtimeEphemeralListener,
  ): () => void {
    const entry = {
      channelId: filter.channelId,
      workspaceId: filter.workspaceId,
      listener,
    };
    this.ephemeralListeners.add(entry);
    // Like the gateway's presence request: a new subscriber is told who is here, once.
    for (const [userId, presence] of livePresence()) {
      if (userId === this.viewerId) continue;
      try {
        listener({
          kind: "presence",
          channelId: filter.channelId,
          userId,
          state: presence.state,
          ttlMs: REALTIME_PRESENCE_TTL_MS,
        });
      } catch {
        // Safe dispatch: errors in listeners don't crash transport
      }
    }
    return () => {
      this.ephemeralListeners.delete(entry);
    };
  }

  sendTyping(channelId: string): void {
    simulateMockTyping(channelId, this.viewerId, this);
  }

  sendPresence(state: RealtimePresenceState): void {
    // The viewer's own heartbeat reaches the other live transports of every channel they listen to.
    const channels = new Set<string>();
    for (const transport of liveTransports) {
      if (transport === this) continue;
      for (const entry of transport.ephemeralListeners) {
        channels.add(entry.channelId);
      }
    }
    simulateMockPresence([...channels], this.viewerId, state, this);
  }

  /** Delivers an ephemeral event to the listeners of its channel (safe dispatch). */
  deliverEphemeral(event: RealtimeEphemeralEvent): void {
    if (event.userId === this.viewerId) return;
    for (const entry of Array.from(this.ephemeralListeners)) {
      if (entry.channelId !== event.channelId) continue;
      try {
        entry.listener(event);
      } catch {
        // Safe dispatch: errors in listeners don't crash transport
      }
    }
  }

  subscribeState(listener: RealtimeStateListener): () => void {
    this.stateListeners.add(listener);
    listener(this.state);
    return () => {
      this.stateListeners.delete(listener);
    };
  }

  subscribeRecovery(listener: RealtimeRecoveryListener): () => void {
    this.recoveryListeners.add(listener);
    return () => {
      this.recoveryListeners.delete(listener);
    };
  }

  getState(): RealtimeConnectionState {
    return this.state;
  }

  /**
   * Test/dev helper: deliver a gateway-shaped event frame (no payload) to the
   * listeners subscribed for its Workspace or Board scope, like the real client does after
   * checkpoint validation.
   */
  deliverEvent(envelope: RealtimeEnvelope): void {
    for (const { filter, listener } of this.subscriptions.values()) {
      if (filter.workspaceId !== envelope.workspaceId) continue;
      // A board frame reaches only the listener of that board, a workspace
      // frame only workspace listeners, as the gateway streams are scoped.
      const frameBoardId = envelope.subscriptionId.startsWith("board:")
        ? envelope.subscriptionId.slice("board:".length)
        : undefined;
      if (frameBoardId !== filter.boardId) continue;
      if (envelope.subscriptionId.startsWith("channel:")) continue;
      if (
        filter.eventTypes &&
        filter.eventTypes.length > 0 &&
        !filter.eventTypes.includes(envelope.eventType)
      ) {
        continue;
      }
      try {
        listener(envelope);
      } catch {
        // Safe dispatch: errors in listeners don't crash transport
      }
    }
  }

  /** Test/dev helper: simulate a stream gap/reset or subscription ack. */
  deliverRecovery(signal: RealtimeRecoverySignal): void {
    for (const listener of this.recoveryListeners) {
      try {
        listener(signal);
      } catch {
        // Safe dispatch: errors in listeners don't crash transport
      }
    }
  }

  /**
   * Subscribes a Chat scope stream (channel or per-user). Frames reach the
   * listener when `publishMockScopeFrame` delivers them; nothing is sent.
   */
  subscribeScope(
    target: MockScopeTarget,
    listener: RealtimeEventListener,
  ): () => void {
    const key = `${target.kind}:${target.id}`;
    const listeners = this.scopeListeners.get(key) ?? new Set();
    listeners.add(listener);
    this.scopeListeners.set(key, listeners);
    return () => {
      listeners.delete(listener);
      if (listeners.size === 0) this.scopeListeners.delete(key);
    };
  }

  /** Delivers a scope frame to the listeners of its subscription id (safe dispatch). */
  deliverScopeFrame(envelope: RealtimeEnvelope): void {
    const listeners = this.scopeListeners.get(envelope.subscriptionId);
    if (!listeners) return;
    for (const listener of Array.from(listeners)) {
      try {
        listener(envelope);
      } catch {
        // Safe dispatch: errors in listeners don't crash transport
      }
    }
  }

  dispose(): void {
    this.disconnect();
    this.stateListeners.clear();
    this.recoveryListeners.clear();
    this.subscriptions.clear();
    this.scopeListeners.clear();
    this.ephemeralListeners.clear();
    liveTransports.delete(this);
  }

  private setState(nextState: RealtimeConnectionState): void {
    if (this.state === nextState) return;
    this.state = nextState;
    for (const listener of this.stateListeners) {
      try {
        listener(nextState);
      } catch {
        // Safe dispatch: errors in listeners don't crash transport
      }
    }
  }
}

export function createMockRealtimeTransport(options?: {
  readonly viewerId?: string;
}): RealtimeTransport {
  return new MockRealtimeTransport(options);
}

/**
 * Hands a Chat scope frame to every live mock transport. The mock store has no
 * socket and no transport of its own; the app's transport is one of these.
 */
export function publishMockScopeFrame(envelope: RealtimeEnvelope): void {
  for (const transport of liveTransports) {
    transport.deliverScopeFrame(envelope);
  }
}
