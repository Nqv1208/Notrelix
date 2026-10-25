import { useCallback, useState } from "react";

/**
 * A presentation preference that lives in this browser only (date format,
 * sidebar default): Identity does not own it (docs/product/identity.md), so
 * it never reaches the backend. Storage can be missing or throw (private
 * window); the value then lasts for the page's life.
 */
export function useClientPreference<T extends string | boolean>(
  key: string,
  fallback: T,
): readonly [T, (next: T) => void] {
  const [value, setValue] = useState<T>(() => {
    try {
      const raw = window.localStorage.getItem(key);
      if (raw === null) return fallback;
      return (typeof fallback === "boolean" ? raw === "true" : raw) as T;
    } catch {
      return fallback;
    }
  });
  const set = useCallback(
    (next: T) => {
      setValue(next);
      try {
        window.localStorage.setItem(key, String(next));
      } catch {
        // Storage is unavailable: the choice holds until the page closes.
      }
    },
    [key],
  );
  return [value, set] as const;
}
