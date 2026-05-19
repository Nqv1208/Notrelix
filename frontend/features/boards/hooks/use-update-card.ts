"use client"

import { useMutation, useQueryClient } from "@tanstack/react-query"
import { toast } from "sonner"
import { queryKeys } from "@/lib/query/query-keys"
import { boardsApi } from "../api/boards-api"
import type { FullBoardResponse } from "../types"
import type { UpdateCardInput } from "../schemas"
import { updateCardInFullBoard } from "./query-cache"

export function useUpdateCard(boardId: string) {
  const queryClient = useQueryClient()
  const queryKey = queryKeys.boards.fullBoard(boardId)

  return useMutation({
    mutationFn: ({ cardId, patch }: { cardId: string; patch: UpdateCardInput }) => boardsApi.updateCard(cardId, patch),
    onMutate: async ({ cardId, patch }) => {
      await queryClient.cancelQueries({ queryKey })
      const previous = queryClient.getQueryData<FullBoardResponse>(queryKey)
      queryClient.setQueryData<FullBoardResponse>(queryKey, (old) =>
        updateCardInFullBoard(old, cardId, (card) => {
          const next = {
            ...card,
            title: patch.title ?? card.title,
            descriptionMd: patch.descriptionMd ?? card.descriptionMd,
            priority: patch.priority === null ? undefined : patch.priority ?? card.priority,
            dueDate: patch.dueDate === null ? undefined : patch.dueDate ?? card.dueDate,
            startDate: patch.startDate === null ? undefined : patch.startDate ?? card.startDate,
            fieldValues: {
              ...card.fieldValues,
              [`${card.boardId}-field-title`]: patch.title ?? card.fieldValues[`${card.boardId}-field-title`],
              [`${card.boardId}-field-due-date`]: patch.dueDate ?? card.fieldValues[`${card.boardId}-field-due-date`],
              [`${card.boardId}-field-priority`]: patch.priority ?? card.fieldValues[`${card.boardId}-field-priority`],
            },
            updatedAt: new Date().toISOString(),
          }
          return next
        })
      )
      return { previous }
    },
    onError: (_error, _variables, context) => {
      queryClient.setQueryData(queryKey, context?.previous)
      toast.error("Failed to update card. Changes reverted.")
    },
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey })
    },
  })
}
