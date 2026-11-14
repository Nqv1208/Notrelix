import {
  createContext,
  useContext,
  useState,
  useSyncExternalStore,
} from "react";

/**
 * Which message row shows its action bar. One store per list, so exactly one bar is visible at a time: the row
 * under the pointer or the row that holds keyboard focus, whichever came last. Each row subscribes to "am I the
 * active one", so a pointer move re-renders only the two rows whose answer changed.
 */
export interface RowActivityStore {
  readonly active: () => string | null;
  readonly activate: (id: string) => void;
  /** Clears the active row only when it is still `id` (a late leave of another row does not hide the new one). */
  readonly release: (id: string) => void;
  readonly clear: () => void;
  readonly subscribe: (listener: () => void) => () => void;
}

export function createRowActivityStore(): RowActivityStore {
  let current: string | null = null;
  const listeners = new Set<() => void>();
  function set(next: string | null) {
    if (next === current) return;
    current = next;
    for (const listener of listeners) listener();
  }
  return {
    active: () => current,
    activate: (id) => set(id),
    release: (id) => {
      if (current === id) set(null);
    },
    clear: () => set(null),
    subscribe: (listener) => {
      listeners.add(listener);
      return () => listeners.delete(listener);
    },
  };
}

const RowActivityContext = createContext<RowActivityStore | null>(null);

export const RowActivityProvider = RowActivityContext.Provider;

/** Whether this row is the active one, and the store to change it. Without a provider the row has its own store. */
export function useRowActivity(id: string): {
  readonly active: boolean;
  readonly store: RowActivityStore;
} {
  const shared = useContext(RowActivityContext);
  const [own] = useState(createRowActivityStore);
  const store = shared ?? own;
  const active = useSyncExternalStore(
    store.subscribe,
    () => store.active() === id,
    () => false,
  );
  return { active, store };
}

/** Whether a focus came from the keyboard (`:focus-visible`); a click's focus does not open the bar. */
export function isKeyboardFocus(element: Element): boolean {
  try {
    return element.matches(":focus-visible");
  } catch {
    return true;
  }
}
