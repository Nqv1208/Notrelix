import { useQuery } from '@tanstack/react-query';
import { createInvitationsService, type InvitationsEndpoints } from '../../api/invitations.service';
import type { WorkspaceApiClient } from '../../api/workspace.service';
import { workspaceQueryKeys } from '../keys';

interface UsePendingInvitationsDeps {
  api: WorkspaceApiClient;
  endpoints: InvitationsEndpoints;
}

export function createUsePendingInvitations({ api, endpoints }: UsePendingInvitationsDeps) {
  const service = createInvitationsService(api, endpoints);

  return function usePendingInvitations() {
    return useQuery({
      queryKey: workspaceQueryKeys.pendingInvitations,
      queryFn: () => service.getPending(),
    });
  };
}
