import { useQuery } from '@tanstack/react-query';
import { createInvitationsService, type InvitationsEndpoints } from '../../../core/api/invitations.service';
import type { WorkspaceApiClient } from '../../../core/api/workspace.service';
import { workspaceQueryKeys } from '../../../core/query/keys';

interface UseWorkspaceInvitationsDeps {
  api: WorkspaceApiClient;
  endpoints: InvitationsEndpoints;
  options?: {
    mockMode?: boolean;
  };

}

export function createUseWorkspaceInvitations({ api, endpoints, options }: UseWorkspaceInvitationsDeps) {
  const service = createInvitationsService(api, endpoints, options);

  return function useWorkspaceInvitations(workspaceId: string) {
    return useQuery({
      queryKey: workspaceQueryKeys.invitations(workspaceId),
      queryFn: () => service.getPending(), // pending invitations scoped generally or in workspace (stubbed fallback)
      enabled: !!workspaceId,
    });
  };
}
