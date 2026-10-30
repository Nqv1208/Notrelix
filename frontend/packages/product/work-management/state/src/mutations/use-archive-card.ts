import {
  useMutation,
  useQueryClient,
  type QueryClient,
} from "@tanstack/react-query";
import type { ApiRequestOptions } from "@notrelix/contracts";
import type { FullBoardResponse } from "@notrelix/work-management-core";
import { findBoardCard, restoreBoardCard } from "../cache/card-restore";
import { wmQueryKeys } from "../queries/keys";
import { useWorkManagementServices } from "../services";
import { runCardCommand } from "./card-command";
import type { CardWriteHookOptions } from "./use-update-item-status";

interface ItemArchiver {
  archiveCard(
    cardId: string,
    expectedVersion: number | undefined,
    options?: ApiRequestOptions,
  ): Promise<void>;
}

/**
 * Archives an item (`POST /board-items/{id}/archive`; the item can be
 * restored, it is not deleted) with the version the board read carries. The
 * card leaves the board at once and comes back if the write fails.
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
  const board = queryClient.getQueryData<FullBoardResponse>(
    wmQueryKeys.fullBoard(workspaceId, boardId),
  );
  // The version the person saw; an item the board read does not carry (just
  // created) is read again by the client at send time.
  const expectedVersion = findBoardCard(board, cardId)?.version ?? undefined;
  return runCardCommand({
    queryClient,
    workspaceId,
    boardId,
    cardId,
    kind: "archive-item",
    applyBoard: (board) => restoreBoardCard(board, cardId, undefined) ?? board,
    send: (context) => cards.archiveCard(cardId, expectedVersion, context),
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
