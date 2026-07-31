import { useQuery } from '@tanstack/react-query';
import { createInvitationsService, type InvitationsEndpoints } from '../../../core/api/invitations.service';
import type { WorkspaceApiClient } from '../../../core/api/workspace.service';
import { workspaceQueryKeys } from '../../../core/query/keys';

interface UsePendingInvitationsDeps {
  api: WorkspaceApiClient;
  endpoints: InvitationsEndpoints;
  options?: {
    mockMode?: boolean;
  };

}

export function createUsePendingInvitations({ api, endpoints, options }: UsePendingInvitationsDeps) {
  const service = createInvitationsService(api, endpoints, options);

  return function usePendingInvitations() {
    return useQuery({
      queryKey: workspaceQueryKeys.pendingInvitations,
      queryFn: () => service.getPending(),
    });
  };
}
