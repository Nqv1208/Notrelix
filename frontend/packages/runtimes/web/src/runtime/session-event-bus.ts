export interface SessionExpiredEvent {
  readonly type: 'session-expired';
  readonly error?: unknown;
  readonly occurredAt: string;
}

export interface SessionEventBus {
  publish(event: SessionExpiredEvent): void;
  subscribe(listener: (event: SessionExpiredEvent) => void): () => void;
  clear(): void;
}

export function createSessionEventBus(): SessionEventBus {
  const listeners: Set<(event: SessionExpiredEvent) => void> = new Set();

  return {
    publish(event: SessionExpiredEvent): void {
      listeners.forEach((listener) => {
        try {
          listener(event);
        } catch (err) {
          console.error('[SessionEventBus] Error in session event listener:', err);
        }
      });
    },

    subscribe(listener: (event: SessionExpiredEvent) => void): () => void {
      listeners.add(listener);
      return () => {
        listeners.delete(listener);
      };
    },

    clear(): void {
      listeners.clear();
    },
  };
}
