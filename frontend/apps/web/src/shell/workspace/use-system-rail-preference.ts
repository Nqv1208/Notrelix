import { useCallback, useMemo, useState } from "react";
import type { SystemRailPresentation } from "@notrelix/features-workspace/web";
import type { KeyValueStorage } from "@notrelix/runtime-web";

/** Persisted values: `"hidden" | "pinned"`. The overlay is never persisted. */
export const SYSTEM_RAIL_STORAGE_KEY = "notrelix-system-rail";

type PersistedRail = "hidden" | "pinned";

function readStored(storage: KeyValueStorage): PersistedRail {
  try {
    return storage.getItem(SYSTEM_RAIL_STORAGE_KEY) === "pinned"
      ? "pinned"
      : "hidden";
  } catch {
    return "hidden";
  }
}

function writeStored(storage: KeyValueStorage, value: PersistedRail) {
  try {
    storage.setItem(SYSTEM_RAIL_STORAGE_KEY, value);
  } catch {
    // Storage blocked (private mode, quota): the choice lasts this session.
  }
}

export interface SystemRailPreference {
  readonly presentation: SystemRailPresentation;
  /** Shows the overlay (a pinned rail stays pinned). Not persisted. */
  open(): void;
  /** Hides the rail; closing a pinned rail persists `hidden`. */
  close(): void;
  /** Pins (persisted `pinned`) or unpins to hidden (persisted `hidden`). */
  togglePinned(): void;
}

/**
 * The Notrelix system rail preference (Final-Space `nav`): hidden by
 * default, an unpersisted overlay from the workspace chip, and a pinned
 * rail that survives reloads. Storage is injected like the theme storage
 * (`runtime.storage`); failures degrade to hidden.
 */
export function useSystemRailPreference(
  storage: KeyValueStorage,
): SystemRailPreference {
  const [presentation, setPresentation] = useState<SystemRailPresentation>(() =>
    readStored(storage),
  );

  const open = useCallback(() => {
    setPresentation((current) => (current === "pinned" ? "pinned" : "overlay"));
  }, []);

  const close = useCallback(() => {
    setPresentation((current) => {
      if (current === "pinned") writeStored(storage, "hidden");
      return "hidden";
    });
  }, [storage]);

  const togglePinned = useCallback(() => {
    setPresentation((current) => {
      const next: PersistedRail = current === "pinned" ? "hidden" : "pinned";
      writeStored(storage, next);
      return next;
    });
  }, [storage]);

  return useMemo(
    () => ({ presentation, open, close, togglePinned }),
    [presentation, open, close, togglePinned],
  );
}
