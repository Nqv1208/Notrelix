import { useAccountId } from "@notrelix/query";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  createInvitationsService,
  type InvitationsEndpoints,
} from "../../../core/api/invitations.service";
import type { WorkspaceApiClient } from "../../../core/api/workspace.service";
import { workspaceQueryKeys } from "../../../query/keys";

interface UseAcceptInvitationDeps {
  api: WorkspaceApiClient;
  endpoints: InvitationsEndpoints;
}

/** Accepts one of the signed-in person's own pending invitations by its id. */
export function createUseAcceptInvitation({
  api,
  endpoints,
}: UseAcceptInvitationDeps) {
  const service = createInvitationsService(api, endpoints);

  return function useAcceptInvitation() {
    const accountId = useAccountId();
    const queryClient = useQueryClient();

    return useMutation({
      mutationFn: (invitationId: string) => service.acceptById(invitationId),
      // Whatever the answer, a lost reply included: the invitation may be answered and the person already a
      // member, so the pending list and the person's workspaces (the rail lists them) are read again.
      onSettled: () => {
        void queryClient.invalidateQueries({
          queryKey: workspaceQueryKeys.pendingInvitations(accountId),
        });
        void queryClient.invalidateQueries({
          queryKey: workspaceQueryKeys.all(accountId),
        });
      },
    });
  };
}
