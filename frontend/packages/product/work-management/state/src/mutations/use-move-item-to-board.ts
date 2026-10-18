import { useMutation, useQueryClient } from "@tanstack/react-query";
import { workspaceQueryKey } from "@notrelix/query";
import { restoreBoardCard } from "../cache/card-restore";
import { wmQueryKeys } from "../queries/keys";
import { useWorkManagementServices } from "../services";
import { runCardCommand } from "./card-command";
import type { CardWriteHookOptions } from "./use-update-item-status";

export interface MoveItemToBoardVariables {
  readonly cardId: string;
  readonly targetBoardId: string;
}

/**
 * Moves an item to another board (gap `CTR-GAP-WM-TABLE-BULK`). The card
 * leaves this board at once and comes back if the write fails; the target
 * board's read and the lists that show the item are read again either way.
 */
export function useMoveItemToBoard(
  boardId: string,
  workspaceId: string,
  options: CardWriteHookOptions<MoveItemToBoardVariables> = {},
) {
  const queryClient = useQueryClient();
  const { itemExtras } = useWorkManagementServices();
  return useMutation({
    mutationFn: ({ cardId, targetBoardId }: MoveItemToBoardVariables) =>
      runCardCommand({
        queryClient,
        workspaceId,
        boardId,
        cardId,
        kind: "move-to-board",
        applyBoard: (board) =>
          restoreBoardCard(board, cardId, undefined) ?? board,
        send: () => itemExtras.moveToBoard(cardId, targetBoardId),
      }),
    onSettled: (_data, _error, { targetBoardId }) => {
      void queryClient.invalidateQueries({
        queryKey: wmQueryKeys.fullBoard(workspaceId, targetBoardId),
      });
      void queryClient.invalidateQueries({
        queryKey: workspaceQueryKey(workspaceId, "work-management"),
      });
    },
    onError: options.onError,
  });
}
