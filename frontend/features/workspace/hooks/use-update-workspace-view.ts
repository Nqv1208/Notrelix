"use client"

import { useMutation, useQueryClient } from "@tanstack/react-query"
import { queryKeys } from "@/lib/query/query-keys"
import { workspacesApi } from "../api/workspaces-api"
import type { UpdateWorkspaceViewInput, WorkspaceView } from "../types"

export function useUpdateWorkspaceView(slug: string) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ viewId, input }: { viewId: string; input: UpdateWorkspaceViewInput }) =>
      workspacesApi.updateView(slug, viewId, input),
    onMutate: async ({ viewId, input }) => {
      await queryClient.cancelQueries({ queryKey: queryKeys.workspaces.views(slug) })
      const previous = queryClient.getQueryData(queryKeys.workspaces.views(slug))
      queryClient.setQueryData(queryKeys.workspaces.views(slug), (old: WorkspaceView[] | undefined) => {
        if (!old) return old
        return old.map((view) => {
          if (view.id !== viewId) return view
          return { ...view, ...input, config: { ...view.config, ...input.config } }
        })
      })
      return { previous }
    },
    onError: (_error, _variables, context) => {
      queryClient.setQueryData(queryKeys.workspaces.views(slug), context?.previous)
    },
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.workspaces.views(slug) })
      queryClient.invalidateQueries({ queryKey: queryKeys.workspaces.snapshot(slug) })
    },
  })
}
