import {
  useMutation,
  useQueryClient,
  type QueryClient,
} from "@tanstack/react-query";
import type { ApiRequestOptions } from "@notrelix/contracts";
import { restoreBoardCard } from "../cache/card-restore";
import { useWorkManagementServices } from "../services";
import { runCardCommand } from "./card-command";
import type { CardWriteHookOptions } from "./use-update-item-status";

interface ItemArchiver {
  archiveCard(cardId: string, options?: ApiRequestOptions): Promise<void>;
}

/**
 * Archives an item (`POST /board-items/{id}/archive`; the item can be
 * restored, it is not deleted). The card leaves the board at once and comes
 * back if the write fails.
 */
export function runArchiveCardCommand(
  queryClient: QueryClient,
  cards: ItemArchiver,
  {
    workspaceId,
    boardId,
    cardId,
  }: { workspaceId: string; boardId: string; cardId: string },
): Promise<void> {
  return runCardCommand({
    queryClient,
    workspaceId,
    boardId,
    cardId,
    kind: "archive-item",
    applyBoard: (board) => restoreBoardCard(board, cardId, undefined) ?? board,
    send: (context) => cards.archiveCard(cardId, context),
  });
}

export function useArchiveCard(
  boardId: string,
  workspaceId: string,
  options: CardWriteHookOptions<string> = {},
) {
  const queryClient = useQueryClient();
  const { cards } = useWorkManagementServices();
  return useMutation({
    mutationFn: (cardId: string) =>
      runArchiveCardCommand(queryClient, cards, {
        workspaceId,
        boardId,
        cardId,
      }),
    onError: options.onError,
  });
}
