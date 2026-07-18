import { useMutation, useQueryClient } from '@tanstack/react-query';
import { createMembersService } from '~/core/api/members.service';
import type { WorkspaceApiClient } from '~/core/api/workspace.service';
import { workspaceQueryKeys } from '~/core/query/keys';
import type { WorkspaceMember } from '~/core/types/workspace';

interface UseUpdateMemberRoleDeps {
  api: WorkspaceApiClient;
}

export function createUseUpdateMemberRole({ api }: UseUpdateMemberRoleDeps) {
  const service = createMembersService(api);

  return function useUpdateMemberRole(workspaceId: string) {
    const queryClient = useQueryClient();

    return useMutation({
      mutationFn: ({ userId, role }: { userId: string; role: WorkspaceMember['role'] }) =>
        service.updateRole(workspaceId, userId, role),
      onSuccess: () => {
        queryClient.invalidateQueries({ queryKey: workspaceQueryKeys.members(workspaceId) });
      },
    });
  };
}
