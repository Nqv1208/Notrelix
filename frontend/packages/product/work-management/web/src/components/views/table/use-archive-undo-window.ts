import {
  useCallback,
  useEffect,
  useLayoutEffect,
  useRef,
  useState,
} from "react";

/** How long an archive can be taken back before its write goes out. */
export const ARCHIVE_UNDO_MS = 6000;

/**
 * Archive waits for the undo window, as the Table does it: the items leave the
 * view at once and the write goes out when the window ends, on the next
 * archive, on close, when the page is hidden or when the view goes away.
 * `send` writes the ids and calls `done` once they may show again (after the
 * answer for a request that can fail and put them back).
 */
export function useArchiveUndoWindow(
  send: (ids: string[], done: () => void) => void,
) {
  const [pendingIds, setPendingIds] = useState<readonly string[]>([]);
  const pendingRef = useRef<{ ids: string[]; timer: number } | null>(null);
  const sendRef = useRef(send);
  useLayoutEffect(() => {
    sendRef.current = send;
  });

  const flush = useCallback(() => {
    const pending = pendingRef.current;
    if (!pending) return;
    window.clearTimeout(pending.timer);
    pendingRef.current = null;
    sendRef.current(pending.ids, () =>
      setPendingIds((current) => (current === pending.ids ? [] : current)),
    );
  }, []);

  useEffect(() => {
    window.addEventListener("pagehide", flush);
    return () => {
      window.removeEventListener("pagehide", flush);
      flush();
    };
  }, [flush]);

  const start = useCallback(
    (ids: string[]) => {
      flush();
      if (ids.length === 0) return;
      const timer = window.setTimeout(flush, ARCHIVE_UNDO_MS);
      pendingRef.current = { ids, timer };
      setPendingIds(ids);
    },
    [flush],
  );

  const undo = useCallback(() => {
    const pending = pendingRef.current;
    if (!pending) return;
    window.clearTimeout(pending.timer);
    pendingRef.current = null;
    setPendingIds([]);
  }, []);

  return { pendingIds, start, undo, flush };
}
