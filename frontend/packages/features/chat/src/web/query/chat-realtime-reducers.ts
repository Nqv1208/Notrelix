/**
 * Pure reducers that apply Chat realtime frames to the cached history and the
 * unread summary (UC-CHT-C02, C03, C04, C09). No React, no client, no network.
 *
 * The cache is the loaded window of the history (pages, oldest first). A frame
 * changes it only when the window reaches the newest message (the last page has
 * no `hasNewer`); otherwise the frame is outside the window and the reader's
 * paging brings the message later. The server stays the authority: a frame that
 * cannot be applied safely reports a `gap` or `omitted` outcome, and the caller
 * fetches the missing range over REST and merges it with `mergeHistoryPage`.
 */

import type {
  ChatMessage,
  ChatMessagePage,
  ChatUnreadSummary,
} from "../../core/types/chat";
import type {
  ChatMessageFramePayload,
  ChatUnreadFramePayload,
} from "../../core/realtime/chat-frames";
import type { ChatMessagesCache } from "./message-cache";

export type MessageFrameOutcome =
  | { readonly kind: "applied"; readonly cache: ChatMessagesCache }
  | {
      readonly kind: "unchanged";
      readonly cache: ChatMessagesCache;
      readonly reason: "duplicate" | "stale" | "outside-window";
    }
  /** The frame is past the newest loaded message by more than one: the range in between is missing. */
  | {
      readonly kind: "gap";
      readonly cache: ChatMessagesCache;
      readonly after: number;
    }
  /** The text did not fit the frame: fetch the message's range (after the sequence before it). */
  | {
      readonly kind: "omitted";
      readonly cache: ChatMessagesCache;
      readonly after: number;
    };

export type UnreadFrameOutcome =
  | { readonly kind: "applied"; readonly summary: ChatUnreadSummary }
  | { readonly kind: "unchanged"; readonly summary: ChatUnreadSummary }
  /** A space the summary does not list (a new channel): refetch the summary. */
  | { readonly kind: "unknown-space"; readonly summary: ChatUnreadSummary };

function allMessages(cache: ChatMessagesCache): ChatMessage[] {
  return cache.pages.flatMap((page) => page.items as ChatMessage[]);
}

function newestSequence(cache: ChatMessagesCache): number {
  let newest = 0;
  for (const message of allMessages(cache)) {
    if (message.sequence > newest) newest = message.sequence;
  }
  return newest;
}

function locate(cache: ChatMessagesCache, id: string): ChatMessage | undefined {
  for (const page of cache.pages) {
    const found = page.items.find((item) => item.id === id);
    if (found) return found;
  }
  return undefined;
}

/** Inserts a message into an ascending list by sequence; an equal sequence keeps the existing one first. */
function insertSorted(
  items: readonly ChatMessage[],
  message: ChatMessage,
): ChatMessage[] {
  let index = items.length;
  while (index > 0 && items[index - 1]!.sequence > message.sequence) index--;
  return [...items.slice(0, index), message, ...items.slice(index)];
}

/** Replaces the last page (the newest window) and keeps the other pages as they are. */
function withLastPage(
  cache: ChatMessagesCache,
  page: ChatMessagePage,
): ChatMessagesCache {
  const pages = cache.pages.slice();
  pages[pages.length - 1] = page;
  return { ...cache, pages };
}

/** The message a created frame stands for. A created frame is always the first version. */
export function messageFromCreatedFrame(
  frame: ChatMessageFramePayload,
  spaceId: string,
  occurredAt: string,
): ChatMessage {
  return {
    id: frame.id,
    spaceId,
    sequence: frame.sequence,
    kind: frame.kind,
    authorId: frame.authorId,
    content: frame.content,
    clientMessageId: frame.clientMessageId,
    replyToId: frame.replyToId,
    edited: frame.editedAt !== null,
    deleted: frame.deleted,
    createdAt: frame.createdAt ?? occurredAt,
    editedAt: frame.editedAt,
    deletedAt: frame.deleted ? occurredAt : null,
    version: frame.version,
  };
}

/** The highest sequence of the loaded history (0 when nothing is loaded): the last known point. */
export function lastKnownSequence(cache: ChatMessagesCache): number {
  return newestSequence(cache);
}

/** Whether the loaded window reaches the newest message, so frames may join it. */
export function windowIsLive(cache: ChatMessagesCache): boolean {
  const last = cache.pages[cache.pages.length - 1];
  return last !== undefined && !last.hasNewer;
}

export function applyMessageCreated(
  cache: ChatMessagesCache,
  frame: ChatMessageFramePayload,
  spaceId: string,
  occurredAt: string,
): MessageFrameOutcome {
  if (cache.pages.length === 0) {
    return { kind: "unchanged", cache, reason: "outside-window" };
  }
  // The optimistic send of this client, once stored, is the same message.
  if (
    frame.clientMessageId !== null &&
    allMessages(cache).some(
      (item) => item.clientMessageId === frame.clientMessageId,
    )
  ) {
    return { kind: "unchanged", cache, reason: "duplicate" };
  }
  const existing = locate(cache, frame.id);
  const message = messageFromCreatedFrame(frame, spaceId, occurredAt);
  if (existing) {
    if (existing.version >= frame.version) {
      return {
        kind: "unchanged",
        cache,
        reason: existing.version === frame.version ? "duplicate" : "stale",
      };
    }
    return { kind: "applied", cache: replaceInCache(cache, message) };
  }
  if (!windowIsLive(cache)) {
    return { kind: "unchanged", cache, reason: "outside-window" };
  }
  const newest = newestSequence(cache);
  if (frame.sequence > newest + 1) {
    return { kind: "gap", cache, after: newest };
  }
  const last = cache.pages[cache.pages.length - 1]!;
  return {
    kind: "applied",
    cache: withLastPage(cache, {
      ...last,
      items: insertSorted(last.items, message),
    }),
  };
}

function replaceInCache(
  cache: ChatMessagesCache,
  message: ChatMessage,
): ChatMessagesCache {
  return {
    ...cache,
    pages: cache.pages.map((page) =>
      page.items.some((item) => item.id === message.id)
        ? {
            ...page,
            items: page.items.map((item) =>
              item.id === message.id ? message : item,
            ),
          }
        : page,
    ),
  };
}

export function applyMessageUpdated(
  cache: ChatMessagesCache,
  frame: ChatMessageFramePayload,
): MessageFrameOutcome {
  const current = locate(cache, frame.id);
  // An edit of a message outside the loaded window is ignored; the window refetches it when it is reached.
  if (!current) return { kind: "unchanged", cache, reason: "outside-window" };
  if (frame.version < current.version) {
    return { kind: "unchanged", cache, reason: "stale" };
  }
  if (frame.version === current.version) {
    return { kind: "unchanged", cache, reason: "duplicate" };
  }
  if (frame.contentOmitted) {
    return { kind: "omitted", cache, after: frame.sequence - 1 };
  }
  return {
    kind: "applied",
    cache: replaceInCache(cache, {
      ...current,
      content: frame.content,
      edited: frame.editedAt !== null || current.edited,
      editedAt: frame.editedAt ?? current.editedAt,
      version: frame.version,
    }),
  };
}

export function applyMessageDeleted(
  cache: ChatMessagesCache,
  frame: ChatMessageFramePayload,
  occurredAt: string,
): MessageFrameOutcome {
  const current = locate(cache, frame.id);
  if (!current) return { kind: "unchanged", cache, reason: "outside-window" };
  if (frame.version < current.version) {
    return { kind: "unchanged", cache, reason: "stale" };
  }
  if (current.deleted && frame.version === current.version) {
    return { kind: "unchanged", cache, reason: "duplicate" };
  }
  // A tombstone keeps the id, the author, the time and the sequence; the text goes.
  return {
    kind: "applied",
    cache: replaceInCache(cache, {
      ...current,
      content: null,
      deleted: true,
      deletedAt: occurredAt,
      version: frame.version,
    }),
  };
}

/**
 * Merges a page fetched after the last known sequence (the gap or reconnect
 * backfill) into the live window. Items already loaded are replaced only by a
 * newer version; the rest join in order. A window that is not live is left alone.
 */
export function mergeHistoryPage(
  cache: ChatMessagesCache,
  page: ChatMessagePage,
): ChatMessagesCache {
  if (cache.pages.length === 0 || !windowIsLive(cache)) return cache;
  const last = cache.pages[cache.pages.length - 1]!;
  let items = last.items as ChatMessage[];
  for (const incoming of page.items) {
    const index = items.findIndex((item) => item.id === incoming.id);
    if (index >= 0) {
      if (incoming.version > items[index]!.version) {
        items = items.map((item, at) => (at === index ? incoming : item));
      }
      continue;
    }
    items = insertSorted(items, incoming);
  }
  return withLastPage(cache, {
    ...last,
    items,
    hasNewer: page.hasNewer,
  });
}

export function applyUnreadFrame(
  summary: ChatUnreadSummary,
  payload: ChatUnreadFramePayload,
): UnreadFrameOutcome {
  const row = summary.spaces.find((entry) => entry.spaceId === payload.spaceId);
  if (!row) return { kind: "unknown-space", summary };
  if (
    row.unreadCount === payload.unread &&
    row.mentionCount === payload.mentions
  ) {
    return { kind: "unchanged", summary };
  }
  const spaces = summary.spaces.map((entry) =>
    entry === row
      ? {
          ...entry,
          unreadCount: payload.unread,
          mentionCount: payload.mentions,
          firstUnreadSequence:
            payload.unread === 0 ? null : entry.firstUnreadSequence,
        }
      : entry,
  );
  // The totals count the unmuted spaces only (the backend's UnreadSummaryBuilder rule).
  const unmuted = spaces.filter((entry) => !entry.muted);
  return {
    kind: "applied",
    summary: {
      spaces,
      totalUnread: unmuted.reduce((sum, entry) => sum + entry.unreadCount, 0),
      totalMentions: unmuted.reduce(
        (sum, entry) => sum + entry.mentionCount,
        0,
      ),
    },
  };
}
