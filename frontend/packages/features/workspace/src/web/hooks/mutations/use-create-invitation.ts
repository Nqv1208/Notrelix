import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  createInvitationsService,
  type InvitationsEndpoints,
} from "../../../core/api/invitations.service";
import type { WorkspaceApiClient } from "../../../core/api/workspace.service";
import { workspaceQueryKeys } from "../../../core/query/keys";
import type { CreateWorkspaceInvitationInput } from "../../../core/types/workspace";

interface UseCreateInvitationDeps {
  api: WorkspaceApiClient;
  endpoints: InvitationsEndpoints;
}

export function createUseCreateInvitation({
  api,
  endpoints,
}: UseCreateInvitationDeps) {
  const service = createInvitationsService(api, endpoints);

  return function useCreateInvitation(workspaceId: string) {
    const queryClient = useQueryClient();

    return useMutation({
      mutationFn: (input: CreateWorkspaceInvitationInput) =>
        service.create(workspaceId, input),
      onSuccess: () => {
        queryClient.invalidateQueries({
          queryKey: workspaceQueryKeys.invitations(workspaceId),
        });
      },
    });
  };
}
