import type { QueryClient } from "@tanstack/react-query";
import { authQueryKeys } from "@notrelix/features-auth";
import { resetCacheForAccountTransition } from "@notrelix/query";
import type { RealtimeTransport } from "@notrelix/realtime";
import type {
  SessionEventBus,
  SessionExpiredEvent,
} from "@notrelix/runtime-web";

const MAX_HANDLED_SESSION_EVENTS = 32;

/**
 * Drops every cached read and mutation. The session profile query is kept as
 * the same query so the auth provider's observer stays on it: a removed query
 * would leave the provider waiting on an orphan ("Verifying session") after
 * the next sign-in. It is reset when the session ended and left alone when
 * only the workspace changed.
 */
export function clearApplicationCache(
  queryClient: QueryClient,
  { sessionEnded }: { readonly sessionEnded: boolean },
): void {
  const cache = queryClient.getQueryCache();
  const profile = cache.find({ queryKey: authQueryKeys.profile, exact: true });
  for (const query of cache.getAll()) {
    if (query !== profile) cache.remove(query);
  }
  queryClient.getMutationCache().clear();
  if (sessionEnded) profile?.reset();
}

export interface ApplicationLifecycleDependencies {
  readonly queryClient: QueryClient;
  readonly realtime: RealtimeTransport;
  readonly sessionEvents: SessionEventBus;
  readonly navigateToSignedOut: () => void;
}

export interface ApplicationLifecycle {
  /**
   * Principal-scope transition. TEMPORARY MODEL: Account equals the
   * authenticated principal until the backend exposes an Account ID and a
   * switch signal. A change between two different principals drops the whole
   * cache and in-flight requests (FE-STATE-038/039).
   */
  prepareAccountTransition(nextAccountId: string | null): void;
  prepareWorkspaceTransition(nextWorkspaceId: string | null): void;
  dispose(): void;
}

export function createApplicationLifecycle(
  deps: ApplicationLifecycleDependencies,
): ApplicationLifecycle {
  const handledSessionEventIds: Set<string> = new Set();
  let activeWorkspaceId: string | null = null;
  let activeAccountId: string | null = null;
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

    clearApplicationCache(deps.queryClient, { sessionEnded: true });
    deps.realtime.disconnect("session-expired");
    deps.navigateToSignedOut();
  };

  const unsubscribeSession = deps.sessionEvents.subscribe(handleSessionExpired);

  return {
    prepareAccountTransition(nextAccountId: string | null): void {
      if (disposed) return;

      // Sign-out (null) is handled by the session/logout paths that clear the
      // cache; the last known principal is kept so a different principal
      // signing in afterwards is still detected.
      if (nextAccountId === null) return;

      const previous = activeAccountId;
      activeAccountId = nextAccountId;
      if (previous === null || previous === nextAccountId) {
        return;
      }

      resetCacheForAccountTransition(deps.queryClient);
      deps.realtime.disconnect("account-switch");
      activeWorkspaceId = null;
    },

    prepareWorkspaceTransition(nextWorkspaceId: string | null): void {
      if (disposed) return;

      if (activeWorkspaceId === null) {
        activeWorkspaceId = nextWorkspaceId;
        return;
      }

      if (activeWorkspaceId === nextWorkspaceId) {
        return;
      }

      clearApplicationCache(deps.queryClient, { sessionEnded: false });
      deps.realtime.disconnect("workspace-switch");
      activeWorkspaceId = nextWorkspaceId;
    },

    dispose(): void {
      if (disposed) return;
      disposed = true;
      unsubscribeSession();
      handledSessionEventIds.clear();
      activeWorkspaceId = null;
      activeAccountId = null;
    },
  };
}
