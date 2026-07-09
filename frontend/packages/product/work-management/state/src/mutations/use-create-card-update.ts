"use client"

import { useMutation, useQueryClient } from "@tanstack/react-query"
import { toast } from "sonner"
import { queryKeys } from "@notrelix/query"
import { commentApi } from "../api/item-comments.api"
import type { CreateCardUpdateInput } from "@notrelix/work-management-core"

export function useCreateCardUpdate(cardId: string) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (input: CreateCardUpdateInput) => commentApi.createCardUpdate(input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.cards.updates(cardId) })
      queryClient.invalidateQueries({ queryKey: queryKeys.cards.detail(cardId) })
    },
    onError: () => {
      toast.error("Failed to post update.")
    },
  })
}
