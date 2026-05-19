"use client"

import { useMutation, useQueryClient } from "@tanstack/react-query"
import { queryKeys } from "@/lib/query/query-keys"
import { pagesApi } from "../api/pages-api"

export function useDeletePage(workspaceId: string) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (pageId: string) => pagesApi.delete(pageId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.pages.tree(workspaceId) })
      queryClient.invalidateQueries({ queryKey: queryKeys.pages.list(workspaceId) })
      queryClient.invalidateQueries({ queryKey: queryKeys.pages.favorites(workspaceId) })
    },
  })
}
