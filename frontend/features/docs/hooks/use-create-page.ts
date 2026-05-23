"use client"

import { useMutation, useQueryClient } from "@tanstack/react-query"
import { queryKeys } from "@/lib/query/query-keys"
import { pageService } from "../api/page.service"
import type { CreatePagePayload, Page } from "../types"

export function useCreatePage() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (payload: CreatePagePayload) => pageService.create(payload),
    onMutate: async (payload) => {
      await queryClient.cancelQueries({ queryKey: queryKeys.pages.list(payload.workspaceId) })
      const previous = queryClient.getQueryData<Page[]>(queryKeys.pages.list(payload.workspaceId))
      return { previous, workspaceId: payload.workspaceId }
    },
    onSuccess: (newPage) => {
      queryClient.invalidateQueries({ queryKey: queryKeys.pages.tree(newPage.workspaceId) })
      queryClient.invalidateQueries({ queryKey: queryKeys.pages.list(newPage.workspaceId) })
    },
    onError: (_error, _payload, context) => {
      if (context?.previous) {
        queryClient.setQueryData(queryKeys.pages.list(context.workspaceId), context.previous)
      }
    },
  })
}
