"use client"

import { useMutation, useQueryClient } from "@tanstack/react-query"
import { queryKeys } from "@/lib/query/query-keys"
import { viewsApi } from "../../api/views.api"
import type { CreateWorkspaceViewInput } from "../../types"

export function useCreateWorkspaceView() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (input: CreateWorkspaceViewInput) => viewsApi.createView(input),
    onSuccess: (view) => {
      queryClient.invalidateQueries({ queryKey: queryKeys.workspaces.detail(view.workspaceId) })
      queryClient.invalidateQueries({ queryKey: queryKeys.workspaces.views(view.workspaceId) })
      queryClient.invalidateQueries({ queryKey: queryKeys.workspaces.snapshot(view.workspaceId) })
    },
  })
}
