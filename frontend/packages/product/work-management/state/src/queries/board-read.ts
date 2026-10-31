import { useEffect, useId, useRef, useState } from "react";
import type { QueryClient } from "@tanstack/react-query";
import { useQueryClient } from "@tanstack/react-query";
import type { FullBoardResponse } from "@notrelix/work-management-core";
import type { WorkManagementServices } from "../services";
import { appendLanePage, isTruncated, loadedByLane } from "../cache/lane-pages";
import { wmQueryKeys } from "./keys";
import { useWorkManagementServices } from "../services";

/** Items of each Status lane a board read brings with it. */
export const BOARD_FIRST_PAGE = 50;
/** Items a "show more" reads from the server. */
export const BOARD_LANE_PAGE = 50;
// Pages a refetch reads back to put a lane's loaded items where they were (about 2,000 items).
const RESTORE_PAGES = 40;

/** What the open view needs of the board read. */
export interface BoardReadNeed {
  /** Every item of the board, not pages (a view that lays cards out by anything but Status lanes). */
  readonly full: boolean;
  /** The filter and sort in the wire forms; empty: none. */
  readonly filter: string;
  readonly sort: string;
}

const needsByBoard = new Map<string, Map<string, BoardReadNeed>>();
/** Criteria the server refused: the board is read whole under them. */
const refused = new Set<string>();

const boardKey = (workspaceId: string, boardId: string) =>
  `${workspaceId}:${boardId}`;
const criteriaKeyOf = (need: Pick<BoardReadNeed, "filter" | "sort">) =>
  `${need.filter}|${need.sort}`;

/** What every mounted view of the board needs together: one view wanting everything reads everything. */
export function resolveReadNeed(
  workspaceId: string,
  boardId: string,
): BoardReadNeed {
  let full = false;
  let filter = "";
  let sort = "";
  // A view brings the filter, another the sort (the table sorts by its own columns): each is the last one set.
  for (const need of needsByBoard
    .get(boardKey(workspaceId, boardId))
    ?.values() ?? []) {
    if (need.full) full = true;
    if (need.filter) filter = need.filter;
    if (need.sort) sort = need.sort;
  }
  const refusedHere = refused.has(
    `${boardKey(workspaceId, boardId)}|${criteriaKeyOf({ filter, sort })}`,
  );
  return { full: full || refusedHere, filter, sort };
}

/** Whether the server refused to read a board under these criteria. */
function isRefusal(error: unknown): boolean {
  return (error as { kind?: string } | null)?.kind === "validation";
}

/**
 * Reads the board the way the open views need it: whole when one asks for it, else the first page of each
 * Status lane under the views' filter and sort. A refusal of the criteria falls back to the whole board, and
 * a refetch puts the pages the person had loaded back where they were.
 */
export async function readBoard(
  services: Pick<WorkManagementServices, "boards">,
  queryClient: QueryClient,
  workspaceId: string,
  boardId: string,
): Promise<FullBoardResponse> {
  const { boards } = services;
  const need = resolveReadNeed(workspaceId, boardId);
  if (need.full) return boards.getFullBoard(boardId, { workspaceId });
  let read: FullBoardResponse;
  try {
    read = await boards.getFullBoard(boardId, {
      workspaceId,
      page: { size: BOARD_FIRST_PAGE, filter: need.filter, sort: need.sort },
    });
  } catch (error) {
    if (!isRefusal(error) || (!need.filter && !need.sort)) throw error;
    refused.add(`${boardKey(workspaceId, boardId)}|${criteriaKeyOf(need)}`);
    return boards.getFullBoard(boardId, { workspaceId });
  }
  const before = queryClient.getQueryData<FullBoardResponse>(
    wmQueryKeys.fullBoard(workspaceId, boardId),
  );
  if (
    !before?.paging ||
    !read.paging ||
    criteriaKeyOf(before.paging.criteria) !== criteriaKeyOf(need)
  ) {
    return read;
  }
  // The person had pages loaded: read them again, lane by lane, until the lane is as long as it was.
  const wanted = loadedByLane(before);
  for (const key of Object.keys(read.paging.lanes)) {
    for (
      let pages = 0;
      pages < RESTORE_PAGES &&
      read.paging?.lanes[key]?.nextCursor &&
      (loadedByLane(read).get(key) ?? 0) < (wanted.get(key) ?? 0);
      pages += 1
    ) {
      try {
        const page = await boards.getLanePage(boardId, key, {
          cursor: read.paging.lanes[key]!.nextCursor!,
          limit: BOARD_LANE_PAGE,
          ...need,
          board: read.board,
          fieldDefinitions: read.fieldDefinitions,
        });
        read = appendLanePage(read, key, page);
      } catch {
        break; // what is loaded stays; the person presses "show more" again
      }
    }
  }
  return read;
}

/**
 * Tells the board read what the calling view needs of it (see {@link BoardReadNeed}). A change that the cached
 * read cannot serve (a lane the cache holds only part of, under other criteria; a view that wants everything)
 * reads the board again.
 */
export function useBoardReadNeed(
  boardId: string | undefined,
  workspaceId: string | undefined,
  need: BoardReadNeed,
): void {
  const owner = useId();
  const queryClient = useQueryClient();
  const { full, filter, sort } = need;
  useEffect(() => {
    if (!boardId || !workspaceId) return;
    const key = boardKey(workspaceId, boardId);
    const owners = needsByBoard.get(key) ?? new Map<string, BoardReadNeed>();
    needsByBoard.set(key, owners);
    owners.set(owner, { full, filter, sort });
    const queryKey = wmQueryKeys.fullBoard(workspaceId, boardId);
    const cached = queryClient.getQueryData<FullBoardResponse>(queryKey);
    const resolved = resolveReadNeed(workspaceId, boardId);
    const stale =
      cached?.paging !== undefined &&
      (resolved.full ||
        (isTruncated(cached) &&
          criteriaKeyOf(cached.paging.criteria) !== criteriaKeyOf(resolved)));
    if (stale) void queryClient.refetchQueries({ queryKey, exact: true });
    return () => {
      owners.delete(owner);
      if (owners.size === 0) needsByBoard.delete(key);
    };
  }, [boardId, workspaceId, owner, full, filter, sort, queryClient]);
}

/** Where each lane's "show more" stands: reading, or failed (the loaded cards stay, the person can retry). */
export interface LanePagerState {
  readonly loading: ReadonlySet<string>;
  readonly failed: ReadonlySet<string>;
}

const NO_LANES: ReadonlySet<string> = new Set();

/**
 * Reads the next page of a Status lane into the board read. One read per lane at a time; a refused cursor
 * (the lane changed under it) reads the board again.
 */
export function useLanePager(
  boardId: string | undefined,
  workspaceId: string | undefined,
) {
  const queryClient = useQueryClient();
  const { boards } = useWorkManagementServices();
  const busy = useRef(new Set<string>());
  const [state, setState] = useState<LanePagerState>({
    loading: NO_LANES,
    failed: NO_LANES,
  });
  const mark = (key: string, change: { loading?: boolean; failed?: boolean }) =>
    setState((current) => {
      const flip = (set: ReadonlySet<string>, on: boolean | undefined) => {
        if (on === undefined || set.has(key) === on) return set;
        const next = new Set(set);
        if (on) next.add(key);
        else next.delete(key);
        return next;
      };
      return {
        loading: flip(current.loading, change.loading),
        failed: flip(current.failed, change.failed),
      };
    });
  const loadMore = async (laneKey: string): Promise<void> => {
    if (!boardId || !workspaceId || busy.current.has(laneKey)) return;
    const queryKey = wmQueryKeys.fullBoard(workspaceId, boardId);
    const board = queryClient.getQueryData<FullBoardResponse>(queryKey);
    const lane = board?.paging?.lanes[laneKey];
    if (!board?.paging || !lane?.nextCursor) return;
    busy.current.add(laneKey);
    mark(laneKey, { loading: true, failed: false });
    try {
      const page = await boards.getLanePage(boardId, laneKey, {
        cursor: lane.nextCursor,
        limit: BOARD_LANE_PAGE,
        filter: board.paging.criteria.filter,
        sort: board.paging.criteria.sort,
        board: board.board,
        fieldDefinitions: board.fieldDefinitions,
      });
      queryClient.setQueryData<FullBoardResponse>(queryKey, (current) =>
        current ? appendLanePage(current, laneKey, page) : current,
      );
    } catch (error) {
      mark(laneKey, { failed: true });
      // The cursor no longer fits the lane: the first pages are read again.
      if (isRefusal(error)) {
        void queryClient.refetchQueries({ queryKey, exact: true });
      }
    } finally {
      busy.current.delete(laneKey);
      mark(laneKey, { loading: false });
    }
  };
  return { loadMore, ...state };
}
