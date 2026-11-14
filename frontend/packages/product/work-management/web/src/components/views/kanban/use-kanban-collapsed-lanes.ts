import { useCallback, useMemo, useState } from "react";

/** The subset of `Storage` the preference needs (tests pass a fake). */
export interface CollapsedLanesStorage {
  getItem(key: string): string | null;
  setItem(key: string, value: string): void;
  removeItem(key: string): void;
}

/** Lane ids kept per member, workspace and view; a bound keeps the value small. */
const MAX_STORED = 200;

export function kanbanCollapsedLanesKey(
  workspaceId: string,
  viewId: string,
): string {
  return `notrelix.kanban.collapsed.${workspaceId}.${viewId}`;
}

/**
 * The Table keeps its collapsed groups the same way, per workspace and view (D38); a board with no saved Table
 * view yet keys them by the board, as before.
 */
export function tableCollapsedGroupsKey(
  workspaceId: string,
  viewOrBoardId: string,
): string {
  return `notrelix.table.collapsed.${workspaceId}.${viewOrBoardId}`;
}

function defaultStorage(): CollapsedLanesStorage | null {
  try {
    return typeof window === "undefined" ? null : window.localStorage;
  } catch {
    // Storage blocked (privacy mode, sandboxed frame): keep state in memory.
    return null;
  }
}

function read(
  storage: CollapsedLanesStorage | null,
  key: string,
): ReadonlySet<string> {
  if (!storage) return new Set();
  try {
    const raw = storage.getItem(key);
    const parsed: unknown = raw ? JSON.parse(raw) : [];
    return new Set(
      Array.isArray(parsed)
        ? parsed.filter((id): id is string => typeof id === "string")
        : [],
    );
  } catch {
    return new Set();
  }
}

function write(
  storage: CollapsedLanesStorage | null,
  key: string,
  lanes: ReadonlySet<string>,
): void {
  if (!storage) return;
  try {
    if (lanes.size === 0) storage.removeItem(key);
    else storage.setItem(key, JSON.stringify([...lanes].slice(-MAX_STORED)));
  } catch {
    // Quota or blocked storage: collapsing is a convenience, never an error.
  }
}

/**
 * The lanes a member keeps collapsed on a view: a personal, per-workspace,
 * per-view UI preference kept on this device (not server state). The value is
 * read when the view opens and again only when the key changes (the saved
 * view's id arrives after the first paint), so a refetch never re-expands what
 * the member collapsed.
 */
export function useKanbanCollapsedLanes(
  key: string,
  storage: CollapsedLanesStorage | null = defaultStorage(),
) {
  const [read0] = useState(() => ({ key, ids: read(storage, key) }));
  const initial = useMemo(
    () => (key === read0.key ? read0.ids : read(storage, key)),
    // The device storage is read again per key only, never per render.
    [key],
  );
  const onChange = useCallback(
    (lanes: ReadonlySet<string>) => write(storage, key, lanes),
    [storage, key],
  );
  return { initial, onChange };
}

/**
 * The collapsed set itself, for a container that owns it (the Table): read from this device when the view
 * opens and again when the key changes (the saved view arrives, or the tabs switch view), written on every
 * change. A refetch of the items never touches it, so a collapsed group stays collapsed.
 */
export function useCollapsedSet(
  key: string,
  storage: CollapsedLanesStorage | null = defaultStorage(),
) {
  const [state, setState] = useState(() => ({
    key,
    ids: read(storage, key),
  }));
  let ids = state.ids;
  if (state.key !== key) {
    ids = read(storage, key);
    setState({ key, ids });
  }
  const update = useCallback(
    (next: ReadonlySet<string>) => {
      setState({ key, ids: next });
      write(storage, key, next);
    },
    [storage, key],
  );
  return [ids, update] as const;
}
