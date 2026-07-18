import { useQuery } from '@tanstack/react-query';
import { createMembersService } from '~/core/api/members.service';
import type { WorkspaceApiClient } from '~/core/api/workspace.service';
import { workspaceQueryKeys } from '../keys';

interface UseWorkspaceMembersDeps {
  api: WorkspaceApiClient;
  options?: {
    mockMode?: boolean;
  };
}

export function createUseWorkspaceMembers({ api, options }: UseWorkspaceMembersDeps) {
  const service = createMembersService(api, options);

  return function useWorkspaceMembers(workspaceId: string) {
    return useQuery({
      queryKey: workspaceQueryKeys.members(workspaceId),
      queryFn: () => service.getList(workspaceId),
      enabled: !!workspaceId,
    });
  };
}
