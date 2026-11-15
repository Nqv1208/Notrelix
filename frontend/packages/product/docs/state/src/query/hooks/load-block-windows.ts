import type { Block } from "@notrelix/docs-core";
import type { BlockWindow } from "../../api/block.api";

export const WINDOW_ATTEMPTS = 3;

/**
 * Reads the windows of blocks after `from` and hands each to `append` as it
 * arrives. A window is tried `WINDOW_ATTEMPTS` times; when it still fails the
 * cursor it stopped at comes back, so a retry can carry on from there. `null`
 * means every window was read.
 */
export async function loadBlockWindows(
  read: (cursor: string) => Promise<BlockWindow>,
  from: string,
  append: (blocks: Block[]) => void,
): Promise<string | null> {
  let cursor: string | null = from;
  while (cursor) {
    let window: BlockWindow | null = null;
    for (let attempt = 0; attempt < WINDOW_ATTEMPTS && !window; attempt += 1) {
      try {
        window = await read(cursor);
      } catch {
        // Tried again; after the last attempt the caller offers a retry.
      }
    }
    if (!window) return cursor;
    append(window.blocks);
    cursor = window.nextCursor;
  }
  return null;
}
