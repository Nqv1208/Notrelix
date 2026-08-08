import { useQuery } from "@tanstack/react-query"
import { wmQueryKeys } from "@notrelix/work-management-core"
import { useWorkManagementServices } from "../services"

export function useCardComments(cardId: string) {
  const { comments } = useWorkManagementServices()
  return useQuery({
    queryKey: wmQueryKeys.cardUpdates(cardId),
    queryFn: () => comments.getCardUpdates(cardId),
    enabled: Boolean(cardId),
    staleTime: 30_000,
  })
}
