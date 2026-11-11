/**
 * Chat view rules that need no React and no network: the composer's content
 * check, the message order and merge, the rows of the list (day separators,
 * the "New" divider, author runs), the monotonic read boundary and the
 * projection of pending sends. The server stays the authority; these only
 * shape what the cache shows.
 */

import {
  CHAT_LIMITS,
  type ChatMessage,
  type ChatMessagePage,
  type ChatUnreadRow,
} from "../types/chat";

// ─── Composer ────────────────────────────────────────────────────────────────

/** Characters as the user sees them (code points), not UTF-16 units. */
export function chatTextLength(text: string): number {
  return Array.from(text).length;
}

export type ContentCheck =
  | { readonly ok: true; readonly content: string }
  | { readonly ok: false; readonly reason: "empty" | "too-long" };

/** The trimmed content the server accepts: 1 to 4,000 characters after trim. */
export function checkMessageContent(raw: string): ContentCheck {
  const content = raw.trim();
  if (content.length === 0) return { ok: false, reason: "empty" };
  if (chatTextLength(content) > CHAT_LIMITS.messageMaxLength) {
    return { ok: false, reason: "too-long" };
  }
  return { ok: true, content };
}

/** Whether the composer should show its counter (from 3,800 characters). */
export function showsCounter(raw: string): boolean {
  return chatTextLength(raw) >= CHAT_LIMITS.counterFrom;
}

// ─── Messages ────────────────────────────────────────────────────────────────

/**
 * Every message of the loaded pages, ascending by sequence, once per id. When
 * an id appears twice (an overlap of two windows), the later version wins.
 */
export function mergeMessages(
  pages: readonly ChatMessagePage[],
): ChatMessage[] {
  const byId = new Map<string, ChatMessage>();
  for (const page of pages) {
    for (const message of page.items) {
      const seen = byId.get(message.id);
      if (!seen || message.version >= seen.version)
        byId.set(message.id, message);
    }
  }
  return [...byId.values()].sort((a, b) => a.sequence - b.sequence);
}

/** A send the user made that the server has not confirmed yet (or refused). */
export interface PendingMessage {
  readonly clientMessageId: string;
  readonly spaceId: string;
  readonly content: string;
  readonly authorId: string;
  readonly createdAt: string;
  readonly status: "sending" | "failed";
  /** The failure of a failed send: a retry may succeed, or the send is refused. */
  readonly failure: "retryable" | "refused" | null;
}

/** The pending sends that the server has not already stored, oldest first. */
export function unconfirmedPending(
  pending: readonly PendingMessage[],
  messages: readonly ChatMessage[],
): PendingMessage[] {
  const stored = new Set(
    messages.map((message) => message.clientMessageId).filter(Boolean),
  );
  return pending
    .filter((entry) => !stored.has(entry.clientMessageId))
    .sort((a, b) => a.createdAt.localeCompare(b.createdAt));
}

// ─── Rows of the list ────────────────────────────────────────────────────────

/** Consecutive messages of one author within this many minutes share a header. */
export const AUTHOR_RUN_MINUTES = 5;

export type ChatRow =
  | { readonly kind: "day"; readonly key: string; readonly dayKey: string }
  | { readonly kind: "unread"; readonly key: string }
  | {
      readonly kind: "message";
      readonly key: string;
      readonly message: ChatMessage;
      /** True when the message is in the same author run as the one above (no header). */
      readonly continued: boolean;
    }
  | {
      readonly kind: "pending";
      readonly key: string;
      readonly clientMessageId: string;
      readonly pending: PendingMessage;
    };

/** The calendar day of an instant in the viewer's zone, as YYYY-MM-DD. */
export function dayKeyOf(iso: string, timeZone?: string): string {
  const date = new Date(iso);
  const parts = new Intl.DateTimeFormat("en-CA", {
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
    timeZone,
  }).formatToParts(date);
  const pick = (type: string) =>
    parts.find((p) => p.type === type)?.value ?? "";
  return `${pick("year")}-${pick("month")}-${pick("day")}`;
}

/**
 * The sequence of the first message the viewer has not read: the first one
 * after the read boundary that someone else wrote. Null when nothing is unread.
 */
export function firstUnreadSequence(
  messages: readonly ChatMessage[],
  readSequence: number,
  viewerId: string,
): number | null {
  const first = messages.find(
    (message) =>
      message.sequence > readSequence &&
      message.kind === "user" &&
      message.authorId !== viewerId &&
      !message.deleted,
  );
  return first ? first.sequence : null;
}

/**
 * The list as rows: a day separator before each new day, the unread divider
 * before the first unread message, the author headers (a run of one author
 * within five minutes continues without one), and the pending sends at the end.
 */
export function buildChatRows(input: {
  readonly messages: readonly ChatMessage[];
  readonly pending: readonly PendingMessage[];
  readonly firstUnread: number | null;
  readonly timeZone?: string;
}): ChatRow[] {
  const rows: ChatRow[] = [];
  let previous: ChatMessage | null = null;
  let previousDay: string | null = null;
  let dividerPlaced = false;
  for (const message of input.messages) {
    const day = dayKeyOf(message.createdAt, input.timeZone);
    if (day !== previousDay) {
      rows.push({ kind: "day", key: `day-${day}`, dayKey: day });
      previousDay = day;
    }
    if (
      !dividerPlaced &&
      input.firstUnread !== null &&
      message.sequence === input.firstUnread
    ) {
      rows.push({ kind: "unread", key: "unread-divider" });
      dividerPlaced = true;
    }
    rows.push({
      kind: "message",
      key: `m-${message.id}`,
      message,
      continued: continuesRun(previous, message),
    });
    previous = message;
  }
  for (const entry of input.pending) {
    rows.push({
      kind: "pending",
      key: `p-${entry.clientMessageId}`,
      clientMessageId: entry.clientMessageId,
      pending: entry,
    });
  }
  return rows;
}

function continuesRun(
  previous: ChatMessage | null,
  next: ChatMessage,
): boolean {
  if (!previous) return false;
  if (previous.kind !== "user" || next.kind !== "user") return false;
  if (previous.authorId !== next.authorId) return false;
  const gap = Date.parse(next.createdAt) - Date.parse(previous.createdAt);
  return gap >= 0 && gap <= AUTHOR_RUN_MINUTES * 60_000;
}

// ─── Read state ──────────────────────────────────────────────────────────────

/** The read boundary only moves forward (CHT-009). */
export function nextReadBoundary(current: number, candidate: number): number {
  return Math.max(current, candidate);
}

/**
 * Whether a mark-read call would move the boundary: the candidate is past the
 * greatest boundary this client already knows (the cache's and the one sent).
 */
export function shouldReportRead(
  candidate: number,
  known: { readonly cached: number; readonly sent: number },
): boolean {
  return candidate > known.cached && candidate > known.sent;
}

/** The unread row after a read to `sequence`: the count drops to what is still ahead. */
export function unreadAfterRead(
  row: ChatUnreadRow,
  sequence: number,
): ChatUnreadRow {
  const readSequence = nextReadBoundary(row.readSequence, sequence);
  if (readSequence === row.readSequence) return row;
  return {
    ...row,
    readSequence,
    unreadCount: row.lastSequence <= readSequence ? 0 : row.unreadCount,
    firstUnreadSequence:
      row.lastSequence <= readSequence ? null : row.firstUnreadSequence,
  };
}
