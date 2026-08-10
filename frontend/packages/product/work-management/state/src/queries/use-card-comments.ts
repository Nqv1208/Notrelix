import { useQuery } from "@tanstack/react-query";
import { wmQueryKeys } from "./keys";
import { useWorkManagementServices } from "../services";

export function useCardComments(cardId: string, workspaceId: string) {
  const { comments } = useWorkManagementServices();
  return useQuery({
    queryKey: wmQueryKeys.cardUpdates(workspaceId, cardId),
    queryFn: () => comments.getCardUpdates(cardId),
    enabled: Boolean(cardId),
    staleTime: 30_000,
  });
}
