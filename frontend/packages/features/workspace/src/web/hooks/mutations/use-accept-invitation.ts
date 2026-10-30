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
      // A refusal too (gone, no free seat) changes what the list should say: read it again.
      onSettled: () => {
        void queryClient.invalidateQueries({
          queryKey: workspaceQueryKeys.pendingInvitations(accountId),
        });
      },
      // A new membership changes the person's workspaces (the rail lists them).
      onSuccess: () => {
        void queryClient.invalidateQueries({
          queryKey: workspaceQueryKeys.all(accountId),
        });
      },
    });
  };
}
