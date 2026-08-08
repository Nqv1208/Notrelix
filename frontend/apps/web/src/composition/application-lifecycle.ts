import type { QueryClient } from '@tanstack/react-query';
import type { RealtimeTransport } from '@notrelix/realtime';
import type { SessionEventBus, SessionExpiredEvent } from '@notrelix/runtime-web';

const MAX_HANDLED_SESSION_EVENTS = 32;

export interface WorkspaceChangeEvent {
  readonly previousWorkspaceId: string | null;
  readonly nextWorkspaceId: string | null;
}

export interface WorkspaceEventSource {
  publish(event: WorkspaceChangeEvent): void;
  subscribe(listener: (event: WorkspaceChangeEvent) => void): () => void;
  clear(): void;
}

export function createWorkspaceEventSource(
  reportError?: (error: unknown, context?: Record<string, unknown>) => void
): WorkspaceEventSource {
  const listeners: Set<(event: WorkspaceChangeEvent) => void> = new Set();

  return {
    publish(event: WorkspaceChangeEvent): void {
      listeners.forEach((listener) => {
        try {
          listener(event);
        } catch (err) {
          if (reportError) {
            reportError(err, {
              previousWorkspaceId: event.previousWorkspaceId ?? undefined,
              nextWorkspaceId: event.nextWorkspaceId ?? undefined,
            });
          }
        }
      });
    },

    subscribe(listener: (event: WorkspaceChangeEvent) => void): () => void {
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

export interface ApplicationLifecycleDependencies {
  readonly queryClient: QueryClient;
  readonly realtime: RealtimeTransport;
  readonly sessionEvents: SessionEventBus;
  readonly workspaceEvents: WorkspaceEventSource;
  readonly clearSessionState: () => void;
  readonly clearWorkspaceState: () => void;
  readonly navigateToSignedOut: () => void;
}

export interface ApplicationLifecycle {
  dispose(): void;
}

export function createApplicationLifecycle(
  deps: ApplicationLifecycleDependencies
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

    deps.clearSessionState();
    deps.clearWorkspaceState();
    deps.queryClient.clear();
    deps.realtime.disconnect('session-expired');
    deps.navigateToSignedOut();
  };

  const handleWorkspaceChange = (event: WorkspaceChangeEvent): void => {
    if (disposed || event.nextWorkspaceId === event.previousWorkspaceId) {
      return;
    }
    if (event.nextWorkspaceId === activeWorkspaceId) {
      return;
    }

    activeWorkspaceId = event.nextWorkspaceId;
    deps.queryClient.clear();
    deps.clearWorkspaceState();
    deps.realtime.disconnect('workspace-switch');
  };

  const unsubscribeSession = deps.sessionEvents.subscribe(handleSessionExpired);
  const unsubscribeWorkspace = deps.workspaceEvents.subscribe(handleWorkspaceChange);

  return {
    dispose(): void {
      if (disposed) {
        return;
      }
      disposed = true;
      unsubscribeSession();
      unsubscribeWorkspace();
      handledSessionEventIds.clear();
      activeWorkspaceId = null;
    },
  };
}
