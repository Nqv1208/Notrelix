"use client"

import { useMutation, useQueryClient } from "@tanstack/react-query"
import { toast } from "sonner"
import { queryKeys } from "@/lib/query/query-keys"
import { invitationsApi, type AcceptInvitationResponse } from "../../api/invitations.api"

export function useAcceptInvitation() {
  const queryClient = useQueryClient()

  return useMutation<AcceptInvitationResponse, Error, string>({
    mutationFn: (token: string) => invitationsApi.acceptInvitation(token),
    onSuccess: () => {
      toast.success("Đã chấp nhận lời mời tham gia Workspace thành công!")
      queryClient.invalidateQueries({ queryKey: queryKeys.workspaces.all })
      queryClient.invalidateQueries({ queryKey: queryKeys.workspaces.pendingInvitations })
    },
    onError: (error: unknown) => {
      toast.error(getInvitationErrorMessage(error))
    },
  })
}

function getInvitationErrorMessage(error: unknown) {
  if (error instanceof Error) return error.message
  return "Không thể chấp nhận lời mời."
}
