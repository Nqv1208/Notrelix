"use client"

import { useQuery } from "@tanstack/react-query"
import { queryKeys } from "@notrelix/query"
import { cardApi } from "../api/item.api"

export function useCardFiles(cardId: string) {
  return useQuery({
    queryKey: queryKeys.cards.files(cardId),
    queryFn: () => cardApi.getCardFiles(cardId),
    enabled: Boolean(cardId),
    staleTime: 30_000,
  })
}
