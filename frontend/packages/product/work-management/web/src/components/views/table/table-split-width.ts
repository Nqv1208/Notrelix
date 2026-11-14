/**
 * The width of the item pane in the tablet Table split (`Final-T-Table` chia-doi), as a share of the view.
 * The artboard draws the pane at 46 %; the person may drag the grip between the two limits, and the width
 * they leave is kept in this browser.
 */
export const SPLIT_PANE_DEFAULT = 0.46;
export const SPLIT_PANE_MIN = 0.3;
export const SPLIT_PANE_MAX = 0.7;
/** One keyboard step on the grip, as a share of the view. */
export const SPLIT_PANE_STEP = 0.02;
export const SPLIT_PANE_STORAGE_KEY = "notrelix.table.split-pane-width";

/** Clamps a share to the limits; anything that is not a number falls back to the artboard's 46 %. */
export function clampSplitWidth(value: number): number {
  if (!Number.isFinite(value)) return SPLIT_PANE_DEFAULT;
  return Math.min(SPLIT_PANE_MAX, Math.max(SPLIT_PANE_MIN, value));
}

/** The pane share for a pointer at `clientX` on a view that spans `rect` (the pane sits at its right edge). */
export function splitWidthAt(
  clientX: number,
  rect: { readonly left: number; readonly width: number },
): number {
  if (rect.width <= 0) return SPLIT_PANE_DEFAULT;
  return clampSplitWidth((rect.left + rect.width - clientX) / rect.width);
}

/** The stored width, or the default when nothing valid is stored (or storage is unavailable). */
export function readSplitWidth(
  storage: Pick<Storage, "getItem"> | null | undefined,
): number {
  if (!storage) return SPLIT_PANE_DEFAULT;
  try {
    const raw = storage.getItem(SPLIT_PANE_STORAGE_KEY);
    if (raw === null) return SPLIT_PANE_DEFAULT;
    const value = Number(raw);
    return Number.isFinite(value) ? clampSplitWidth(value) : SPLIT_PANE_DEFAULT;
  } catch {
    return SPLIT_PANE_DEFAULT;
  }
}

/** Keeps the width for the next visit; a refused write (private mode, full storage) is ignored. */
export function writeSplitWidth(
  storage: Pick<Storage, "setItem"> | null | undefined,
  value: number,
): void {
  if (!storage) return;
  try {
    storage.setItem(SPLIT_PANE_STORAGE_KEY, String(clampSplitWidth(value)));
  } catch {
    // Ignored: the width still applies for this visit.
  }
}

/** The browser's local storage, or null where the page may not read it. */
export function splitWidthStorage(): Storage | null {
  try {
    return typeof window === "undefined" ? null : window.localStorage;
  } catch {
    return null;
  }
}
