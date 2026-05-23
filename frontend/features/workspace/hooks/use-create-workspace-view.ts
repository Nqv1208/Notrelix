"use client"

import { useMutation, useQueryClient } from "@tanstack/react-query"
import { queryKeys } from "@/lib/query/query-keys"
import { workspaceService } from "../api/workspace.service"
import type { CreateWorkspaceViewInput } from "../types"

export function useCreateWorkspaceView() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (input: CreateWorkspaceViewInput) => workspaceService.createView(input),
    onSuccess: (view) => {
      queryClient.invalidateQueries({ queryKey: queryKeys.workspaces.views(view.workspaceId) })
      queryClient.invalidateQueries({ queryKey: queryKeys.workspaces.snapshot(view.workspaceId) })
    },
  })
}
