"use client"

import { useMutation, useQueryClient } from "@tanstack/react-query"
import { toast } from "sonner"
import { queryKeys } from "@/lib/query/query-keys"
import { cardApi } from "../api/card.api"
import type { UpdateFieldValueInput } from "../schemas"
import type { Card, FullBoardResponse } from "../types"
import { updateCardInFullBoard } from "./query-cache"

export function useUpdateFieldValue(boardId: string, workspaceId?: string) {
  const queryClient = useQueryClient()
  const queryKey = queryKeys.boards.fullBoard(boardId, workspaceId)

  return useMutation({
    mutationFn: (payload: UpdateFieldValueInput) => cardApi.updateFieldValue(payload),
    onMutate: async (payload) => {
      await queryClient.cancelQueries({ queryKey })
      const previous = queryClient.getQueryData<FullBoardResponse>(queryKey)
      queryClient.setQueryData<FullBoardResponse>(queryKey, (old) =>
        updateCardInFullBoard(old, payload.cardId, (card) => {
          const memberIds = payload.fieldDefinitionId.endsWith("field-person") && Array.isArray(payload.value) ? payload.value : undefined
          const members = memberIds
            ? (old?.board.members
                .filter((member) => memberIds.includes(member.userId))
                .map((member) => ({
                  id: `cm-${card.id}-${member.userId}`,
                  userId: member.userId,
                  name: member.name,
                  initials: member.initials,
                  avatarUrl: member.avatarUrl,
                  color: member.color,
                })) ?? [])
            : card.members

          return {
            ...card,
            status: payload.fieldDefinitionId.endsWith("field-status") && typeof payload.value === "string" ? payload.value : card.status,
            priority: payload.fieldDefinitionId.endsWith("field-priority") ? (payload.value as Card["priority"]) : card.priority,
            members,
            fieldValues: { ...card.fieldValues, [payload.fieldDefinitionId]: payload.value },
            updatedAt: new Date().toISOString(),
          }
        })
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
