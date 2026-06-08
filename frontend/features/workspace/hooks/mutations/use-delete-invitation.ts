"use client"

import { useMutation, useQueryClient } from "@tanstack/react-query"
import { toast } from "sonner"
import { queryKeys } from "@/lib/query/query-keys"
import { invitationsApi } from "../../api/invitations.api"

export function useDeleteInvitation(workspaceId: string) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (invitationId: string) => invitationsApi.deleteInvitation(workspaceId, invitationId),
    onSuccess: () => {
      toast.success("Invitation cancelled.")
      queryClient.invalidateQueries({ queryKey: queryKeys.workspaces.invitations(workspaceId) })
    },
    onError: () => {
      toast.error("Failed to cancel invitation.")
    },
  })
}
