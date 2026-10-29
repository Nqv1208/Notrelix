import { useMutation, useQueryClient } from "@tanstack/react-query";
import { wmQueryKeys } from "../queries/keys";
import { useWorkManagementServices } from "../services";

/** Deletes a file of an item; the item's files and history are read again after. */
export function useDeleteCardFile(cardId: string, workspaceId: string) {
  const queryClient = useQueryClient();
  const { cards } = useWorkManagementServices();

  return useMutation({
    mutationFn: (attachmentId: string) =>
      cards.deleteCardFile(cardId, attachmentId),
    onSuccess: () => {
      void queryClient.invalidateQueries({
        queryKey: wmQueryKeys.cardFiles(workspaceId, cardId),
      });
      void queryClient.invalidateQueries({
        queryKey: wmQueryKeys.cardActivity(workspaceId, cardId),
      });
    },
    // A refusal (403, or 404 when the item is no longer visible) leaves the
    // list stale: read it again so it shows what the server holds.
    onError: () => {
      void queryClient.invalidateQueries({
        queryKey: wmQueryKeys.cardFiles(workspaceId, cardId),
      });
    },
  });
}
