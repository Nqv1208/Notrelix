import { useCallback } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { wmQueryKeys } from "../queries/keys";
import { useWorkManagementServices } from "../services";

/**
 * Puts a board label on an item or takes it off. Not optimistic: the item and
 * the board are read again once the server has answered. `createAndApply`
 * creates a new label of the board and puts it on the item (it throws when
 * either step fails, so the picker can say so).
 */
export function useCardLabelWrites(
  cardId: string,
  boardId: string,
  workspaceId: string,
) {
  const queryClient = useQueryClient();
  const { labels } = useWorkManagementServices();
  const refresh = useCallback(() => {
    void queryClient.invalidateQueries({
      queryKey: wmQueryKeys.cardDetail(workspaceId, cardId),
    });
    void queryClient.invalidateQueries({
      queryKey: wmQueryKeys.fullBoard(workspaceId, boardId),
    });
  }, [boardId, cardId, queryClient, workspaceId]);

  const toggle = useMutation({
    mutationFn: ({ labelId, on }: { labelId: string; on: boolean }) =>
      on
        ? labels.addLabelToCard(cardId, labelId)
        : labels.removeLabelFromCard(cardId, labelId),
    onSettled: refresh,
  });

  const createAndApply = useCallback(
    async (name: string, color: string): Promise<void> => {
      try {
        const label = await labels.createLabel({ boardId, name, color });
        await labels.addLabelToCard(cardId, label.id);
      } finally {
        // The label exists even when putting it on the item failed.
        void queryClient.invalidateQueries({
          queryKey: wmQueryKeys.boardLabels(workspaceId, boardId),
        });
        refresh();
      }
    },
    [boardId, cardId, labels, queryClient, refresh, workspaceId],
  );

  return { mutate: toggle.mutate, createAndApply };
}
