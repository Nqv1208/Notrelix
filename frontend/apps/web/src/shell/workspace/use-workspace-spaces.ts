import { useMemo } from "react";
import {
  createUseSpaceMembers,
  createUseSpaceMutations,
  createUseSpaceTree,
  createUseTeamsMembers,
  createUseWorkspaceTeams,
  type WorkspaceSpacesCopy,
} from "@notrelix/features-workspace/web";
import type { WorkspaceFailure } from "@notrelix/features-workspace/core";
import { useAppRuntime } from "@notrelix/runtime-web";

/** The Space producer hooks (Workspaces.Spaces.*), built once per runtime. */
export function useSpaceHooks() {
  const { api: runtimeClient } = useAppRuntime();
  return useMemo(() => {
    const deps = { api: runtimeClient.api, endpoints: runtimeClient.endpoints };
    return {
      useSpaceTree: createUseSpaceTree(deps),
      useSpaceMembers: createUseSpaceMembers(deps),
      useWorkspaceTeams: createUseWorkspaceTeams(deps),
      useTeamsMembers: createUseTeamsMembers(deps),
      useSpaceMutations: createUseSpaceMutations(deps),
    };
  }, [runtimeClient]);
}

/** Recovery hint for a failed Space write (DESIGN.md §34). */
export function spaceRecoveryHint(copy: WorkspaceSpacesCopy) {
  return (failure: WorkspaceFailure): string => {
    switch (failure.kind) {
      case "conflict":
        return copy.toasts.conflict;
      case "forbidden":
        return copy.toasts.forbidden;
      case "not-found":
        return copy.toasts.notFound;
      case "network":
        return copy.toasts.network;
      default:
        return copy.toasts.unknown;
    }
  };
}
