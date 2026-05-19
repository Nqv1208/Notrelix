"use client"

import { useMutation, useQueryClient } from "@tanstack/react-query"
import { queryKeys } from "@/lib/query/query-keys"
import { pagesApi } from "../api/pages-api"
import type { PageDetail, UpdatePagePayload } from "../types"

export function useUpdatePage(pageId: string) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (payload: UpdatePagePayload) => pagesApi.update(pageId, payload),
    onMutate: async (payload) => {
      await queryClient.cancelQueries({ queryKey: queryKeys.pages.detail(pageId) })
      const previous = queryClient.getQueryData<PageDetail>(queryKeys.pages.detail(pageId))
      queryClient.setQueryData<PageDetail>(queryKeys.pages.detail(pageId), (old) =>
        old ? { ...old, ...payload, updatedAt: new Date().toISOString() } : old
      )
      return { previous }
    },
    onError: (_err, _payload, context) => {
      if (context?.previous) queryClient.setQueryData(queryKeys.pages.detail(pageId), context.previous)
    },
    onSettled: (page) => {
      queryClient.invalidateQueries({ queryKey: queryKeys.pages.detail(pageId) })
      if (page?.workspaceId) {
        queryClient.invalidateQueries({ queryKey: queryKeys.pages.tree(page.workspaceId) })
        queryClient.invalidateQueries({ queryKey: queryKeys.pages.list(page.workspaceId) })
        queryClient.invalidateQueries({ queryKey: queryKeys.pages.favorites(page.workspaceId) })
      }
    },
  })
}
