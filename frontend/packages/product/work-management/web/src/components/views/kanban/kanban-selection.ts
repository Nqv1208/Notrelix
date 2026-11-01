import { useCallback, useMemo, useRef, useState } from "react";

/**
 * What a card needs to be picked for the bulk bar: the picked ids and one
 * toggle. `range` (Shift+click) extends from the card picked last, through the
 * cards in the order the board shows them.
 */
export interface KanbanCardSelection {
  readonly selectedIds: ReadonlySet<string>;
  readonly onToggle: (cardId: string, options?: { range?: boolean }) => void;
}

/** The cards picked on a board, kept to the ones still shown. */
export function useKanbanSelection(orderedIds: readonly string[]) {
  const [picked, setPicked] = useState<ReadonlySet<string>>(new Set());
  const anchor = useRef<string | null>(null);
  const shown = useMemo(() => new Set(orderedIds), [orderedIds]);
  const selectedIds = useMemo(
    () => new Set([...picked].filter((id) => shown.has(id))),
    [picked, shown],
  );

  const onToggle = useCallback(
    (cardId: string, options?: { range?: boolean }) => {
      setPicked((current) => {
        const next = new Set(current);
        const from = anchor.current ? orderedIds.indexOf(anchor.current) : -1;
        const to = orderedIds.indexOf(cardId);
        if (options?.range && from >= 0 && to >= 0) {
          const [start, end] = from < to ? [from, to] : [to, from];
          for (const id of orderedIds.slice(start, end + 1)) next.add(id);
        } else if (next.has(cardId)) next.delete(cardId);
        else next.add(cardId);
        return next;
      });
      anchor.current = cardId;
    },
    [orderedIds],
  );
  const clear = useCallback(() => {
    setPicked(new Set());
    anchor.current = null;
  }, []);

  const selection = useMemo<KanbanCardSelection>(
    () => ({ selectedIds, onToggle }),
    [selectedIds, onToggle],
  );
  return { selectedIds, selection, clear };
}
