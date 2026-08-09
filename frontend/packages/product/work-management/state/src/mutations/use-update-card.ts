import { useMutation, useQueryClient } from "@tanstack/react-query"
import { wmQueryKeys } from "../queries/keys"
import type { FullBoardResponse, Card } from "@notrelix/work-management-core"
import type { UpdateCardInput } from "@notrelix/work-management-core"
import { updateCardInFullBoard } from "@notrelix/work-management-core"
import { useWorkManagementServices } from "../services"

export function useUpdateCard(boardId: string, workspaceId?: string) {
  const queryClient = useQueryClient()
  const { cards } = useWorkManagementServices()
  const queryKey = wmQueryKeys.fullBoard(workspaceId!, boardId)

  return useMutation({
    mutationFn: ({ cardId, patch }: { cardId: string; patch: UpdateCardInput }) => cards.updateCard(cardId, patch),
    onMutate: async ({ cardId, patch }) => {
      await queryClient.cancelQueries({ queryKey })
      const previous = queryClient.getQueryData<FullBoardResponse>(queryKey)
      queryClient.setQueryData<FullBoardResponse>(queryKey, (old) =>
        updateCardInFullBoard(old, cardId, (card: Card) => {
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
    },
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey })
    },
  })
}
