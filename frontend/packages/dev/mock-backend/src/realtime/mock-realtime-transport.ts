/**
 * MockRealtimeTransport — deterministic, zero-network dev realtime transport.
 *
 * Implements RealtimeTransport without opening any WebSocket or network sockets.
 *
 * Plan: 01-FREEZE-SPEC.md §FZ-S05, 02-IMPLEMENTATION-PLAN.md §MFB-FZ-01
 */

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

  constructor() {
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

export function createMockRealtimeTransport(): RealtimeTransport {
  return new MockRealtimeTransport();
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
