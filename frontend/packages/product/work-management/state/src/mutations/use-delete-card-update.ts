import { useMutation, useQueryClient } from "@tanstack/react-query";
import { wmQueryKeys } from "../queries/keys";
import { useWorkManagementServices } from "../services";

export function useDeleteCardUpdate(cardId: string, workspaceId: string) {
  const queryClient = useQueryClient();
  const { comments } = useWorkManagementServices();

  return useMutation({
    mutationFn: comments.deleteCardUpdate,
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: wmQueryKeys.cardUpdates(workspaceId, cardId),
      });
      queryClient.invalidateQueries({
        queryKey: wmQueryKeys.cardActivity(workspaceId, cardId),
      });
    },
    // A refusal (403, or 404 when the item is no longer visible) leaves the
    // list stale: read it again so it shows what the server holds.
    onError: () => {
      queryClient.invalidateQueries({
        queryKey: wmQueryKeys.cardUpdates(workspaceId, cardId),
      });
    },
  });
}
