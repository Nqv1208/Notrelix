import { useMutation, useQueryClient } from "@tanstack/react-query"
import { toast } from "sonner"
import { queryKeys } from "@notrelix/work-management-core"
import { cardApi } from "../api/item.api"
import type { FullBoardResponse } from "@notrelix/work-management-core"

type MutationContext = { previous?: FullBoardResponse }

export function useDeleteCard(boardId: string, workspaceId?: string) {
  const queryClient = useQueryClient()
  const queryKey = queryKeys.boards.fullBoard(boardId, workspaceId)

  return useMutation<void, Error, string, MutationContext>({
    mutationFn: cardApi.deleteCard,
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
      toast.error("Failed to delete task. Changes reverted.")
    },
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey })
    },
  })
}
