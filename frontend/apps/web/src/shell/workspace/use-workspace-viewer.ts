import { useMemo } from "react";
import { useCurrentUser } from "@notrelix/features-auth";
import { createUseWorkspaceMembers } from "@notrelix/features-workspace/web";
import type { WorkspaceRole } from "@notrelix/features-workspace/core";
import { useAppRuntime } from "@notrelix/runtime-web";

/**
 * The signed-in member and their role in this workspace, from the member
 * list (Workspaces). The role only shapes what the UI offers; the producer
 * authorizes every request (FE-AGENTS §40).
 */
export function useWorkspaceViewer(workspaceId: string) {
  const user = useCurrentUser();
  const { api: runtimeClient } = useAppRuntime();
  const useMembers = useMemo(
    () => createUseWorkspaceMembers({ api: runtimeClient.api }),
    [runtimeClient],
  );
  const members = useMembers(workspaceId);
  const role: WorkspaceRole | null =
    members.data?.find((member) => member.userId === user?.id)?.role ?? null;
  return { userId: user?.id ?? null, role, members };
}
