import type { Block } from "../types/block";

export interface OrderedBlock {
  readonly block: Block;
  /** 0 for a top-level block, one more per level of nesting. */
  readonly depth: number;
}

/**
 * The page's blocks in reading order: top-level blocks by position, each
 * followed by its children (nested blocks without a known parent read as
 * top-level).
 */
export function orderedBlocks(blocks: readonly Block[]): OrderedBlock[] {
  const byParent = new Map<string | null, Block[]>();
  const ids = new Set(blocks.map((block) => block.id));
  for (const block of blocks) {
    const parent =
      block.parentId && ids.has(block.parentId) ? block.parentId : null;
    const siblings = byParent.get(parent) ?? [];
    siblings.push(block);
    byParent.set(parent, siblings);
  }
  const out: OrderedBlock[] = [];
  const visit = (parent: string | null, depth: number) => {
    const siblings = [...(byParent.get(parent) ?? [])].sort(
      (a, b) => a.position - b.position,
    );
    for (const block of siblings) {
      out.push({ block, depth });
      visit(block.id, depth + 1);
    }
  };
  visit(null, 0);
  return out;
}
