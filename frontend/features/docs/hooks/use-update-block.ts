"use client"

import { useMutation, useQueryClient } from "@tanstack/react-query"
import { queryKeys } from "@/lib/query/query-keys"
import { blockService } from "../api/block.service"
import type { Block, UpdateBlockPayload } from "../types"

export function useUpdateBlock(pageId: string) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ blockId, payload }: { blockId: string; payload: UpdateBlockPayload }) =>
      blockService.update(blockId, payload),
    onMutate: async ({ blockId, payload }) => {
      await queryClient.cancelQueries({ queryKey: queryKeys.pages.blocks(pageId) })
      const previous = queryClient.getQueryData<Block[]>(queryKeys.pages.blocks(pageId))
      queryClient.setQueryData<Block[]>(queryKeys.pages.blocks(pageId), (old = []) =>
        old.map((block) =>
          block.id === blockId
            ? {
                ...block,
                ...payload,
                properties: { ...block.properties, ...payload.properties },
                updatedAt: new Date().toISOString(),
              }
            : block
        )
      )
      return { previous }
    },
    onError: (_error, _variables, context) => {
      if (context?.previous) queryClient.setQueryData(queryKeys.pages.blocks(pageId), context.previous)
    },
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.pages.blocks(pageId) })
    },
  })
}
