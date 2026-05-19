"use client"

import { useQuery } from "@tanstack/react-query"
import { queryKeys } from "@/lib/query/query-keys"
import { boardsApi } from "../api/boards-api"

export function useCardComments(cardId: string) {
  return useQuery({
    queryKey: queryKeys.cards.comments(cardId),
    queryFn: () => boardsApi.getCardComments(cardId),
    enabled: Boolean(cardId),
    staleTime: 30_000,
  })
}
