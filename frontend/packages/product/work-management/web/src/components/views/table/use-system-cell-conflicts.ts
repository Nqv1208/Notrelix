import { useCallback, useRef, useState } from "react";
import type {
  SystemCellColumn,
  SystemCellConflict,
} from "./table-cell-editors";

const keyOf = (rowId: string, column: SystemCellColumn) =>
  `${rowId}\u0000${column}`;

interface OpenConflict extends SystemCellConflict {
  /** Sends the written value again; it reopens the choice when it loses once more. */
  readonly resend: () => Promise<unknown>;
}

/**
 * The open version conflicts of the table's built-in title and due date cells (UC-WM-B33). A write that lost to
 * someone else's save keeps the value this person wrote and who saved first; the row already shows theirs.
 * "Giữ giá trị của tôi" reads the board again first, so the value is written on the newest version (never a blind
 * overwrite); "Dùng giá trị mới" drops the written value and sends nothing.
 */
export function useSystemCellConflicts(reload: () => Promise<unknown>) {
  const [conflicts, setConflicts] = useState<ReadonlyMap<string, OpenConflict>>(
    () => new Map(),
  );
  const latest = useRef(conflicts);
  latest.current = conflicts;

  const put = useCallback((key: string, conflict: OpenConflict | null) => {
    setConflicts((current) => {
      const next = new Map(current);
      if (conflict) next.set(key, conflict);
      else next.delete(key);
      return next;
    });
  }, []);

  const open = useCallback(
    (rowId: string, column: SystemCellColumn, conflict: OpenConflict) =>
      put(keyOf(rowId, column), conflict),
    [put],
  );
  const conflictOf = useCallback(
    (rowId: string, column: SystemCellColumn): SystemCellConflict | undefined =>
      conflicts.get(keyOf(rowId, column)),
    [conflicts],
  );
  const keepMine = useCallback(
    (rowId: string, column: SystemCellColumn) => {
      const key = keyOf(rowId, column);
      const mine = latest.current.get(key);
      if (!mine) return;
      put(key, null);
      void reload().finally(() => void mine.resend());
    },
    [put, reload],
  );
  const takeTheirs = useCallback(
    (rowId: string, column: SystemCellColumn) =>
      put(keyOf(rowId, column), null),
    [put],
  );

  return { open, conflictOf, keepMine, takeTheirs };
}
