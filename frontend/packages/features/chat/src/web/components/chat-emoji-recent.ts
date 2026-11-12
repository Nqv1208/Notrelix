import { useSyncExternalStore } from "react";
import { pushRecent } from "../../core/model/emoji-model";

/**
 * The emoji the caller used lately, for the "Recent" row. Kept in memory for the life of the page only:
 * the feature package keeps nothing in browser storage.
 */
let recent: readonly string[] = [];
const listeners = new Set<() => void>();

function subscribe(listener: () => void): () => void {
  listeners.add(listener);
  return () => {
    listeners.delete(listener);
  };
}

export function rememberEmoji(char: string): void {
  recent = pushRecent(recent, char);
  for (const listener of listeners) listener();
}

/** Empties the list (used by tests; a page never needs it). */
export function resetRecentEmoji(): void {
  recent = [];
  for (const listener of listeners) listener();
}

export function useRecentEmoji(): readonly string[] {
  return useSyncExternalStore(
    subscribe,
    () => recent,
    () => recent,
  );
}
