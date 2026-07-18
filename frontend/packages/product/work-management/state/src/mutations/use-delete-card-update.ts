import { useMutation, useQueryClient } from "@tanstack/react-query"
import { toast } from "sonner"
import { queryKeys } from "@notrelix/query"
import { commentApi } from "../api/item-comments.api"

export function useDeleteCardUpdate(cardId: string) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: commentApi.deleteCardUpdate,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.cards.updates(cardId) })
      queryClient.invalidateQueries({ queryKey: queryKeys.cards.activity(cardId) })
    },
    onError: () => toast.error("Failed to delete update."),
  })
}
