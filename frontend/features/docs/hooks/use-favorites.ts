"use client"

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"
import { queryKeys } from "@/lib/query/query-keys"
import { pageService } from "../api/page.service"

export function useFavorites(workspaceId: string) {
  return useQuery({
    queryKey: queryKeys.pages.favorites(workspaceId),
    queryFn: () => pageService.getFavorites(workspaceId),
    enabled: !!workspaceId,
    staleTime: 30_000,
  })
}

export function useToggleFavorite(workspaceId: string) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ pageId, isFavorited }: { pageId: string; isFavorited: boolean }) =>
      pageService.favorite(pageId, isFavorited),
    onSettled: (_page, _error, variables) => {
      queryClient.invalidateQueries({ queryKey: queryKeys.pages.favorites(workspaceId) })
      queryClient.invalidateQueries({ queryKey: queryKeys.pages.list(workspaceId) })
      queryClient.invalidateQueries({ queryKey: queryKeys.pages.tree(workspaceId) })
      queryClient.invalidateQueries({ queryKey: queryKeys.pages.detail(variables.pageId) })
    },
  })
}
