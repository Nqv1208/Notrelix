import { useQuery } from "@tanstack/react-query"
import { wmQueryKeys } from "@notrelix/work-management-core"
import { useWorkManagementServices } from "../services"

export function useCardActivity(cardId: string) {
  const { cards } = useWorkManagementServices()
  return useQuery({
    queryKey: wmQueryKeys.cardActivity(cardId),
    queryFn: () => cards.getCardActivity(cardId),
    enabled: Boolean(cardId),
    staleTime: 30_000,
  })
}
