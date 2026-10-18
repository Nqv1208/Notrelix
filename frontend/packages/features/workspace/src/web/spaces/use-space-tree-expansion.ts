import { useCallback, useEffect, useMemo, useState } from "react";

/** The subset of `Storage` the expansion state needs (tests pass a fake). */
export interface ExpansionStorage {
  getItem(key: string): string | null;
  setItem(key: string, value: string): void;
}

/** Ids kept per member and workspace; a bound keeps the stored value small. */
const MAX_STORED = 500;

export function spaceExpansionStorageKey(
  userId: string,
  workspaceId: string,
): string {
  return `notrelix.spaces.expanded.${userId}.${workspaceId}`;
}

function defaultStorage(): ExpansionStorage | null {
  try {
    return typeof window === "undefined" ? null : window.localStorage;
  } catch {
    // Storage blocked (privacy mode, sandboxed frame): keep state in memory.
    return null;
  }
}

function read(storage: ExpansionStorage | null, key: string | null): string[] {
  if (!storage || !key) return [];
  try {
    const raw = storage.getItem(key);
    const parsed: unknown = raw ? JSON.parse(raw) : [];
    return Array.isArray(parsed)
      ? parsed.filter((id): id is string => typeof id === "string")
      : [];
  } catch {
    return [];
  }
}

function write(
  storage: ExpansionStorage | null,
  key: string | null,
  ids: readonly string[],
): void {
  if (!storage || !key) return;
  try {
    storage.setItem(key, JSON.stringify(ids.slice(-MAX_STORED)));
  } catch {
    // Quota or blocked storage: expansion is a convenience, never an error.
  }
}

/**
 * Which Spaces a member keeps open in the sidebar tree — a persisted
 * per-member, per-workspace UI preference (FE-AGENTS §28), not server state.
 * `reveal` opens the ancestors of the current Space without collapsing
 * anything the member opened.
 */
export function useSpaceTreeExpansion(
  userId: string | null,
  workspaceId: string,
  storage: ExpansionStorage | null = defaultStorage(),
) {
  const key = userId ? spaceExpansionStorageKey(userId, workspaceId) : null;
  const [state, setState] = useState(() => ({ key, ids: read(storage, key) }));

  // A new member or workspace is a new scope: load its own stored state.
  useEffect(() => {
    if (state.key !== key) setState({ key, ids: read(storage, key) });
  }, [key, state.key, storage]);

  const update = useCallback(
    (next: (ids: readonly string[]) => string[]) => {
      setState((current) => {
        const ids = next(current.ids);
        if (
          ids.length === current.ids.length &&
          ids.every((id, index) => id === current.ids[index])
        ) {
          return current;
        }
        write(storage, current.key, ids);
        return { key: current.key, ids };
      });
    },
    [storage],
  );

  const setExpanded = useCallback(
    (spaceId: string, expanded: boolean) =>
      update((ids) =>
        expanded
          ? ids.includes(spaceId)
            ? [...ids]
            : [...ids, spaceId]
          : ids.filter((id) => id !== spaceId),
      ),
    [update],
  );

  const reveal = useCallback(
    (spaceIds: readonly string[]) =>
      update((ids) => [...ids, ...spaceIds.filter((id) => !ids.includes(id))]),
    [update],
  );

  const expandedIds = useMemo(() => new Set(state.ids), [state.ids]);
  return { expandedIds, setExpanded, reveal };
}
