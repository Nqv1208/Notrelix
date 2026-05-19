"use client"

import { useMutation, useQueryClient } from "@tanstack/react-query"
import { toast } from "sonner"
import { queryKeys } from "@/lib/query/query-keys"
import { boardsApi } from "../api/boards-api"
import type { MoveCardInput } from "../schemas/move-card.schema"
import type { FullBoardResponse } from "../types"

export function useMoveCard(boardId: string) {
  const queryClient = useQueryClient()
  const queryKey = queryKeys.boards.fullBoard(boardId)

  return useMutation({
    mutationFn: (payload: MoveCardInput) => boardsApi.moveCard(payload),
    onMutate: async (payload) => {
      await queryClient.cancelQueries({ queryKey })
      const previous = queryClient.getQueryData<FullBoardResponse>(queryKey)
      queryClient.setQueryData<FullBoardResponse>(queryKey, (old) => {
        if (!old) return old
        let movedCard = old.groups.flatMap((group) => group.cards).find((card) => card.id === payload.cardId)
        if (!movedCard) return old
        movedCard = { ...movedCard, listId: payload.listId, position: payload.position, updatedAt: new Date().toISOString() }
        return {
          ...old,
          groups: old.groups.map((group) => {
            const withoutCard = group.cards.filter((card) => card.id !== payload.cardId)
            if (group.id !== payload.listId) return { ...group, cards: withoutCard }
            return {
              ...group,
              cards: [...withoutCard, movedCard].sort((a, b) => a.position - b.position),
            }
          }),
        }
      })
      return { previous }
    },
    onError: (_error, _variables, context) => {
      queryClient.setQueryData(queryKey, context?.previous)
      toast.error("Failed to move card. Changes reverted.")
    },
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey })
    },
  })
}
