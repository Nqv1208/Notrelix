"use client"

import { useMutation, useQueryClient } from "@tanstack/react-query"
import { toast } from "sonner"
import { queryKeys } from "@/lib/query/query-keys"
import { commentApi } from "../api/comment.api"

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
