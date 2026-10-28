import type { Checklist } from "../types/checklist-types";

/** An entry the server has not answered for yet carries this id prefix. */
export const PENDING_CHECKLIST_ID_PREFIX = "pending-";

export function isPendingChecklistId(id: string): boolean {
  return id.startsWith(PENDING_CHECKLIST_ID_PREFIX);
}

export interface ChecklistProgress {
  readonly done: number;
  readonly total: number;
}

/** Done over all items of one list. */
export function listProgress(list: Checklist): ChecklistProgress {
  return {
    done: list.items.filter((item) => item.isDone).length,
    total: list.items.length,
  };
}

/** Done over all items of every list; null when there is no item (the face shows nothing). */
export function checklistProgressOf(
  lists: readonly Checklist[],
): ChecklistProgress | null {
  let done = 0;
  let total = 0;
  for (const list of lists) {
    const progress = listProgress(list);
    done += progress.done;
    total += progress.total;
  }
  return total > 0 ? { done, total } : null;
}

/** `items` with the one named `id` taken out and put back so it ends at `toIndex`. */
export function moveById<T extends { readonly id: string }>(
  items: readonly T[],
  id: string,
  toIndex: number,
): T[] {
  const moved = items.find((item) => item.id === id);
  if (!moved) return [...items];
  const rest = items.filter((item) => item.id !== id);
  const at = Math.max(0, Math.min(toIndex, rest.length));
  return [...rest.slice(0, at), moved, ...rest.slice(at)];
}

/** The neighbours the server places a moved entry between (after it left its old place). */
export function placementOf(
  items: readonly { readonly id: string }[],
  id: string,
): { previousSiblingId: string | null; nextSiblingId: string | null } {
  const index = items.findIndex((item) => item.id === id);
  return {
    previousSiblingId: index > 0 ? items[index - 1]!.id : null,
    nextSiblingId:
      index >= 0 && index < items.length - 1 ? items[index + 1]!.id : null,
  };
}
