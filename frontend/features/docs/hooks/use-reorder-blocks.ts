"use client"

import { useMutation, useQueryClient } from "@tanstack/react-query"
import { queryKeys } from "@/lib/query/query-keys"
import { blocksApi } from "../api/blocks-api"
import type { Block, ReorderBlocksInput } from "../types"

export function useReorderBlocks(pageId: string) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (payload: ReorderBlocksInput) => blocksApi.reorder(payload),
    onMutate: async (payload) => {
      await queryClient.cancelQueries({ queryKey: queryKeys.pages.blocks(pageId) })
      const previous = queryClient.getQueryData<Block[]>(queryKeys.pages.blocks(pageId))
      queryClient.setQueryData<Block[]>(queryKeys.pages.blocks(pageId), (old = []) => {
        const byId = new Map(old.map((block) => [block.id, block]))
        const ordered = payload.orderedBlockIds
          .map((blockId, index) => {
            const block = byId.get(blockId)
            if (!block) return null
            return { ...block, position: index + 1, updatedAt: new Date().toISOString() }
          })
          .filter((block): block is Block => Boolean(block))
        const missing = old.filter((block) => !payload.orderedBlockIds.includes(block.id))
        return [...ordered, ...missing]
      })
      return { previous }
    },
    onError: (_error, _payload, context) => {
      if (context?.previous) queryClient.setQueryData(queryKeys.pages.blocks(pageId), context.previous)
    },
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.pages.blocks(pageId) })
      queryClient.invalidateQueries({ queryKey: queryKeys.pages.detail(pageId) })
    },
  })
}
