"use client"

import { useMutation, useQueryClient } from "@tanstack/react-query"
import { queryKeys } from "@/lib/query/query-keys"
import { blockApi } from "../../api/block.api"
import type { Block, CreateBlockPayload } from "../../types/block.types"

export function useCreateBlock(pageId: string) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (payload: CreateBlockPayload) => blockApi.create(pageId, payload),
    onMutate: async (payload) => {
      await queryClient.cancelQueries({ queryKey: queryKeys.pages.blocks(pageId) })
      const previous = queryClient.getQueryData<Block[]>(queryKeys.pages.blocks(pageId))
      const optimistic: Block = {
        id: `optimistic-${Date.now()}`,
        pageId,
        type: payload.type,
        properties: payload.properties ?? { text: "" },
        position: payload.position ?? (previous?.length ?? 0) + 1,
        parentId: payload.parentId ?? null,
        createdById: "u-ana",
        updatedById: "u-ana",
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString(),
      }
      queryClient.setQueryData<Block[]>(queryKeys.pages.blocks(pageId), [...(previous ?? []), optimistic])
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
