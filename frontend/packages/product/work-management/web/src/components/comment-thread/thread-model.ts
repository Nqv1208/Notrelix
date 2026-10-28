import type { CardUpdate } from "@notrelix/work-management-core";

/** How many replies a thread lists before "Show N more replies". */
export const VISIBLE_REPLIES = 3;

/** Top-level comments and each one's replies, oldest first (replies are one level deep). */
export function splitThreads(updates: readonly CardUpdate[]): {
  top: CardUpdate[];
  repliesOf: (id: string) => CardUpdate[];
} {
  const known = new Set(updates.map((update) => update.id));
  const byParent = new Map<string, CardUpdate[]>();
  const top: CardUpdate[] = [];
  for (const update of updates) {
    // A reply whose parent is gone is listed on its own rather than lost.
    if (update.parentId && known.has(update.parentId)) {
      byParent.set(update.parentId, [
        ...(byParent.get(update.parentId) ?? []),
        update,
      ]);
    } else top.push(update);
  }
  const oldestFirst = (a: CardUpdate, b: CardUpdate) =>
    a.createdAt.localeCompare(b.createdAt);
  return {
    top,
    repliesOf: (id) => [...(byParent.get(id) ?? [])].sort(oldestFirst),
  };
}
