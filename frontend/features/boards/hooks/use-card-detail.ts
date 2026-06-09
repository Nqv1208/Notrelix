import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"
import { toast } from "sonner"
import { queryKeys } from "@/lib/query/query-keys"
import { cardApi } from "../api/card.api"
import { labelApi } from "../api/label.api"
import type { CardDetail } from "../types"
import { useUpdateCard } from "./use-update-card"
import { useUpdateFieldValue } from "./use-update-field-value"

export function useCardDetail(cardId: string, boardId: string, workspaceId: string) {
  const queryClient = useQueryClient()
  const detailKey = queryKeys.cards.detail(cardId)
  const fullBoardKey = queryKeys.boards.fullBoard(boardId, workspaceId)

  const cardQuery = useQuery<CardDetail>({
    queryKey: detailKey,
    queryFn: () => cardApi.getCard(cardId),
    enabled: Boolean(cardId),
    staleTime: 10_000,
  })

  const updateCardMutation = useUpdateCard(boardId, workspaceId)
  const updateFieldValueMutation = useUpdateFieldValue(boardId, workspaceId)

  const addLabelMutation = useMutation({
    mutationFn: (labelId: string) => labelApi.addLabelToCard(cardId, labelId),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: detailKey })
      void queryClient.invalidateQueries({ queryKey: fullBoardKey })
    },
    onError: () => {
      toast.error("Failed to add label.")
    },
  })

  const removeLabelMutation = useMutation({
    mutationFn: (labelId: string) => labelApi.removeLabelFromCard(cardId, labelId),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: detailKey })
      void queryClient.invalidateQueries({ queryKey: fullBoardKey })
    },
    onError: () => {
      toast.error("Failed to remove label.")
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
