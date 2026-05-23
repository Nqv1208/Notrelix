"use client"

import { useQuery } from "@tanstack/react-query"
import { queryKeys } from "@/lib/query/query-keys"
import { commentApi } from "../api/comment.api"

export function useCardComments(cardId: string) {
  return useQuery({
    queryKey: queryKeys.cards.updates(cardId),
    queryFn: () => commentApi.getCardUpdates(cardId),
    enabled: Boolean(cardId),
    staleTime: 30_000,
  })
}
