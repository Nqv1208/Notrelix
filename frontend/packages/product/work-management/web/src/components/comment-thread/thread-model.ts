import type { CardUpdate } from "@notrelix/work-management-core";

/** How many replies a thread lists before "Show N more replies". */
export const VISIBLE_REPLIES = 3;

/** Replies whose comment is gone (deleted), kept together under its id, oldest first. */
export interface OrphanThread {
  readonly parentId: string;
  readonly replies: readonly CardUpdate[];
}

/**
 * Top-level comments and each one's replies, oldest first (replies are one
 * level deep). Replies whose comment is gone are listed in `orphans`, grouped
 * by the missing comment, so a surface can draw its placeholder (COL-006).
 */
export function splitThreads(updates: readonly CardUpdate[]): {
  top: CardUpdate[];
  repliesOf: (id: string) => CardUpdate[];
  orphans: OrphanThread[];
} {
  const known = new Set(updates.map((update) => update.id));
  const byParent = new Map<string, CardUpdate[]>();
  const top: CardUpdate[] = [];
  const missing: string[] = [];
  for (const update of updates) {
    if (update.parentId && known.has(update.parentId)) {
      byParent.set(update.parentId, [
        ...(byParent.get(update.parentId) ?? []),
        update,
      ]);
    } else if (update.parentId) {
      if (!byParent.has(update.parentId)) missing.push(update.parentId);
      byParent.set(update.parentId, [
        ...(byParent.get(update.parentId) ?? []),
        update,
      ]);
    } else top.push(update);
  }
  const oldestFirst = (a: CardUpdate, b: CardUpdate) =>
    a.createdAt.localeCompare(b.createdAt);
  const repliesOf = (id: string) =>
    [...(byParent.get(id) ?? [])].sort(oldestFirst);
  return {
    top,
    repliesOf,
    orphans: missing.map((parentId) => ({
      parentId,
      replies: repliesOf(parentId),
    })),
  };
}
