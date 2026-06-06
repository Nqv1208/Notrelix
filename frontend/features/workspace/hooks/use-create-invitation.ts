"use client"

import { useMutation, useQueryClient } from "@tanstack/react-query"
import { toast } from "sonner"
import { queryKeys } from "@/lib/query/query-keys"
import { workspaceService } from "../api/workspace.service"

export function useCreateInvitation(workspaceId: string) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (input: { email: string; role: string }) =>
      workspaceService.createInvitation(workspaceId, input.email, input.role),
    onSuccess: () => {
      toast.success("Invitation sent successfully.")
      queryClient.invalidateQueries({ queryKey: queryKeys.workspaces.invitations(workspaceId) })
    },
    onError: () => {
      toast.error("Failed to send invitation.")
    },
  })
}
