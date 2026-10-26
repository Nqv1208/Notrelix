import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  createWorkspaceInvitationsService,
  type WorkspaceInvitationsEndpoints,
} from "../../../core/api/invitations.service";
import type { WorkspaceApiClient } from "../../../core/api/workspace.service";
import { workspaceQueryKeys } from "../../../query/keys";
import type { CreateWorkspaceInvitationInput } from "../../../core/types/workspace";

interface InvitationMutationDeps {
  api: WorkspaceApiClient;
  endpoints: WorkspaceInvitationsEndpoints;
}

/** Invites by e-mail. Not retried: the producer rejects a second pending invite. */
export function createUseCreateInvitation({
  api,
  endpoints,
}: InvitationMutationDeps) {
  const service = createWorkspaceInvitationsService(api, endpoints);

  return function useCreateInvitation(workspaceId: string) {
    const queryClient = useQueryClient();

    return useMutation({
      mutationFn: (input: CreateWorkspaceInvitationInput) =>
        service.create(workspaceId, input),
      retry: false,
      onSettled: () =>
        queryClient.invalidateQueries({
          queryKey: workspaceQueryKeys.invitations(workspaceId),
        }),
    });
  };
}

/** Sends an invitation again (`Workspaces.Invitations.ResendInvitation`). */
export function createUseResendInvitation({
  api,
  endpoints,
}: InvitationMutationDeps) {
  const service = createWorkspaceInvitationsService(api, endpoints);

  return function useResendInvitation(workspaceId: string) {
    const queryClient = useQueryClient();

    return useMutation({
      mutationFn: (invitationId: string) =>
        service.resend(workspaceId, invitationId),
      retry: false,
      onSettled: () =>
        queryClient.invalidateQueries({
          queryKey: workspaceQueryKeys.invitations(workspaceId),
        }),
    });
  };
}

/** Revokes a pending invitation (`Workspaces.Invitations.CancelInvitation`). */
export function createUseRevokeInvitation({
  api,
  endpoints,
}: InvitationMutationDeps) {
  const service = createWorkspaceInvitationsService(api, endpoints);

  return function useRevokeInvitation(workspaceId: string) {
    const queryClient = useQueryClient();

    return useMutation({
      mutationFn: (invitationId: string) =>
        service.revoke(workspaceId, invitationId),
      onSettled: () =>
        queryClient.invalidateQueries({
          queryKey: workspaceQueryKeys.invitations(workspaceId),
        }),
    });
  };
}
