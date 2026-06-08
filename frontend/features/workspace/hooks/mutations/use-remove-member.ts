"use client"

import { useMutation, useQueryClient } from "@tanstack/react-query"
import { toast } from "sonner"
import { queryKeys } from "@/lib/query/query-keys"
import { membersApi } from "../../api/members.api"

export function useRemoveMember(workspaceId: string) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (userId: string) => membersApi.removeMember(workspaceId, userId),
    onSuccess: () => {
      toast.success("Member removed from workspace.")
      queryClient.invalidateQueries({ queryKey: queryKeys.workspaces.members(workspaceId) })
      queryClient.invalidateQueries({ queryKey: queryKeys.workspaces.snapshot(workspaceId) })
    },
    onError: () => {
      toast.error("Failed to remove member.")
    },
  })
}
