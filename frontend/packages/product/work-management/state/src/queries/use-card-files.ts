import { useQuery } from "@tanstack/react-query"
import { wmQueryKeys } from "@notrelix/work-management-core"
import { useWorkManagementServices } from "../services"

export function useCardFiles(cardId: string) {
  const { cards } = useWorkManagementServices()
  return useQuery({
    queryKey: wmQueryKeys.cardFiles(cardId),
    queryFn: () => cards.getCardFiles(cardId),
    enabled: Boolean(cardId),
    staleTime: 30_000,
  })
}
