"use client"

import { useQuery } from "@tanstack/react-query"
import { queryKeys } from "@/lib/query/query-keys"
import { boardsApi } from "../api/boards-api"

export function useCard(cardId: string) {
  const query = useQuery({
    queryKey: queryKeys.cards.detail(cardId),
    queryFn: () => boardsApi.getCard(cardId),
    enabled: Boolean(cardId),
    staleTime: 30_000,
  })

  return {
    card: query.data,
    isLoading: query.isLoading,
    error: query.error,
  }
}
