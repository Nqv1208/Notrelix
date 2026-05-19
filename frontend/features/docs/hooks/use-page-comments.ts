"use client"

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"
import { queryKeys } from "@/lib/query/query-keys"
import { mockPageService } from "../mock/mock-page-service"
import type { CreateCommentPayload, PageComment } from "../types"

export function usePageComments(pageId: string) {
  return useQuery({
    queryKey: queryKeys.pages.comments(pageId),
    queryFn: () => mockPageService.getComments(pageId),
    enabled: !!pageId,
    staleTime: 15_000,
  })
}

export function useCreatePageComment(pageId: string) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (payload: Omit<CreateCommentPayload, "pageId">) =>
      mockPageService.createComment({ ...payload, pageId }),
    onMutate: async (payload) => {
      await queryClient.cancelQueries({ queryKey: queryKeys.pages.comments(pageId) })
      const previous = queryClient.getQueryData<PageComment[]>(queryKeys.pages.comments(pageId))
      const optimistic: PageComment = {
        id: `optimistic-comment-${Date.now()}`,
        pageId,
        blockId: payload.blockId ?? null,
        authorId: "u-ana",
        body: payload.body,
        mentionIds: payload.mentionIds ?? [],
        resolved: false,
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString(),
      }
      queryClient.setQueryData<PageComment[]>(queryKeys.pages.comments(pageId), [optimistic, ...(previous ?? [])])
      return { previous }
    },
    onError: (_error, _payload, context) => {
      if (context?.previous) queryClient.setQueryData(queryKeys.pages.comments(pageId), context.previous)
    },
    onSettled: () => queryClient.invalidateQueries({ queryKey: queryKeys.pages.comments(pageId) }),
  })
}
