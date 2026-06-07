"use client"

import { useMutation, useQueryClient } from "@tanstack/react-query"
import { queryKeys } from "@/lib/query/query-keys"
import { workspaceService } from "../api/workspace.service"
import { toast } from "sonner"

export function useReorderWorkspaceViews(workspaceId: string) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (orderedViewIds: string[]) =>
      workspaceService.reorderViews(workspaceId, orderedViewIds),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.workspaces.views(workspaceId) })
      queryClient.invalidateQueries({ queryKey: queryKeys.workspaces.snapshot(workspaceId) })
    },
    onError: (err: any) => {
      toast.error(err?.message || "Không thể sắp xếp lại các view.")
    }
  })
}
