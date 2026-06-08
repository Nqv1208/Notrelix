"use client"

import { useMutation, useQueryClient } from "@tanstack/react-query"
import { queryKeys } from "@/lib/query/query-keys"
import { viewsApi } from "../../api/views.api"
import type { UpdateWorkspaceViewInput, WorkspaceView } from "../../types"

export function useUpdateWorkspaceView(workspaceId: string) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ viewId, input }: { viewId: string; input: UpdateWorkspaceViewInput }) =>
      viewsApi.updateView(workspaceId, viewId, input),
    onMutate: async ({ viewId, input }) => {
      await queryClient.cancelQueries({ queryKey: queryKeys.workspaces.views(workspaceId) })
      const previous = queryClient.getQueryData(queryKeys.workspaces.views(workspaceId))
      queryClient.setQueryData(queryKeys.workspaces.views(workspaceId), (old: WorkspaceView[] | undefined) => {
        if (!old) return old
        return old.map((view) => {
          if (view.id !== viewId) return view
          return { ...view, ...input, config: { ...view.config, ...input.config } }
        })
      })
      return { previous }
    },
    onError: (_error, _variables, context) => {
      queryClient.setQueryData(queryKeys.workspaces.views(workspaceId), context?.previous)
    },
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.workspaces.detail(workspaceId) })
      queryClient.invalidateQueries({ queryKey: queryKeys.workspaces.views(workspaceId) })
      queryClient.invalidateQueries({ queryKey: queryKeys.workspaces.snapshot(workspaceId) })
    },
  })
}
