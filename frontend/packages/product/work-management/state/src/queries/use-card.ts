import { useQuery } from "@tanstack/react-query"
import { queryKeys } from "@notrelix/work-management-core"
import type { CardDetail } from "@notrelix/work-management-core"
import { useWorkManagementServices } from "../services"

export function useCard(cardId: string) {
  const { cards } = useWorkManagementServices()
  const query = useQuery<CardDetail>({
    queryKey: queryKeys.cards.detail(cardId),
    queryFn: () => cards.getCard(cardId),
    enabled: Boolean(cardId),
    staleTime: 30_000,
  })

  return {
    card: query.data,
    isLoading: query.isLoading,
    error: query.error,
  }
}
