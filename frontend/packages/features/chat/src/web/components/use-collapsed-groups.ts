import { useCallback, useState } from "react";
import type { KeyValueStorage } from "@notrelix/platform";

const PREFIX = "notrelix.chat.collapsed";
const UNGROUPED = "ungrouped";

/** What the collapse needs of the host's storage: the web runtime's key-value port satisfies it. */
export type ChatCollapseStore = Pick<KeyValueStorage, "getItem" | "setItem">;

/** One key per person-browser, Workspace and category: the collapse is the reader's own and never leaves the device. */
export function collapseKey(workspaceId: string, groupId: string | null) {
  return `${PREFIX}.${workspaceId}.${groupId ?? UNGROUPED}`;
}

function readCollapsed(
  store: ChatCollapseStore | undefined,
  workspaceId: string,
  groupId: string | null,
): boolean {
  try {
    return store?.getItem(collapseKey(workspaceId, groupId)) === "1";
  } catch {
    // Blocked or private storage: every category shows open.
    return false;
  }
}

function writeCollapsed(
  store: ChatCollapseStore | undefined,
  workspaceId: string,
  groupId: string | null,
  collapsed: boolean,
) {
  try {
    store?.setItem(collapseKey(workspaceId, groupId), collapsed ? "1" : "0");
  } catch {
    // The choice then lasts only until the page reloads.
  }
}

/**
 * Which categories the reader collapsed (UC-CHT-A04): kept by the host's storage per Workspace and category, read
 * lazily so the sidebar renders open when there is no storage or it is blocked. The package never touches browser storage.
 */
export function useCollapsedGroups(
  workspaceId: string,
  store?: ChatCollapseStore,
) {
  const [overrides, setOverrides] = useState<ReadonlyMap<string, boolean>>(
    new Map(),
  );
  const isCollapsed = useCallback(
    (groupId: string | null) =>
      overrides.get(collapseKey(workspaceId, groupId)) ??
      readCollapsed(store, workspaceId, groupId),
    [overrides, store, workspaceId],
  );
  const toggle = useCallback(
    (groupId: string | null) => {
      const next = !isCollapsed(groupId);
      writeCollapsed(store, workspaceId, groupId, next);
      setOverrides((current) =>
        new Map(current).set(collapseKey(workspaceId, groupId), next),
      );
    },
    [isCollapsed, store, workspaceId],
  );
  return { isCollapsed, toggle };
}
