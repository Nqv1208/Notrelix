"use client"

import { useMutation, useQueryClient } from "@tanstack/react-query"
import { toast } from "sonner"
import { queryKeys } from "@/lib/query/query-keys"
import { invitationsApi } from "../../api/invitations.api"
import type { CreateWorkspaceInvitationInput } from "../../types"

export function useCreateInvitation(workspaceId: string) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (input: CreateWorkspaceInvitationInput) =>
      invitationsApi.createInvitation(workspaceId, input),
    onSuccess: () => {
      toast.success("Invitation sent successfully.")
      queryClient.invalidateQueries({ queryKey: queryKeys.workspaces.invitations(workspaceId) })
    },
    onError: () => {
      toast.error("Failed to send invitation.")
    },
  })
}
