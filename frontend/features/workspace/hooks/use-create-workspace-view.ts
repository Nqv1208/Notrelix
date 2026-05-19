"use client"

import { useMutation, useQueryClient } from "@tanstack/react-query"
import { queryKeys } from "@/lib/query/query-keys"
import { workspacesApi } from "../api/workspaces-api"
import type { CreateWorkspaceViewInput } from "../types"

export function useCreateWorkspaceView() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (input: CreateWorkspaceViewInput) => workspacesApi.createView(input),
    onSuccess: (view) => {
      queryClient.invalidateQueries({ queryKey: queryKeys.workspaces.views(view.workspaceSlug) })
      queryClient.invalidateQueries({ queryKey: queryKeys.workspaces.snapshot(view.workspaceSlug) })
    },
  })
}
