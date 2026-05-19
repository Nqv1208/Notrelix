"use client"

import { useMutation, useQueryClient } from "@tanstack/react-query"
import { toast } from "sonner"
import { queryKeys } from "@/lib/query/query-keys"
import { boardsApi } from "../api/boards-api"
import type { UpdateFieldValueInput } from "../schemas"
import type { Card, FullBoardResponse } from "../types"
import { updateCardInFullBoard } from "./query-cache"

export function useUpdateFieldValue(boardId: string) {
  const queryClient = useQueryClient()
  const queryKey = queryKeys.boards.fullBoard(boardId)

  return useMutation({
    mutationFn: (payload: UpdateFieldValueInput) => boardsApi.updateFieldValue(payload),
    onMutate: async (payload) => {
      await queryClient.cancelQueries({ queryKey })
      const previous = queryClient.getQueryData<FullBoardResponse>(queryKey)
      queryClient.setQueryData<FullBoardResponse>(queryKey, (old) =>
        updateCardInFullBoard(old, payload.cardId, (card) => ({
          ...card,
          status: payload.fieldDefinitionId.endsWith("field-status") && typeof payload.value === "string" ? payload.value : card.status,
          priority: payload.fieldDefinitionId.endsWith("field-priority") ? (payload.value as Card["priority"]) : card.priority,
          fieldValues: { ...card.fieldValues, [payload.fieldDefinitionId]: payload.value },
          updatedAt: new Date().toISOString(),
        }))
      )
      return { previous }
    },
    onError: (_error, _variables, context) => {
      queryClient.setQueryData(queryKey, context?.previous)
      toast.error("Failed to update field. Changes reverted.")
    },
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey })
    },
  })
}
