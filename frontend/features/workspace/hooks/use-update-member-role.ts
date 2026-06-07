"use client"

import { useMutation, useQueryClient } from "@tanstack/react-query"
import { toast } from "sonner"
import { queryKeys } from "@/lib/query/query-keys"
import { workspaceService } from "../api/workspace.service"

export function useUpdateMemberRole(workspaceId: string) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (input: { userId: string; role: string }) =>
      workspaceService.updateMemberRole(workspaceId, input.userId, input.role),
    onSuccess: () => {
      toast.success("Member role updated.")
      queryClient.invalidateQueries({ queryKey: queryKeys.workspaces.members(workspaceId) })
      queryClient.invalidateQueries({ queryKey: queryKeys.workspaces.snapshot(workspaceId) })
    },
    onError: () => {
      toast.error("Failed to update member role.")
    },
  })
}
