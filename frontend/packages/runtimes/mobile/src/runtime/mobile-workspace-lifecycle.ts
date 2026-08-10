import type { QueryClient } from "@tanstack/react-query";
import type { RealtimeTransport } from "@notrelix/realtime";

export interface MobileWorkspaceLifecycle {
  getActiveWorkspaceId(): string | null;
  prepareWorkspaceTransition(nextWorkspaceId: string | null): void;
}

export function createMobileWorkspaceLifecycle(deps: {
  queryClient: QueryClient;
  realtime: RealtimeTransport;
}): MobileWorkspaceLifecycle {
  let activeWorkspaceId: string | null = null;

  return {
    getActiveWorkspaceId() {
      return activeWorkspaceId;
    },

    prepareWorkspaceTransition(nextWorkspaceId: string | null) {
      if (nextWorkspaceId === activeWorkspaceId) {
        return;
      }

      if (activeWorkspaceId !== null && nextWorkspaceId !== null) {
        deps.queryClient.clear();
        deps.realtime.disconnect("workspace-switch");
      }

      activeWorkspaceId = nextWorkspaceId;
    },
  };
}
