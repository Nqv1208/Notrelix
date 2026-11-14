import { useEffect, useSyncExternalStore } from "react";

/**
 * The words of the filter conditions the open view applies ("Phân hệ ERP là Kế toán"), published by the view's
 * state bar so a dialog about the view (export) can name what the file will follow. Display text only: the
 * filter itself travels as the board's `filter` search value.
 */
let labels: readonly string[] = [];
const listeners = new Set<() => void>();

function publish(next: readonly string[]) {
  if (next.length === labels.length && next.every((l, i) => l === labels[i]))
    return;
  labels = next;
  for (const listener of listeners) listener();
}

/** Called by the state bar: publishes while mounted, clears when it goes. */
export function usePublishFilterChipLabels(next: readonly string[]) {
  const key = next.join("\u0000");
  useEffect(() => {
    publish(key === "" ? [] : key.split("\u0000"));
    return () => publish([]);
  }, [key]);
}

export function useFilterChipLabels(): readonly string[] {
  return useSyncExternalStore(
    (listener) => {
      listeners.add(listener);
      return () => listeners.delete(listener);
    },
    () => labels,
    () => [],
  );
}
