import type { ID } from "./ids";

/**
 * Where a comment points: a quoted range of one block of the page. Offsets
 * count UTF-16 code units of the block's plain text (the runs' text joined,
 * hard line breaks as "\n"). Collaboration owns the anchor; the page's content
 * stays Documents', so the anchor is a locator that can go stale.
 */
export interface CommentAnchor {
  blockId: ID;
  quote: string;
  start: number;
  end: number;
}

export interface PageComment {
  id: ID;
  pageId: ID;
  blockId: ID | null;
  authorId: ID;
  body: string;
  mentionIds: ID[];
  /** The text the comment was made on, when it was made on a selection. */
  anchor: CommentAnchor | null;
  resolved: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface CreateCommentPayload {
  pageId: ID;
  blockId?: ID | null;
  body: string;
  mentionIds?: ID[];
  /** Comment on a selection of one block; only for a top-level comment. */
  anchor?: CommentAnchor;
}

/** The longest quote the producer accepts. */
export const COMMENT_ANCHOR_MAX_QUOTE = 500;

/**
 * Where an anchor points in the block's text now. The stored range wins when
 * it still holds the quote; otherwise the occurrence of the quote nearest to
 * the stored start (the text moved); otherwise `null`: the anchored text is
 * gone and the comment is detached. It is never attached to different text.
 */
export function resolveCommentAnchor(
  blockText: string,
  anchor: Pick<CommentAnchor, "quote" | "start" | "end">,
): { start: number; end: number } | null {
  const { quote, start, end } = anchor;
  if (!quote) return null;
  if (blockText.slice(start, end) === quote) return { start, end };
  let best: number | null = null;
  let at = blockText.indexOf(quote);
  while (at !== -1) {
    if (best === null || Math.abs(at - start) < Math.abs(best - start)) {
      best = at;
    }
    at = blockText.indexOf(quote, at + 1);
  }
  return best === null ? null : { start: best, end: best + quote.length };
}
