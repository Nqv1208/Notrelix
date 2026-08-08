import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"
import { wmQueryKeys } from "@notrelix/work-management-core"
import type { CardDetail } from "@notrelix/work-management-core"
import { useUpdateCard } from "../mutations/use-update-card"
import { useUpdateFieldValue } from "../mutations/use-update-field-value"
import { useWorkManagementServices } from "../services"

export function useCardDetail(cardId: string, boardId: string, workspaceId: string) {
  const queryClient = useQueryClient()
  const { cards, labels } = useWorkManagementServices()
  const detailKey = wmQueryKeys.cardDetail(cardId)
  const fullBoardKey = wmQueryKeys.fullBoard(boardId, workspaceId)

  const cardQuery = useQuery<CardDetail>({
    queryKey: detailKey,
    queryFn: () => cards.getCard(cardId),
    enabled: Boolean(cardId),
    staleTime: 10_000,
  })

  const updateCardMutation = useUpdateCard(boardId, workspaceId)
  const updateFieldValueMutation = useUpdateFieldValue(boardId, workspaceId)

  const addLabelMutation = useMutation({
    mutationFn: (labelId: string) => labels.addLabelToCard(cardId, labelId),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: detailKey })
      void queryClient.invalidateQueries({ queryKey: fullBoardKey })
    },
  })

  const removeLabelMutation = useMutation({
    mutationFn: (labelId: string) => labels.removeLabelFromCard(cardId, labelId),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: detailKey })
      void queryClient.invalidateQueries({ queryKey: fullBoardKey })
    },
  })

  const updateTitle = (title: string) => {
    updateCardMutation.mutate({ cardId, patch: { title } })
  }

  const updateDescription = (descriptionMd: string) => {
    updateCardMutation.mutate({ cardId, patch: { descriptionMd } })
  }

  const addLabel = (labelId: string) => {
    addLabelMutation.mutate(labelId)
  }

  const removeLabel = (labelId: string) => {
    removeLabelMutation.mutate(labelId)
  }

  return {
    card: cardQuery.data,
    isLoading: cardQuery.isLoading,
    error: cardQuery.error,
    updateTitle,
    updateDescription,
    addLabel,
    removeLabel,
    updateCard: updateCardMutation.mutate,
    updateFieldValue: updateFieldValueMutation.mutate,
  }
}
