import {
  useMutation,
  useQueryClient,
  type QueryClient,
} from "@tanstack/react-query";
import type { ApiRequestOptions } from "@notrelix/contracts";
import type {
  CardDetail,
  FullBoardResponse,
} from "@notrelix/work-management-core";
import { findBoardCard, restoreBoardCard } from "../cache/card-restore";
import { wmQueryKeys } from "../queries/keys";
import { useWorkManagementServices } from "../services";
import { runCardCommand } from "./card-command";
import { UnknownItemVersionError } from "./use-move-card";
import type { CardWriteHookOptions } from "./use-update-item-status";

interface ItemArchiver {
  archiveCard(
    cardId: string,
    expectedVersion: number,
    options?: ApiRequestOptions,
  ): Promise<void>;
}

/** What an archive names: the item and, when the dialog opened on one, the version it showed. */
export interface ArchiveCardInput {
  readonly cardId: string;
  readonly expectedVersion?: number;
}

/**
 * The version of an item as the person last saw it: the board read's or, for a
 * sub-item (not in the board read), the item read's. Undefined when neither
 * carries one.
 */
export function seenItemVersion(
  queryClient: QueryClient,
  {
    workspaceId,
    boardId,
    cardId,
  }: { workspaceId: string; boardId: string; cardId: string },
): number | undefined {
  const version = (
    findBoardCard(
      queryClient.getQueryData<FullBoardResponse>(
        wmQueryKeys.fullBoard(workspaceId, boardId),
      ),
      cardId,
    ) ??
    queryClient.getQueryData<CardDetail>(
      wmQueryKeys.cardDetail(workspaceId, cardId),
    )
  )?.version;
  return version && version > 0 ? version : undefined;
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
): Promise<void> {
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
    send: (context) => cards.archiveCard(cardId, expectedVersion, context),
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
