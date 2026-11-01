import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useRef } from "react";
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

/**
 * Invites by e-mail. Not retried automatically. One key per invitation the person asked for: sending
 * the same address and role again after a failure reuses it, so the producer returns the first
 * invitation instead of sending a second email; a different address or role is a new request.
 */
export function createUseCreateInvitation({
  api,
  endpoints,
}: InvitationMutationDeps) {
  const service = createWorkspaceInvitationsService(api, endpoints);

  return function useCreateInvitation(workspaceId: string) {
    const queryClient = useQueryClient();
    const attempt = useRef<{ signature: string; key: string } | null>(null);

    return useMutation({
      mutationFn: (input: CreateWorkspaceInvitationInput) => {
        const signature = JSON.stringify([
          workspaceId,
          input.email.trim().toLowerCase(),
          input.role,
        ]);
        if (attempt.current?.signature !== signature)
          attempt.current = { signature, key: crypto.randomUUID() };
        return service.create(workspaceId, input, attempt.current.key);
      },
      retry: false,
      onSuccess: () => {
        attempt.current = null;
      },
      onSettled: () =>
        queryClient.invalidateQueries({
          queryKey: workspaceQueryKeys.invitations(workspaceId),
        }),
    });
  };
}

/** Changes the role a waiting invitation offers (`Workspaces.Invitations.ChangeInvitationRole`). */
export function createUseChangeInvitationRole({
  api,
  endpoints,
}: InvitationMutationDeps) {
  const service = createWorkspaceInvitationsService(api, endpoints);

  return function useChangeInvitationRole(workspaceId: string) {
    const queryClient = useQueryClient();

    return useMutation({
      mutationFn: (input: {
        invitationId: string;
        role: CreateWorkspaceInvitationInput["role"];
        expectedVersion: number | undefined;
      }) =>
        service.changeRole(
          workspaceId,
          input.invitationId,
          input.role,
          input.expectedVersion,
        ),
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
