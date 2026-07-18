import { useQuery } from "@tanstack/react-query"
import { queryKeys } from "@notrelix/query"
import { cardApi } from "../api/item.api"

export function useCardActivity(cardId: string) {
  return useQuery({
    queryKey: queryKeys.cards.activity(cardId),
    queryFn: () => cardApi.getCardActivity(cardId),
    enabled: Boolean(cardId),
    staleTime: 30_000,
  })
}
