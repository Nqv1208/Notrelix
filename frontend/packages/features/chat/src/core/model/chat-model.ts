/**
 * Chat view rules that need no React and no network: the composer's content
 * check, the message order and merge, the rows of the list (day separators,
 * the "New" divider, author runs), the monotonic read boundary and the
 * projection of pending sends. The server stays the authority; these only
 * shape what the cache shows.
 */

import {
  CHAT_LIMITS,
  type ChatMentionInput,
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
  /** A 429 only: the seconds the server asked to wait, and whether a rate limit or the slow mode answered. */
  readonly retryAfterSeconds?: number | null;
  readonly limit?: "rate-limited" | "slow-mode" | null;
  /** The mentions that travel with the content, kept so a retry sends the same ones. */
  readonly mentions?: readonly ChatMentionInput[];
  readonly confirmBroadcast?: boolean;
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

/** Consecutive messages of one author within this many minutes form one group (one avatar, one name). */
export const AUTHOR_RUN_MINUTES = 5;
/** A pause longer than this many minutes inside one day gets a centred time separator. */
export const TIME_GAP_MINUTES = 15;

export type ChatRow =
  | { readonly kind: "day"; readonly key: string; readonly dayKey: string }
  | { readonly kind: "unread"; readonly key: string }
  /** A centred time stamp after a long pause inside one day. */
  | { readonly kind: "time"; readonly key: string; readonly at: string }
  | {
      readonly kind: "message";
      readonly key: string;
      readonly message: ChatMessage;
      /** True when the message is in the same group as the row above (no name, no gap). */
      readonly continued: boolean;
      /** True when the next row does not continue the group (the avatar and status sit here). */
      readonly groupEnd: boolean;
    }
  | {
      readonly kind: "pending";
      readonly key: string;
      readonly clientMessageId: string;
      readonly pending: PendingMessage;
      readonly continued: boolean;
      readonly groupEnd: boolean;
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

/** The day key of the day before `dayKey` (YYYY-MM-DD, calendar arithmetic). */
export function previousDayKey(dayKey: string): string {
  const [year, month, day] = dayKey.split("-").map(Number);
  const date = new Date(Date.UTC(year!, month! - 1, day! - 1, 12));
  return date.toISOString().slice(0, 10);
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

/** What grouping reads of a stored message or a pending send. */
interface GroupMember {
  readonly authorId: string;
  readonly createdAt: string;
  readonly kind: "user" | "system";
  /** A reply (it quotes another message) or a thread reply also sent to the channel starts its own group. */
  readonly breaksGroup: boolean;
}

function memberOf(message: ChatMessage): GroupMember {
  return {
    authorId: message.authorId,
    createdAt: message.createdAt,
    kind: message.kind === "system" ? "system" : "user",
    breaksGroup:
      message.replyToId !== null || message.alsoSentToChannel === true,
  };
}

/**
 * The list as rows: a day separator before each new day, a time separator after a pause of more than
 * fifteen minutes, the unread divider before the first unread message, and the groups (a run of one author
 * within five minutes, broken by any separator, a system line or a reply), then the pending sends at the end.
 */
export function buildChatRows(input: {
  readonly messages: readonly ChatMessage[];
  readonly pending: readonly PendingMessage[];
  readonly firstUnread: number | null;
  readonly timeZone?: string;
}): ChatRow[] {
  type Draft =
    | Exclude<ChatRow, { kind: "message" } | { kind: "pending" }>
    | (Omit<Extract<ChatRow, { kind: "message" }>, "groupEnd"> & {
        groupEnd: boolean;
      })
    | (Omit<Extract<ChatRow, { kind: "pending" }>, "groupEnd"> & {
        groupEnd: boolean;
      });
  const rows: Draft[] = [];
  let previous: GroupMember | null = null;
  let previousAt: string | null = null;
  let previousDay: string | null = null;
  let dividerPlaced = false;

  function separate(at: string, key: string) {
    const day = dayKeyOf(at, input.timeZone);
    if (day !== previousDay) {
      rows.push({ kind: "day", key: `day-${day}`, dayKey: day });
      previousDay = day;
      previous = null;
    } else if (
      previousAt !== null &&
      Date.parse(at) - Date.parse(previousAt) > TIME_GAP_MINUTES * 60_000
    ) {
      rows.push({ kind: "time", key: `time-${key}`, at });
      previous = null;
    }
    previousAt = at;
  }

  for (const message of input.messages) {
    separate(message.createdAt, message.id);
    if (
      !dividerPlaced &&
      input.firstUnread !== null &&
      message.sequence === input.firstUnread
    ) {
      rows.push({ kind: "unread", key: "unread-divider" });
      dividerPlaced = true;
      previous = null;
    }
    const member = memberOf(message);
    rows.push({
      kind: "message",
      key: `m-${message.id}`,
      message,
      continued: continuesGroup(previous, member),
      groupEnd: true,
    });
    previous = member;
  }
  for (const entry of input.pending) {
    separate(entry.createdAt, entry.clientMessageId);
    const member: GroupMember = {
      authorId: entry.authorId,
      createdAt: entry.createdAt,
      kind: "user",
      breaksGroup: false,
    };
    rows.push({
      kind: "pending",
      key: `p-${entry.clientMessageId}`,
      clientMessageId: entry.clientMessageId,
      pending: entry,
      continued: continuesGroup(previous, member),
      groupEnd: true,
    });
    previous = member;
  }
  // A row ends its group unless the row right after it continues the group.
  for (let index = 0; index < rows.length - 1; index++) {
    const row = rows[index]!;
    const next = rows[index + 1]!;
    if (
      (row.kind === "message" || row.kind === "pending") &&
      (next.kind === "message" || next.kind === "pending") &&
      next.continued
    ) {
      row.groupEnd = false;
    }
  }
  return rows;
}

/** Whether `next` joins the group of `previous`: same author, both user messages, within five minutes, no break. */
function continuesGroup(
  previous: GroupMember | null,
  next: GroupMember,
): boolean {
  if (!previous) return false;
  if (previous.kind !== "user" || next.kind !== "user") return false;
  if (next.breaksGroup) return false;
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
    // A read to the newest message clears the mention counter; a partial read waits for the server's count.
    mentionCount: row.lastSequence <= readSequence ? 0 : row.mentionCount,
    firstUnreadSequence:
      row.lastSequence <= readSequence ? null : row.firstUnreadSequence,
  };
}
