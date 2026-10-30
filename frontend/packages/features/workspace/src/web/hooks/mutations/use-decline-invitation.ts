import { useAccountId } from "@notrelix/query";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  createInvitationsService,
  type InvitationsEndpoints,
} from "../../../core/api/invitations.service";
import type { WorkspaceApiClient } from "../../../core/api/workspace.service";
import { workspaceQueryKeys } from "../../../query/keys";

interface UseDeclineInvitationDeps {
  api: WorkspaceApiClient;
  endpoints: InvitationsEndpoints;
}

/** Declines one of the signed-in person's own pending invitations by its id. */
export function createUseDeclineInvitation({
  api,
  endpoints,
}: UseDeclineInvitationDeps) {
  const service = createInvitationsService(api, endpoints);

  return function useDeclineInvitation() {
    const accountId = useAccountId();
    const queryClient = useQueryClient();

    return useMutation({
      mutationFn: (invitationId: string) => service.declineById(invitationId),
      // A gone invitation (404) leaves the list too: read it again either way.
      onSettled: () => {
        void queryClient.invalidateQueries({
          queryKey: workspaceQueryKeys.pendingInvitations(accountId),
        });
      },
    });
  };
}
