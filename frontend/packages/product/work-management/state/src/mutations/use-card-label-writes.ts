import { useMutation, useQueryClient } from "@tanstack/react-query";
import { wmQueryKeys } from "../queries/keys";
import { useWorkManagementServices } from "../services";

/**
 * Puts a board label on an item or takes it off. Not optimistic: the item and
 * the board are read again once the server has answered.
 */
export function useCardLabelWrites(
  cardId: string,
  boardId: string,
  workspaceId: string,
) {
  const queryClient = useQueryClient();
  const { labels } = useWorkManagementServices();
  return useMutation({
    mutationFn: ({ labelId, on }: { labelId: string; on: boolean }) =>
      on
        ? labels.addLabelToCard(cardId, labelId)
        : labels.removeLabelFromCard(cardId, labelId),
    onSettled: () => {
      void queryClient.invalidateQueries({
        queryKey: wmQueryKeys.cardDetail(workspaceId, cardId),
      });
      void queryClient.invalidateQueries({
        queryKey: wmQueryKeys.fullBoard(workspaceId, boardId),
      });
    },
  });
}
