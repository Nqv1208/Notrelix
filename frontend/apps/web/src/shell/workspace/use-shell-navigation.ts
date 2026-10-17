import { useCallback, useMemo } from "react";
import { useNavigate } from "@tanstack/react-router";
import type { WorkspaceDestination } from "@notrelix/features-workspace/core";
import type { ShellNavigation } from "@notrelix/features-workspace/web";
import { useWorkspaceContext } from "@/providers/workspace-provider";
import { destinationHref, routerTarget } from "./workspace-destinations";

/** The host navigation port for workspace surfaces (real hrefs + router). */
export function useShellNavigation(): ShellNavigation {
  const { workspaceId } = useWorkspaceContext();
  const navigate = useNavigate();
  const onNavigate = useCallback(
    (destination: WorkspaceDestination) => {
      const target = routerTarget(workspaceId, destination);
      void navigate({
        to: target.to,
        params: target.params,
        search: target.search,
      } as never);
    },
    [navigate, workspaceId],
  );
  return useMemo(
    () => ({
      getHref: (destination) => destinationHref(workspaceId, destination),
      onNavigate,
    }),
    [onNavigate, workspaceId],
  );
}
