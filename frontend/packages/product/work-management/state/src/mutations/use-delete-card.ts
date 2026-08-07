import { useMutation, useQueryClient } from "@tanstack/react-query"
import { queryKeys } from "@notrelix/work-management-core"
import { useWorkManagementServices } from "../services"
import type { FullBoardResponse } from "@notrelix/work-management-core"

type MutationContext = { previous?: FullBoardResponse }

export function useDeleteCard(boardId: string, workspaceId?: string) {
  const queryClient = useQueryClient()
  const { cards } = useWorkManagementServices()
  const queryKey = queryKeys.boards.fullBoard(boardId, workspaceId)

  return useMutation<void, Error, string, MutationContext>({
    mutationFn: cards.deleteCard,
    onMutate: async (cardId) => {
      await queryClient.cancelQueries({ queryKey })
      const previous = queryClient.getQueryData<FullBoardResponse>(queryKey)
      queryClient.setQueryData<FullBoardResponse>(queryKey, (old) => {
        if (!old) return old
        return {
          ...old,
          groups: old.groups.map((group) => ({
            ...group,
            cards: group.cards.filter((card) => card.id !== cardId),
          })),
        }
      })
      return { previous }
    },
    onError: (_error, _cardId, context) => {
      queryClient.setQueryData(queryKey, context?.previous)
    },
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey })
    },
  })
}
