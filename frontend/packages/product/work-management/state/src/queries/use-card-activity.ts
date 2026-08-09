import { useQuery } from "@tanstack/react-query";
import { wmQueryKeys } from "./keys";
import { useWorkManagementServices } from "../services";

export function useCardActivity(cardId: string, workspaceId: string) {
  const { cards } = useWorkManagementServices();
  return useQuery({
    queryKey: wmQueryKeys.cardActivity(workspaceId, cardId),
    queryFn: () => cards.getCardActivity(cardId),
    enabled: Boolean(cardId),
    staleTime: 30_000,
  });
}
