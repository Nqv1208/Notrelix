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

export interface UpdateMemberRoleInput {
  readonly userId: string;
  readonly role: WorkspaceRole;
}

/**
 * Member writes (WX-MEM-001). Role change is applied optimistically to the
 * member list and rolled back on failure; remove, suspend and activate wait
 * for the producer (destructive or not reflected in the list). Every write
 * re-reads the list afterwards: the producer is authoritative.
 */
export function createUseMemberMutations({ api }: MemberMutationDeps) {
  const service = createMembersService(api);

  return function useMemberMutations(workspaceId: string) {
    const queryClient = useQueryClient();
    const key = workspaceQueryKeys.members(workspaceId);
    const refresh = () => queryClient.invalidateQueries({ queryKey: key });

    const updateRole = useMutation({
      mutationFn: ({ userId, role }: UpdateMemberRoleInput) =>
        service.updateRole(workspaceId, userId, role),
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
      mutationFn: (userId: string) => service.removeMember(workspaceId, userId),
      onSettled: refresh,
    });

    const suspend = useMutation({
      mutationFn: (userId: string) => service.suspend(workspaceId, userId),
      onSettled: refresh,
    });

    const activate = useMutation({
      mutationFn: (userId: string) => service.activate(workspaceId, userId),
      onSettled: refresh,
    });

    return { updateRole, remove, suspend, activate };
  };
}
