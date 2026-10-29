/**
 * What a saved filter keeps of the board's filter bar: the route `filter`
 * string (`serializeKanbanFilter` form) and the grouping field. Sort is a
 * setting the shared view owns, so a saved filter does not carry it.
 */
export interface SavedFilterCriteria {
  readonly filter?: string;
  readonly groupBy?: string;
  /** Kanban swimlane field (second dimension). Absent in version 1. */
  readonly swimlaneBy?: string;
  /** Sort levels; only a member's personal view state keeps it (a saved filter has none). */
  readonly sort?: string;
}

/**
 * Criteria JSON version. 1 (no `v` key) held a legacy facet `filter` string and
 * `groupBy`; 2 adds `swimlaneBy`, and `filter` may be a `c2:` compound filter.
 * Both parse; the `filter` string tells its own form apart.
 */
export const SAVED_FILTER_CRITERIA_VERSION = 2;

/** A named filter of one board, personal or shared with the board. */
export interface SavedFilter {
  readonly id: string;
  readonly boardId: string;
  readonly name: string;
  /** "board": every member who can view the board sees it. "me": its creator's alone. */
  readonly visibility: "board" | "me";
  /** True when the signed-in member created it. */
  readonly isMine: boolean;
  readonly criteria: SavedFilterCriteria;
  /** Expected version of the next change. */
  readonly version: number;
}

/** The criteria JSON a saved filter stores; unknown keys and bad values are dropped. */
export function parseSavedFilterCriteria(
  raw: string | null | undefined,
): SavedFilterCriteria {
  if (!raw) return {};
  try {
    const value: unknown = JSON.parse(raw);
    if (typeof value !== "object" || value === null) return {};
    const { filter, groupBy, swimlaneBy, sort } = value as Record<
      string,
      unknown
    >;
    return {
      ...(typeof filter === "string" && filter !== "" ? { filter } : {}),
      ...(typeof groupBy === "string" && groupBy !== "" ? { groupBy } : {}),
      ...(typeof swimlaneBy === "string" && swimlaneBy !== ""
        ? { swimlaneBy }
        : {}),
      ...(typeof sort === "string" && sort !== "" ? { sort } : {}),
    };
  } catch {
    return {};
  }
}

export function serializeSavedFilterCriteria(
  criteria: SavedFilterCriteria,
): string {
  return JSON.stringify({
    v: SAVED_FILTER_CRITERIA_VERSION,
    ...(criteria.filter ? { filter: criteria.filter } : {}),
    ...(criteria.groupBy ? { groupBy: criteria.groupBy } : {}),
    ...(criteria.swimlaneBy ? { swimlaneBy: criteria.swimlaneBy } : {}),
    ...(criteria.sort ? { sort: criteria.sort } : {}),
  });
}

/** The server's cap on the criteria JSON, in UTF-8 bytes. */
export const MAX_CRITERIA_BYTES = 8192;

/** Whether the criteria JSON is over the server's cap (the request would be refused). */
export const criteriaTooLarge = (criteria: SavedFilterCriteria): boolean =>
  new TextEncoder().encode(serializeSavedFilterCriteria(criteria)).length >
  MAX_CRITERIA_BYTES;
