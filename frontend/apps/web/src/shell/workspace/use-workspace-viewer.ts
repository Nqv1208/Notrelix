import { useCallback, useMemo } from "react";
import { useQuery } from "@tanstack/react-query";
import { workspaceQueryKey } from "@notrelix/query";
import { useCurrentUser } from "@notrelix/features-auth";
import {
  createUseMyWorkspaceMembership,
  createUseWorkspaceMembers,
} from "@notrelix/features-workspace/web";
import type { WorkspaceRole } from "@notrelix/features-workspace/core";
import { useAppRuntime } from "@notrelix/runtime-web";

/**
 * The signed-in member and their role in this workspace, from their own
 * membership row (`members/me`, readable by every member including a Guest).
 * The member list is a second read made only when the role may list people:
 * a Guest is denied `ViewMembers` and never requests it, and names of other
 * people come from the resources shared with them. The role only shapes what
 * the UI offers; the producer authorizes every request (FE-AGENTS §40).
 */
export function useWorkspaceViewer(workspaceId: string) {
  const user = useCurrentUser();
  const { api: runtimeClient } = useAppRuntime();
  const hooks = useMemo(
    () => ({
      useMine: createUseMyWorkspaceMembership({ api: runtimeClient.api }),
      useMembers: createUseWorkspaceMembers({ api: runtimeClient.api }),
    }),
    [runtimeClient],
  );
  const mine = hooks.useMine(workspaceId);
  const role: WorkspaceRole | null = mine.data?.role ?? null;
  // Not before the role is known, and never for a Guest.
  const canListMembers = role !== null && role !== "guest";
  const members = hooks.useMembers(workspaceId, { enabled: canListMembers });

  // What a custom role removes from the built-in role (owner decision 2026-10-31). Shapes the UI only; the
  // producer authorizes every request. A change to the person's access arrives as a realtime
  // "access-changed" push that revalidates the workspace scope at once; the slow poll and the focus refetch
  // only back it up (Redis pub/sub is at-most-once).
  const access = useQuery({
    queryKey: workspaceQueryKey(
      workspaceId,
      "workspace",
      "members",
      "me",
      "access",
    ),
    queryFn: () =>
      runtimeClient.api.get<{ removedActions?: string[] | null }>(
        `/workspaces/${workspaceId}/members/me/access`,
      ),
    enabled: role !== null,
    retry: false,
    refetchInterval: 60_000,
    refetchOnWindowFocus: true,
  });
  const removed = useMemo(
    () => new Set(access.data?.removedActions ?? []),
    [access.data],
  );
  const can = useCallback((action: string) => !removed.has(action), [removed]);
  return {
    userId: user?.id ?? null,
    role,
    mine,
    canListMembers,
    members,
    can,
  };
}
