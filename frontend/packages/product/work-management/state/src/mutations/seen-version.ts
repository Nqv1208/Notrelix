import type { QueryClient } from "@tanstack/react-query";
import type {
  CardDetail,
  FullBoardResponse,
} from "@notrelix/work-management-core";
import { findBoardCard } from "../cache/card-restore";
import { wmQueryKeys } from "../queries/keys";
import { UnknownItemVersionError } from "./use-move-card";

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

/** The version a row the person saw carries; a row without one is refused before anything is sent. */
export function neededVersion(
  version: number | null | undefined,
  itemId: string,
): number {
  if (!version || version <= 0) throw new UnknownItemVersionError(itemId);
  return version;
}

/**
 * The version a write must name: the one the person saw (the cache the screen
 * renders from), never a fresh read. An item whose version the cache does not
 * hold is refused before anything is sent.
 */
export function requireSeenItemVersion(
  queryClient: QueryClient,
  scope: { workspaceId: string; boardId: string; cardId: string },
): number {
  const version = seenItemVersion(queryClient, scope);
  if (version === undefined) throw new UnknownItemVersionError(scope.cardId);
  return version;
}
