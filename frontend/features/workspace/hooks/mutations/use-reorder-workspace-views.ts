"use client"

import { useMutation, useQueryClient } from "@tanstack/react-query"
import { toast } from "sonner"
import { queryKeys } from "@/lib/query/query-keys"
import { viewsApi } from "../../api/views.api"

export function useReorderWorkspaceViews(workspaceId: string) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (orderedViewIds: string[]) => viewsApi.reorderViews(workspaceId, orderedViewIds),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.workspaces.detail(workspaceId) })
      queryClient.invalidateQueries({ queryKey: queryKeys.workspaces.views(workspaceId) })
      queryClient.invalidateQueries({ queryKey: queryKeys.workspaces.snapshot(workspaceId) })
    },
    onError: (error: unknown) => {
      toast.error(getErrorMessage(error) || "Không thể sắp xếp lại các view.")
    },
  })
}

function getErrorMessage(error: unknown) {
  return error instanceof Error ? error.message : undefined
}
