import { useQuery } from '@tanstack/react-query';
import { createMembersService } from '../../api/members.service';
import type { WorkspaceApiClient } from '../../api/workspace.service';
import { workspaceQueryKeys } from '../keys';

interface UseWorkspaceMembersDeps {
  api: WorkspaceApiClient;
}

export function createUseWorkspaceMembers({ api }: UseWorkspaceMembersDeps) {
  const service = createMembersService(api);

  return function useWorkspaceMembers(workspaceId: string) {
    return useQuery({
      queryKey: workspaceQueryKeys.members(workspaceId),
      queryFn: () => service.getList(workspaceId),
      enabled: !!workspaceId,
    });
  };
}
