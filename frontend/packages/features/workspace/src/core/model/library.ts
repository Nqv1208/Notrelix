/**
 * Boards and Docs directory model (WX-DIR-001).
 *
 * Pure and framework-neutral. Entries are built by the host from the
 * authorized Board and Doc lists (Work Management, Documents) plus the
 * member's pins (Workspaces); this model only filters, sorts and selects.
 */

import {
  NAVIGATION_RESOURCE_KIND,
  navigationEntryKey,
  normalizeSearchText,
  type NavigationReference,
  type NavigationResourceKind,
} from "./navigation";

export interface LibraryEntry {
  readonly kind: NavigationResourceKind;
  readonly id: string;
  readonly name: string;
  /** Where the resource lives (parent Doc or workspace); `null` = unknown. */
  readonly location: string | null;
  /** ISO time of the last change, when the owning list reports one. */
  readonly updatedAt: string | null;
  readonly pinned: boolean;
}

export type LibraryTypeFilter = "all" | "board" | "doc";
export type LibrarySortField = "name" | "type" | "location" | "updated";
export type LibrarySortDirection = "asc" | "desc";

export interface LibrarySort {
  readonly field: LibrarySortField;
  readonly direction: LibrarySortDirection;
}

export interface LibraryQuery {
  readonly type: LibraryTypeFilter;
  readonly text: string;
  readonly sort: LibrarySort;
}

export const DEFAULT_LIBRARY_QUERY: LibraryQuery = {
  type: "all",
  text: "",
  sort: { field: "updated", direction: "desc" },
};

/** Rows above this count are windowed (DESIGN.md §17.4). */
export const LIBRARY_VIRTUALIZATION_THRESHOLD = 100;

export function libraryEntryKey(
  entry: Pick<LibraryEntry, "kind" | "id">,
): string {
  return navigationEntryKey(entry.kind, entry.id);
}

export interface LibrarySource {
  readonly boards: readonly {
    readonly id: string;
    readonly name: string;
    readonly updatedAt?: string | null;
    readonly location?: string | null;
  }[];
  readonly docs: readonly {
    readonly id: string;
    readonly name: string;
    readonly updatedAt?: string | null;
    readonly location?: string | null;
  }[];
  readonly pins: readonly NavigationReference[];
}

/** Builds entries from the authorized lists; pins only mark entries. */
export function buildLibraryEntries(source: LibrarySource): LibraryEntry[] {
  const pinned = new Set(
    source.pins.map((pin) => navigationEntryKey(pin.kind, pin.resourceId)),
  );
  const entries: LibraryEntry[] = [];
  for (const board of source.boards) {
    entries.push({
      kind: NAVIGATION_RESOURCE_KIND.board,
      id: board.id,
      name: board.name,
      location: board.location ?? null,
      updatedAt: board.updatedAt ?? null,
      pinned: pinned.has(
        navigationEntryKey(NAVIGATION_RESOURCE_KIND.board, board.id),
      ),
    });
  }
  for (const doc of source.docs) {
    entries.push({
      kind: NAVIGATION_RESOURCE_KIND.doc,
      id: doc.id,
      name: doc.name,
      location: doc.location ?? null,
      updatedAt: doc.updatedAt ?? null,
      pinned: pinned.has(
        navigationEntryKey(NAVIGATION_RESOURCE_KIND.doc, doc.id),
      ),
    });
  }
  return entries;
}

function matchesType(entry: LibraryEntry, type: LibraryTypeFilter): boolean {
  if (type === "all") return true;
  return type === "board"
    ? entry.kind === NAVIGATION_RESOURCE_KIND.board
    : entry.kind === NAVIGATION_RESOURCE_KIND.doc;
}

const collator = new Intl.Collator(undefined, {
  sensitivity: "base",
  numeric: true,
});

function compare(
  a: LibraryEntry,
  b: LibraryEntry,
  field: LibrarySortField,
): number {
  switch (field) {
    case "name":
      return collator.compare(a.name, b.name);
    case "type":
      return a.kind.localeCompare(b.kind) || collator.compare(a.name, b.name);
    case "location":
      return (
        collator.compare(a.location ?? "", b.location ?? "") ||
        collator.compare(a.name, b.name)
      );
    case "updated": {
      // Unknown times sort after known ones in both directions.
      if (a.updatedAt === b.updatedAt) return collator.compare(a.name, b.name);
      if (a.updatedAt === null) return 1;
      if (b.updatedAt === null) return -1;
      return a.updatedAt.localeCompare(b.updatedAt);
    }
  }
}

/** Filters by type and text (case/diacritic-insensitive), then sorts. */
export function queryLibrary(
  entries: readonly LibraryEntry[],
  query: LibraryQuery,
): LibraryEntry[] {
  const needle = normalizeSearchText(query.text.trim());
  const filtered = entries.filter(
    (entry) =>
      matchesType(entry, query.type) &&
      (needle === "" ||
        normalizeSearchText(`${entry.name} ${entry.location ?? ""}`).includes(
          needle,
        )),
  );
  const { field, direction } = query.sort;
  const sign = direction === "asc" ? 1 : -1;
  return filtered.sort((a, b) => {
    const result = compare(a, b, field);
    // Keep unknown update times last whichever way the column sorts.
    if (
      field === "updated" &&
      (a.updatedAt === null) !== (b.updatedAt === null)
    )
      return result;
    return result * sign;
  });
}

/** Toggles sort on a column: same field flips, a new field starts ascending
 * (descending for "updated", newest first). */
export function nextLibrarySort(
  current: LibrarySort,
  field: LibrarySortField,
): LibrarySort {
  if (current.field === field) {
    return { field, direction: current.direction === "asc" ? "desc" : "asc" };
  }
  return { field, direction: field === "updated" ? "desc" : "asc" };
}

/** The visible window of rows for a scroll position (fixed row height). */
export function libraryWindow(input: {
  readonly rowCount: number;
  readonly rowHeight: number;
  readonly scrollTop: number;
  readonly viewportHeight: number;
  readonly overscan?: number;
}): { readonly start: number; readonly end: number } {
  const overscan = input.overscan ?? 8;
  if (input.rowCount === 0) return { start: 0, end: 0 };
  const first = Math.floor(Math.max(0, input.scrollTop) / input.rowHeight);
  const visible = Math.ceil(input.viewportHeight / input.rowHeight) + 1;
  const start = Math.max(0, first - overscan);
  const end = Math.min(input.rowCount, first + visible + overscan);
  return { start, end };
}
