"use client"

import { useQuery } from "@tanstack/react-query"
import { queryKeys } from "@/lib/query/query-keys"
import { boardsApi } from "../api/boards-api"

export function useCardActivity(cardId: string) {
  return useQuery({
    queryKey: queryKeys.cards.activity(cardId),
    queryFn: () => boardsApi.getCardActivity(cardId),
    enabled: Boolean(cardId),
    staleTime: 30_000,
  })
}
