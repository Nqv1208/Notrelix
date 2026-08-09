import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  createInvitationsService,
  type InvitationsEndpoints,
} from "../../../core/api/invitations.service";
import type { WorkspaceApiClient } from "../../../core/api/workspace.service";
import { workspaceQueryKeys } from "../../../core/query/keys";

interface UseAcceptInvitationDeps {
  api: WorkspaceApiClient;
  endpoints: InvitationsEndpoints;
}

export function createUseAcceptInvitation({
  api,
  endpoints,
}: UseAcceptInvitationDeps) {
  const service = createInvitationsService(api, endpoints);

  return function useAcceptInvitation() {
    const queryClient = useQueryClient();

    return useMutation({
      mutationFn: service.accept,
      onSuccess: () => {
        // Accept invitation changes all workspaces, so clear/invalidate workspaces
        queryClient.invalidateQueries({ queryKey: workspaceQueryKeys.all });
        queryClient.invalidateQueries({
          queryKey: workspaceQueryKeys.pendingInvitations,
        });
      },
    });
  };
}
