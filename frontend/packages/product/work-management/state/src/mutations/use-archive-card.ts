import {
  useMutation,
  useQueryClient,
  type QueryClient,
} from "@tanstack/react-query";
import type { ApiRequestOptions } from "@notrelix/contracts";
import { restoreBoardCard } from "../cache/card-restore";
import { useWorkManagementServices } from "../services";
import { runCardCommand } from "./card-command";
import { seenItemVersion } from "./seen-version";
import { UnknownItemVersionError } from "./use-move-card";
import type { CardWriteHookOptions } from "./use-update-item-status";

export { seenItemVersion };

interface ItemArchiver {
  archiveCard(
    cardId: string,
    expectedVersion: number,
    options?: ApiRequestOptions,
  ): Promise<{ version?: number } | void>;
}

/** What an archive names: the item and, when the dialog opened on one, the version it showed. */
export interface ArchiveCardInput {
  readonly cardId: string;
  readonly expectedVersion?: number;
}

/**
 * Archives an item (`POST /board-items/{id}/archive`; the item can be
 * restored, it is not deleted) with the version the person saw: the one the
 * archive dialog opened with, else the item's cached version. An item with no
 * known version is refused before anything is sent. The card leaves the board
 * at once and comes back if the write fails.
 */
export function runArchiveCardCommand(
  queryClient: QueryClient,
  cards: ItemArchiver,
  {
    workspaceId,
    boardId,
    cardId,
    expectedVersion: opened,
  }: { workspaceId: string; boardId: string } & ArchiveCardInput,
): Promise<{ version?: number }> {
  const expectedVersion =
    opened ?? seenItemVersion(queryClient, { workspaceId, boardId, cardId });
  if (!expectedVersion || expectedVersion <= 0) {
    return Promise.reject(new UnknownItemVersionError(cardId));
  }
  return runCardCommand({
    queryClient,
    workspaceId,
    boardId,
    cardId,
    kind: "archive-item",
    applyBoard: (board) => restoreBoardCard(board, cardId, undefined) ?? board,
    send: async (context) =>
      (await cards.archiveCard(cardId, expectedVersion, context)) ?? {},
  });
}

export function useArchiveCard(
  boardId: string,
  workspaceId: string,
  options: CardWriteHookOptions<ArchiveCardInput> = {},
) {
  const queryClient = useQueryClient();
  const { cards } = useWorkManagementServices();
  return useMutation({
    mutationFn: (input: ArchiveCardInput) =>
      runArchiveCardCommand(queryClient, cards, {
        workspaceId,
        boardId,
        ...input,
      }),
    onError: options.onError,
  });
}

/** Reads the version of an item as the person sees it now (see `seenItemVersion`). */
export function useSeenItemVersion(boardId: string, workspaceId: string) {
  const queryClient = useQueryClient();
  return (cardId: string) =>
    seenItemVersion(queryClient, { workspaceId, boardId, cardId });
}
