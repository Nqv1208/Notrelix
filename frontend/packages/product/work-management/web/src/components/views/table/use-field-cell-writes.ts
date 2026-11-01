import { useCallback, useRef, useState } from "react";
import type { CellWrite } from "./table-field-cell";

type Send = (
  rowId: string,
  fieldId: string,
  value: unknown,
) => Promise<unknown>;

const keyOf = (rowId: string, fieldId: string) => `${rowId}\u0000${fieldId}`;

/** The `kind` an API error carries (the app error of the client). */
function isConflict(error: unknown): boolean {
  return (
    typeof error === "object" &&
    error !== null &&
    "kind" in error &&
    error.kind === "conflict"
  );
}

/**
 * The write state of the custom field cells: a write is "pending" until the
 * server answers, then gone; a failure keeps the value that was typed with
 * the reason, so "Thử lại" sends it again and nothing is lost or reverted
 * silently. A version conflict reads the board again first so "Giữ giá trị
 * của tôi" is based on the newest version, never a blind overwrite.
 */
export function useFieldCellWrites(send: Send, reload: () => Promise<unknown>) {
  const [writes, setWrites] = useState<ReadonlyMap<string, CellWrite>>(
    () => new Map(),
  );
  // Writes of one cell run one after another, newest value last.
  const latest = useRef(new Map<string, number>());

  const put = useCallback((key: string, write: CellWrite | null) => {
    setWrites((current) => {
      const next = new Map(current);
      if (write) next.set(key, write);
      else next.delete(key);
      return next;
    });
  }, []);

  const write = useCallback(
    (rowId: string, fieldId: string, value: unknown, stale = false) => {
      const key = keyOf(rowId, fieldId);
      const attempt = (latest.current.get(key) ?? 0) + 1;
      latest.current.set(key, attempt);
      // The cell changed on the server while this one was open: the choice shows and nothing is sent.
      if (stale) return put(key, { status: "conflict", value });
      put(key, { status: "pending", value });
      send(rowId, fieldId, value).then(
        () => {
          if (latest.current.get(key) === attempt) put(key, null);
        },
        (error: unknown) => {
          if (latest.current.get(key) !== attempt) return;
          put(key, {
            status: isConflict(error) ? "conflict" : "error",
            value,
          });
        },
      );
    },
    [put, send],
  );

  const writeOf = useCallback(
    (rowId: string, fieldId: string) => writes.get(keyOf(rowId, fieldId)),
    [writes],
  );
  const retry = useCallback(
    (rowId: string, fieldId: string) => {
      const failed = writes.get(keyOf(rowId, fieldId));
      if (failed) write(rowId, fieldId, failed.value);
    },
    [write, writes],
  );
  const keepMine = useCallback(
    (rowId: string, fieldId: string) => {
      const mine = writes.get(keyOf(rowId, fieldId));
      if (!mine) return;
      void reload().finally(() => write(rowId, fieldId, mine.value));
    },
    [reload, write, writes],
  );
  const takeTheirs = useCallback(
    (rowId: string, fieldId: string) => put(keyOf(rowId, fieldId), null),
    [put],
  );

  return { writeOf, write, retry, keepMine, takeTheirs };
}
