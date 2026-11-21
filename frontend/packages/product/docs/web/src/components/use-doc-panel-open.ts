import { useCallback, useState } from "react";

/** The subset of `Storage` the panel preference needs (tests pass a fake). */
export interface PanelStorage {
  getItem(key: string): string | null;
  setItem(key: string, value: string): void;
}

export function docPanelStorageKey(viewerId: string | null): string {
  return `notrelix.docs.sidePanelOpen.${viewerId ?? "anonymous"}`;
}

function defaultStorage(): PanelStorage | null {
  try {
    return typeof window === "undefined" ? null : window.localStorage;
  } catch {
    // Storage blocked (privacy mode, sandboxed frame): keep state in memory.
    return null;
  }
}

/** The panel is a sheet over the page below the `sm` breakpoint, so a phone starts with it closed. */
function openByDefault(): boolean {
  try {
    return !window.matchMedia("(max-width: 639px)").matches;
  } catch {
    return true;
  }
}

function read(storage: PanelStorage | null, key: string): boolean {
  if (!storage) return openByDefault();
  try {
    const saved = storage.getItem(key);
    return saved === null ? openByDefault() : saved !== "0";
  } catch {
    return openByDefault();
  }
}

/**
 * Whether the document side panel is open: a per-viewer UI preference
 * (FE-AGENTS §28), not server state. Open by default on wide screens, closed on a phone; the choice survives
 * reloads and a blocked store only costs the memory.
 */
export function useDocPanelOpen(
  viewerId: string | null,
  storage: PanelStorage | null = defaultStorage(),
): readonly [boolean, (open: boolean) => void] {
  const key = docPanelStorageKey(viewerId);
  const [state, setState] = useState(() => ({
    key,
    open: read(storage, key),
  }));
  // A different viewer reads their own saved choice.
  const open = state.key === key ? state.open : read(storage, key);
  const setOpen = useCallback(
    (next: boolean) => {
      setState({ key, open: next });
      try {
        storage?.setItem(key, next ? "1" : "0");
      } catch {
        // Quota or blocked storage: the preference is a convenience.
      }
    },
    [key, storage],
  );
  return [open, setOpen] as const;
}
