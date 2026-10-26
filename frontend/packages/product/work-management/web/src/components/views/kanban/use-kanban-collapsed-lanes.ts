import { useCallback, useState } from "react";

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
 * The lanes a member keeps collapsed on a Kanban view: a personal,
 * per-workspace, per-view UI preference kept on this device (not server
 * state). The first value is read once when the view opens.
 */
export function useKanbanCollapsedLanes(
  key: string,
  storage: CollapsedLanesStorage | null = defaultStorage(),
) {
  const [initial] = useState(() => read(storage, key));
  const onChange = useCallback(
    (lanes: ReadonlySet<string>) => write(storage, key, lanes),
    [storage, key],
  );
  return { initial, onChange };
}
