import { useQuery } from "@tanstack/react-query"
import { queryKeys } from "@notrelix/work-management-core"
import { commentApi } from "../api/item-comments.api"

export function useCardComments(cardId: string) {
  return useQuery({
    queryKey: queryKeys.cards.updates(cardId),
    queryFn: () => commentApi.getCardUpdates(cardId),
    enabled: Boolean(cardId),
    staleTime: 30_000,
  })
}
