import { useMutation, useQueryClient } from '@tanstack/react-query';
import { createInvitationsService, type InvitationsEndpoints } from '~/core/api/invitations.service';
import type { WorkspaceApiClient } from '~/core/api/workspace.service';
import { workspaceQueryKeys } from '~/core/query/keys';

interface UseDeleteInvitationDeps {
  api: WorkspaceApiClient;
  endpoints: InvitationsEndpoints;
}

export function createUseDeleteInvitation({ api, endpoints }: UseDeleteInvitationDeps) {
  const service = createInvitationsService(api, endpoints);

  return function useDeleteInvitation(workspaceId: string) {
    const queryClient = useQueryClient();

    return useMutation({
      mutationFn: (invitationId: string) => service.delete(workspaceId, invitationId),
      onSuccess: () => {
        queryClient.invalidateQueries({ queryKey: workspaceQueryKeys.invitations(workspaceId) });
      },
    });
  };
}
