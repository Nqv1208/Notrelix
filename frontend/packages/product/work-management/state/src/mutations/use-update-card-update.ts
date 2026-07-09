"use client"

import { useMutation, useQueryClient } from "@tanstack/react-query"
import { toast } from "sonner"
import { queryKeys } from "@notrelix/query"
import { commentApi } from "../api/item-comments.api"

export function useUpdateCardUpdate(cardId: string) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ updateId, body }: { updateId: string; body: string }) => commentApi.updateCardUpdate(updateId, body),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.cards.updates(cardId) })
      queryClient.invalidateQueries({ queryKey: queryKeys.cards.activity(cardId) })
    },
    onError: () => toast.error("Failed to edit update."),
  })
}
