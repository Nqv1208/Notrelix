type Invalidate = (keys: readonly unknown[][]) => Promise<void>;

/**
 * Holds a refetch while a native drag is in flight so no card reorders under the
 * pointer; what was held runs once when the drag ends (the board re-reads after
 * the drop). `drop` and `mouseup` also end it, since `dragend` is not fired when
 * the dragged element left the DOM.
 */
export function createDragGate(
  target: Document,
  invalidate: Invalidate,
): { readonly invalidate: Invalidate; readonly dispose: () => void } {
  let dragging = false;
  let held = new Map<string, unknown[]>();

  const flush = () => {
    dragging = false;
    const keys = [...held.values()];
    held = new Map();
    if (keys.length > 0) void invalidate(keys);
  };
  const start = () => {
    dragging = true;
  };
  target.addEventListener("dragstart", start, true);
  for (const type of ["dragend", "drop", "mouseup"])
    target.addEventListener(type, flush, true);

  return {
    invalidate: async (keys) => {
      if (!dragging) return invalidate(keys);
      for (const key of keys) held.set(JSON.stringify(key), key);
    },
    dispose: () => {
      target.removeEventListener("dragstart", start, true);
      for (const type of ["dragend", "drop", "mouseup"])
        target.removeEventListener(type, flush, true);
    },
  };
}
