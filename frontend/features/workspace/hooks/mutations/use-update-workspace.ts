"use client"

import { useMutation, useQueryClient } from "@tanstack/react-query"
import { toast } from "sonner"
import { queryKeys } from "@/lib/query/query-keys"
import { workspaceApi } from "../../api/workspace.api"
import type { UpdateWorkspaceInput } from "../../types"

export function useUpdateWorkspace(workspaceId: string) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (input: UpdateWorkspaceInput) => workspaceApi.updateWorkspace(workspaceId, input),
    onSuccess: () => {
      toast.success("Workspace updated successfully.")
      queryClient.invalidateQueries({ queryKey: queryKeys.workspaces.detail(workspaceId) })
      queryClient.invalidateQueries({ queryKey: queryKeys.workspaces.snapshot(workspaceId) })
      queryClient.invalidateQueries({ queryKey: queryKeys.workspaces.all })
    },
    onError: () => {
      toast.error("Failed to update workspace.")
    },
  })
}
