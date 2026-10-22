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

export class MockRealtimeTransport implements RealtimeTransport {
  private state: RealtimeConnectionState = "idle";
  private readonly stateListeners = new Set<RealtimeStateListener>();
  private readonly recoveryListeners = new Set<RealtimeRecoveryListener>();
  private readonly subscriptions = new Map<
    string,
    { filter: RealtimeSubscriptionFilter; listener: RealtimeEventListener }
  >();
  private subscriptionCounter = 0;

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
   * listeners subscribed for its Workspace, like the real client does after
   * checkpoint validation.
   */
  deliverEvent(envelope: RealtimeEnvelope): void {
    for (const { filter, listener } of this.subscriptions.values()) {
      if (filter.workspaceId !== envelope.workspaceId) continue;
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

  dispose(): void {
    this.disconnect();
    this.stateListeners.clear();
    this.recoveryListeners.clear();
    this.subscriptions.clear();
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
