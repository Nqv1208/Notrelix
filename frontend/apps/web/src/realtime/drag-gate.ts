import {
  isRealtimeRefetchHeld,
  onRealtimeRefetchRelease,
} from "@notrelix/work-management-state";

type Invalidate = (keys: readonly unknown[][]) => Promise<void>;

/**
 * Holds a refetch while a drag is in flight so no card reorders under the pointer;
 * what was held runs once when the drag ends (the board re-reads after the drop).
 * A pointer drag (the kanban boards) holds through `holdRealtimeRefetch`; a native
 * drag (calendar, timeline) is seen on the document, where `drop` and `mouseup` also
 * end it since `dragend` is not fired when the dragged element left the DOM.
 */
export function createDragGate(
  target: Document,
  invalidate: Invalidate,
): { readonly invalidate: Invalidate; readonly dispose: () => void } {
  let nativeDrag = false;
  let held = new Map<string, unknown[]>();

  const flushIfFree = () => {
    if (nativeDrag || isRealtimeRefetchHeld()) return;
    const keys = [...held.values()];
    held = new Map();
    if (keys.length > 0) void invalidate(keys);
  };
  const start = () => {
    nativeDrag = true;
  };
  const end = () => {
    nativeDrag = false;
    flushIfFree();
  };
  target.addEventListener("dragstart", start, true);
  for (const type of ["dragend", "drop", "mouseup"])
    target.addEventListener(type, end, true);
  const unsubscribe = onRealtimeRefetchRelease(flushIfFree);

  return {
    invalidate: async (keys) => {
      if (!nativeDrag && !isRealtimeRefetchHeld()) return invalidate(keys);
      for (const key of keys) held.set(JSON.stringify(key), key);
    },
    dispose: () => {
      unsubscribe();
      target.removeEventListener("dragstart", start, true);
      for (const type of ["dragend", "drop", "mouseup"])
        target.removeEventListener(type, end, true);
    },
  };
}
