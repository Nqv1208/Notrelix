import type { ChatRow } from "../../core/model/chat-model";

// What the list assumes a row measures before it has been drawn once. The numbers are measured on the chat column:
// text-sm at a 1.45 leading is 20.3 px a line inside a 16 px padded bubble, about 6.9 px a character.
const LINE_PX = 20.3;
const CHAR_PX = 6.9;
const BUBBLE_PAD_PX = 16;
const BUBBLE_SIDE_PAD_PX = 24;
const GAP_CONTINUED_PX = 2;
const GAP_NEW_GROUP_PX = 12;
const NAME_LINE_PX = 40;
const META_LINE_PX = 19.4;
const DAY_PX = 36;
const TIME_PX = 32;
const UNREAD_PX = 40;
const TYPING_PX = 40;
const IMAGE_BOX_PX = 240;
const FILE_ROW_PX = 64;
const QUOTE_PX = 52;
const REACTIONS_PX = 30;
const THREAD_PX = 28;
const NOTICE_PX = 40;
/** The widest a bubble grows (36 rem) and the share of the column it may take. */
const BUBBLE_MAX_PX = 576;
const BUBBLE_COLUMN_SHARE = 0.75;
/** Used while the list has no width yet. */
export const FALLBACK_COLUMN_PX = 740;

type EstimateEntry =
  ChatRow | { readonly kind: "typing"; readonly key: string };

/** Lines a text takes in a bubble of `textWidth`: each hard line wraps by its character count. */
export function estimateLines(text: string, textWidthPx: number): number {
  const perLine = Math.max(8, Math.floor(textWidthPx / CHAR_PX));
  let lines = 0;
  for (const part of text.split("\n"))
    lines += Math.max(1, Math.ceil(part.length / perLine));
  return lines;
}

/** The height of a row before it is measured; `columnPx` is the width of the list. */
export function estimateRowHeight(
  entry: EstimateEntry,
  columnPx: number,
): number {
  switch (entry.kind) {
    case "day":
      return DAY_PX;
    case "time":
      return TIME_PX;
    case "unread":
      return UNREAD_PX;
    case "typing":
      return TYPING_PX;
    default: {
      const bubble = Math.min(
        BUBBLE_MAX_PX,
        BUBBLE_COLUMN_SHARE * Math.max(columnPx - 48, 0),
      );
      const textWidth = Math.max(bubble - BUBBLE_SIDE_PAD_PX, 0);
      const message = entry.kind === "message" ? entry.message : null;
      const text =
        entry.kind === "pending"
          ? entry.pending.content
          : message?.deleted
            ? "x".repeat(24)
            : (message?.content ?? "");
      let height =
        (entry.continued ? GAP_CONTINUED_PX : GAP_NEW_GROUP_PX + NAME_LINE_PX) +
        BUBBLE_PAD_PX +
        estimateLines(text, textWidth) * LINE_PX;
      if (message) {
        for (const file of message.attachments ?? [])
          height += file.contentType.startsWith("image/")
            ? IMAGE_BOX_PX
            : FILE_ROW_PX;
        if (message.replyPreview) height += QUOTE_PX;
        if (message.reactions && message.reactions.length > 0)
          height += REACTIONS_PX;
        if ((message.threadReplyCount ?? 0) > 0) height += THREAD_PX;
        if ((message.mentionNotices ?? []).length > 0) height += NOTICE_PX;
      }
      return Math.round(height + (entry.groupEnd ? META_LINE_PX : 0));
    }
  }
}
