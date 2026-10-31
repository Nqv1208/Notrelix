import { useEffect } from "react";

let holds = 0;
const releases = new Set<() => void>();

/** A drag or another pointer interaction is in flight: a realtime refetch waits until every hold is released. */
export function holdRealtimeRefetch(): () => void {
  holds += 1;
  let released = false;
  return () => {
    if (released) return;
    released = true;
    holds -= 1;
    if (holds === 0) for (const listener of [...releases]) listener();
  };
}

export const isRealtimeRefetchHeld = () => holds > 0;

/** Runs `listener` each time the last hold is released; returns the unsubscribe. */
export function onRealtimeRefetchRelease(listener: () => void): () => void {
  releases.add(listener);
  return () => releases.delete(listener);
}

/** Holds realtime refetches while `active` (a card is being dragged). */
export function useHoldRealtimeRefetch(active: boolean): void {
  useEffect(() => (active ? holdRealtimeRefetch() : undefined), [active]);
}
