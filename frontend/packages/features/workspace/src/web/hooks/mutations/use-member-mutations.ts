import { useMutation, useQueryClient } from "@tanstack/react-query";
import { createMembersService } from "../../../core/api/members.service";
import type { WorkspaceApiClient } from "../../../core/api/workspace.service";
import type {
  WorkspaceMember,
  WorkspaceRole,
} from "../../../core/types/workspace";
import { workspaceQueryKeys } from "../../../query/keys";

interface MemberMutationDeps {
  api: WorkspaceApiClient;
}

/** The member a write acts on, with the version it was read at. */
export interface MemberWriteTarget {
  readonly userId: string;
  readonly expectedVersion: number | undefined;
}

export interface UpdateMemberRoleInput extends MemberWriteTarget {
  readonly role: WorkspaceRole;
}

/**
 * Member writes (WX-MEM-001). Role change is applied optimistically to the
 * member list and rolled back on failure; remove, suspend and activate wait
 * for the producer (destructive or not reflected in the list). Every write
 * quotes the member's version, so a decision made on a stale row is refused
 * (conflict) instead of overwriting another admin's change, and every write
 * re-reads the list afterwards: the producer is authoritative.
 */
export function createUseMemberMutations({ api }: MemberMutationDeps) {
  const service = createMembersService(api);

  return function useMemberMutations(workspaceId: string) {
    const queryClient = useQueryClient();
    const key = workspaceQueryKeys.members(workspaceId);
    const refresh = () => queryClient.invalidateQueries({ queryKey: key });

    const updateRole = useMutation({
      mutationFn: ({ userId, role, expectedVersion }: UpdateMemberRoleInput) =>
        service.updateRole(workspaceId, userId, role, expectedVersion),
      onMutate: async ({ userId, role }: UpdateMemberRoleInput) => {
        await queryClient.cancelQueries({ queryKey: key });
        const previous = queryClient.getQueryData<WorkspaceMember[]>(key);
        if (previous) {
          queryClient.setQueryData<WorkspaceMember[]>(
            key,
            previous.map((member) =>
              member.userId === userId ? { ...member, role } : member,
            ),
          );
        }
        return { previous };
      },
      onError: (_error, _input, context) => {
        if (context?.previous) queryClient.setQueryData(key, context.previous);
      },
      onSettled: refresh,
    });

    const remove = useMutation({
      mutationFn: ({ userId, expectedVersion }: MemberWriteTarget) =>
        service.removeMember(workspaceId, userId, expectedVersion),
      onSettled: refresh,
    });

    const suspend = useMutation({
      mutationFn: ({ userId, expectedVersion }: MemberWriteTarget) =>
        service.suspend(workspaceId, userId, expectedVersion),
      onSettled: refresh,
    });

    const activate = useMutation({
      mutationFn: ({ userId, expectedVersion }: MemberWriteTarget) =>
        service.activate(workspaceId, userId, expectedVersion),
      onSettled: refresh,
    });

    return { updateRole, remove, suspend, activate };
  };
}
