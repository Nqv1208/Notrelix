import { useQuery } from "@tanstack/react-query"
import { wmQueryKeys } from "./keys"
import { useWorkManagementServices } from "../services"

export function useCardFiles(cardId: string, workspaceId: string) {
  const { cards } = useWorkManagementServices()
  return useQuery({
    queryKey: wmQueryKeys.cardFiles(workspaceId, cardId),
    queryFn: () => cards.getCardFiles(cardId),
    enabled: Boolean(cardId),
    staleTime: 30_000,
  })
}
