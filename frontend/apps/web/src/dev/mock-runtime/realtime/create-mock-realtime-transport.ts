import type {
  RealtimeConnectionState,
  RealtimeRecoveryListener,
  RealtimeStateListener,
  RealtimeTransport,
} from "@notrelix/realtime";

export function createMockRealtimeTransport(): RealtimeTransport {
  let state: RealtimeConnectionState = "idle";
  const stateListeners = new Set<RealtimeStateListener>();
  const setState = (next: RealtimeConnectionState) => {
    state = next;
    for (const listener of stateListeners) listener(next);
  };
  return {
    async connect() { setState("connected"); },
    disconnect() { setState("closed"); },
    subscribe() { return () => undefined; },
    subscribeState(listener) { stateListeners.add(listener); return () => stateListeners.delete(listener); },
    subscribeRecovery(_listener: RealtimeRecoveryListener) { return () => undefined; },
    getState() { return state; },
    dispose() { stateListeners.clear(); state = "closed"; },
  };
}
