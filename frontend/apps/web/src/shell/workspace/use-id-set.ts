import { useCallback, useState } from "react";

/** A set of ids with a write in flight (one entry per member or invitation). */
export function useIdSet() {
  const [ids, setIds] = useState<ReadonlySet<string>>(() => new Set());
  const add = useCallback(
    (id: string) => setIds((current) => new Set(current).add(id)),
    [],
  );
  const remove = useCallback(
    (id: string) =>
      setIds((current) => {
        const next = new Set(current);
        next.delete(id);
        return next;
      }),
    [],
  );
  return { ids, add, remove };
}
