import type { SessionExpiredEvent } from "@notrelix/contracts";

export type { SessionExpiredEvent };

export interface SessionEventBus {
  publish(event: SessionExpiredEvent): void;
  subscribe(listener: (event: SessionExpiredEvent) => void): () => void;
  clear(): void;
}

export function createSessionEventBus(
  reportError?: (error: unknown, context?: Record<string, unknown>) => void,
): SessionEventBus {
  const listeners: Set<(event: SessionExpiredEvent) => void> = new Set();

  return {
    publish(event: SessionExpiredEvent): void {
      listeners.forEach((listener) => {
        try {
          listener(event);
        } catch (err) {
          if (reportError) {
            reportError(err, { eventId: event.eventId, reason: event.reason });
          } else {
            console.error(
              "[SessionEventBus] Error in session event listener:",
              err,
            );
          }
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
