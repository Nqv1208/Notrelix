import {
  groupCardsByStatus,
  NO_STATUS_LANE,
  type FullBoardResponse,
  type KanbanStatusLane,
} from "@notrelix/work-management-core";
import type { LanePage } from "../api/board-api-mappers";

/** The Status lane a card belongs to in a paged read: its option, or `none`. */
export const laneKeyOf = (lane: Pick<KanbanStatusLane, "optionId">): string =>
  lane.optionId ?? NO_STATUS_LANE;

/** Cards the cache holds per lane (what a refetch must put back). */
export function loadedByLane(board: FullBoardResponse): Map<string, number> {
  return new Map(
    groupCardsByStatus({
      fieldDefinitions: board.fieldDefinitions,
      groups: board.groups,
    }).map((lane) => [laneKeyOf(lane), lane.cards.length]),
  );
}

/**
 * Puts one more page of a lane into the board read: each card goes to its group, a card already held is
 * replaced where it sits (dedupe by id), and the lane takes the page's sizes and next cursor. The lane's cards
 * the cache gained before the page came stay counted: `baseLoaded` moves only by what the page added.
 */
export function appendLanePage(
  board: FullBoardResponse,
  laneKey: string,
  page: LanePage,
): FullBoardResponse {
  const lane = board.paging?.lanes[laneKey];
  if (!board.paging || !lane) return board;
  const held = new Set(board.groups.flatMap((g) => g.cards.map((c) => c.id)));
  const added = page.cards.filter(({ card }) => !held.has(card.id)).length;
  const groups = board.groups.map((group) => {
    const incoming = page.cards.filter((entry) => entry.groupId === group.id);
    if (incoming.length === 0) return group;
    const byId = new Map(incoming.map((entry) => [entry.card.id, entry.card]));
    let next = Math.max(0, ...group.cards.map((card) => card.position)) + 1;
    const replaced = group.cards.map((card) => {
      const fresh = byId.get(card.id);
      if (!fresh) return card;
      byId.delete(card.id);
      return { ...fresh, position: card.position };
    });
    const appended = [...byId.values()].map((card) => ({
      ...card,
      position: next++,
    }));
    return { ...group, cards: [...replaced, ...appended] };
  });
  return {
    ...board,
    groups,
    paging: {
      ...board.paging,
      lanes: {
        ...board.paging.lanes,
        [laneKey]: {
          total: page.total,
          filteredTotal: page.filteredTotal,
          nextCursor: page.nextCursor,
          baseLoaded: lane.baseLoaded + added,
        },
      },
    },
  };
}

/**
 * Whether the loaded cards fall short of the board: a lane the server holds more of than the cache does.
 * A board read under a filter is short whenever the filter left something out.
 */
export function isTruncated(board: FullBoardResponse | undefined): boolean {
  return Object.values(board?.paging?.lanes ?? {}).some(
    (lane) => lane.baseLoaded < lane.total,
  );
}
