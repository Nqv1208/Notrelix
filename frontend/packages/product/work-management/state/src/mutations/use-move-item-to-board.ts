import { useMutation, useQueryClient } from "@tanstack/react-query";
import { workspaceQueryKey } from "@notrelix/query";
import { restoreBoardCard } from "../cache/card-restore";
import { wmQueryKeys } from "../queries/keys";
import { useWorkManagementServices } from "../services";
import { runCardCommand } from "./card-command";
import type { CardWriteHookOptions } from "./use-update-item-status";
import type { MoveToBoardResultDto } from "../api/item-extras.api";

export interface MoveItemToBoardVariables {
  readonly cardId: string;
  readonly targetBoardId: string;
}

/**
 * Moves an item to another board (`WorkManagement.BoardItems.MoveToBoard`).
 * The card leaves this board at once and comes back if the write fails; the
 * target board's read and the lists that show the item are read again either
 * way. The result names what could not carry over so the caller can say so.
 */
export function useMoveItemToBoard(
  boardId: string,
  workspaceId: string,
  options: CardWriteHookOptions<MoveItemToBoardVariables> & {
    readonly onMoved?: (
      result: MoveToBoardResultDto,
      variables: MoveItemToBoardVariables,
    ) => void;
  } = {},
) {
  const queryClient = useQueryClient();
  const { itemExtras } = useWorkManagementServices();
  return useMutation({
    mutationFn: (variables: MoveItemToBoardVariables) =>
      runCardCommand({
        queryClient,
        workspaceId,
        boardId,
        cardId: variables.cardId,
        kind: "move-to-board",
        applyBoard: (board) =>
          restoreBoardCard(board, variables.cardId, undefined) ?? board,
        send: () =>
          itemExtras.moveToBoard(variables.cardId, variables.targetBoardId),
      }),
    onSuccess: (result, variables) => options.onMoved?.(result, variables),
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
