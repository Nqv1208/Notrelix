import type { QueryClient } from "@tanstack/react-query";
import type { RealtimeTransport } from "@notrelix/realtime";
import type {
  SessionEventBus,
  SessionExpiredEvent,
} from "@notrelix/runtime-web";

const MAX_HANDLED_SESSION_EVENTS = 32;

export interface ApplicationLifecycleDependencies {
  readonly queryClient: QueryClient;
  readonly realtime: RealtimeTransport;
  readonly sessionEvents: SessionEventBus;
  readonly navigateToSignedOut: () => void;
}

export interface ApplicationLifecycle {
  prepareWorkspaceTransition(nextWorkspaceId: string | null): void;
  dispose(): void;
}

export function createApplicationLifecycle(
  deps: ApplicationLifecycleDependencies,
): ApplicationLifecycle {
  const handledSessionEventIds: Set<string> = new Set();
  let activeWorkspaceId: string | null = null;
  let disposed = false;

  const handleSessionExpired = (event: SessionExpiredEvent): void => {
    if (disposed || handledSessionEventIds.has(event.eventId)) {
      return;
    }

    handledSessionEventIds.add(event.eventId);
    if (handledSessionEventIds.size > MAX_HANDLED_SESSION_EVENTS) {
      const first = handledSessionEventIds.values().next().value;
      if (first) {
        handledSessionEventIds.delete(first);
      }
    }

    deps.queryClient.clear();
    deps.realtime.disconnect("session-expired");
    deps.navigateToSignedOut();
  };

  const unsubscribeSession = deps.sessionEvents.subscribe(handleSessionExpired);

  return {
    prepareWorkspaceTransition(nextWorkspaceId: string | null): void {
      if (disposed) return;

      if (activeWorkspaceId === null) {
        activeWorkspaceId = nextWorkspaceId;
        return;
      }

      if (activeWorkspaceId === nextWorkspaceId) {
        return;
      }

      deps.queryClient.clear();
      deps.realtime.disconnect("workspace-switch");
      activeWorkspaceId = nextWorkspaceId;
    },

    dispose(): void {
      if (disposed) return;
      disposed = true;
      unsubscribeSession();
      handledSessionEventIds.clear();
      activeWorkspaceId = null;
    },
  };
}
