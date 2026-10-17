import { useCallback, useMemo, useState } from "react";
import type { KeyValueStorage } from "@notrelix/runtime-web";

/** Persisted values: `"open" | "mini"` (the tablet band only). */
export const TABLET_SIDEBAR_STORAGE_KEY = "notrelix-sidebar-tablet";

function readOpen(storage: KeyValueStorage): boolean {
  try {
    return storage.getItem(TABLET_SIDEBAR_STORAGE_KEY) !== "mini";
  } catch {
    return true;
  }
}

export interface TabletSidebarPreference {
  readonly open: boolean;
  setOpen(open: boolean): void;
}

/**
 * The tablet sidebar choice (Final-T-* `sidebar`: 248 px open or the 64 px
 * icon column). Open by default; storage is injected like the rail and the
 * theme, and failures keep the choice for the session only. The desktop
 * sidebar is not affected.
 */
export function useTabletSidebarPreference(
  storage: KeyValueStorage,
): TabletSidebarPreference {
  const [open, setOpenState] = useState(() => readOpen(storage));
  const setOpen = useCallback(
    (next: boolean) => {
      try {
        storage.setItem(TABLET_SIDEBAR_STORAGE_KEY, next ? "open" : "mini");
      } catch {
        // Storage blocked: the choice lasts this session.
      }
      setOpenState(next);
    },
    [storage],
  );
  return useMemo(() => ({ open, setOpen }), [open, setOpen]);
}
