import { useQuery } from "@tanstack/react-query"
import { queryKeys } from "@notrelix/work-management-core"
import { cardApi } from "../api/item.api"
import type { CardDetail } from "@notrelix/work-management-core"

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
