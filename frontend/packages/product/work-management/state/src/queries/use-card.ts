import { useQuery } from "@tanstack/react-query";
import { wmQueryKeys } from "./keys";
import type { CardDetail } from "@notrelix/work-management-core";
import { useWorkManagementServices } from "../services";

export function useCard(cardId: string, workspaceId: string) {
  const { cards } = useWorkManagementServices();
  const query = useQuery<CardDetail>({
    queryKey: wmQueryKeys.cardDetail(workspaceId, cardId),
    queryFn: () => cards.getCard(cardId),
    enabled: Boolean(cardId),
    staleTime: 30_000,
  });

  return {
    card: query.data,
    isLoading: query.isLoading,
    error: query.error,
  };
}
