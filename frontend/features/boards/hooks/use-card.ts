"use client"

import { useQuery } from "@tanstack/react-query"
import { queryKeys } from "@/lib/query/query-keys"
import { cardApi } from "../api/card.api"
import type { CardDetail } from "../types"

export function useCard(cardId: string) {
  const query = useQuery<CardDetail>({
    queryKey: queryKeys.cards.detail(cardId),
    queryFn: () => cardApi.getCard(cardId),
    enabled: Boolean(cardId),
    staleTime: 30_000,
  })

  return {
    card: query.data,
    isLoading: query.isLoading,
    error: query.error,
  }
}
