import { useQuery } from "@tanstack/react-query";
import { createMembersService } from "../../../core/api/members.service";
import type { WorkspaceApiClient } from "../../../core/api/workspace.service";
import { workspaceQueryKeys } from "../../../query/keys";

interface UseWorkspaceMembersDeps {
  api: WorkspaceApiClient;
}

interface UseWorkspaceMembersOptions {
  /**
   * The member list is closed to a Guest (`ViewMembers`); the caller passes
   * `false` for a Guest and until the role is known.
   */
  enabled?: boolean;
}

export function createUseWorkspaceMembers({ api }: UseWorkspaceMembersDeps) {
  const service = createMembersService(api);

  return function useWorkspaceMembers(
    workspaceId: string,
    options: UseWorkspaceMembersOptions = {},
  ) {
    return useQuery({
      queryKey: workspaceQueryKeys.members(workspaceId),
      queryFn: () => service.getList(workspaceId),
      enabled: !!workspaceId && (options.enabled ?? true),
    });
  };
}

/** The caller's own membership (role, status, name): readable by every member, a Guest included. */
export function createUseMyWorkspaceMembership({
  api,
}: UseWorkspaceMembersDeps) {
  const service = createMembersService(api);

  return function useMyWorkspaceMembership(workspaceId: string) {
    return useQuery({
      queryKey: workspaceQueryKeys.myMembership(workspaceId),
      queryFn: () => service.getMine(workspaceId),
      enabled: !!workspaceId,
    });
  };
}
