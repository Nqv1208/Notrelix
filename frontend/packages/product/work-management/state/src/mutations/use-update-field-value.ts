import { useMutation, useQueryClient } from "@tanstack/react-query"
import { wmQueryKeys } from "@notrelix/work-management-core"
import type { UpdateFieldValueInput } from "@notrelix/work-management-core"
import type { Card, FullBoardResponse } from "@notrelix/work-management-core"
import { updateCardInFullBoard } from "@notrelix/work-management-core"
import { useWorkManagementServices } from "../services"

export function useUpdateFieldValue(boardId: string, workspaceId?: string) {
  const queryClient = useQueryClient()
  const { cards } = useWorkManagementServices()
  const queryKey = wmQueryKeys.fullBoard(boardId, workspaceId)

  return useMutation({
    mutationFn: (payload: UpdateFieldValueInput) => cards.updateFieldValue(payload),
    onMutate: async (payload) => {
      await queryClient.cancelQueries({ queryKey })
      const previous = queryClient.getQueryData<FullBoardResponse>(queryKey)
      queryClient.setQueryData<FullBoardResponse>(queryKey, (old) =>
        updateCardInFullBoard(old, payload.cardId, (card: Card) => {
          const memberIds = Array.isArray(payload.value) ? (payload.value as string[]) : undefined
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

          const fieldDef = previous?.fieldDefinitions.find((f) => f.id === payload.fieldDefinitionId)
          const isDueDateField = fieldDef?.fieldType === "date" || payload.fieldDefinitionId.endsWith("field-due-date")

          return {
            ...card,
            status: payload.fieldDefinitionId.endsWith("field-status") && typeof payload.value === "string" ? payload.value : card.status,
            priority: payload.fieldDefinitionId.endsWith("field-priority") ? (payload.value as Card["priority"]) : card.priority,
            dueDate: isDueDateField ? ((payload.value as string | null) ?? undefined) : card.dueDate,
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
    },
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey })
    },
  })
}
