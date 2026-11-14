/**
 * Cache reducers of the Phase 2 interactions: reactions, threads and pins. Pure:
 * no React, no client, no network. Every reducer returns the same object when
 * nothing changes, so a list does not re-render for a no-op.
 *
 * The message shapes differ by cache: a channel history is a page of `items`
 * (messages), a thread is a page of `items` with the `root` on top, and the pin
 * list is a page of `items` that wrap a message (`{ message, pinnedBy, pinnedAt }`).
 * `patchMessageInData` reaches the message in each of them, so one reducer
 * serves every cache that holds a message.
 */

import type { ChatMessage, ChatReactionSummary } from "../../core/types/chat";

/** The thread summary of a root message as the realtime path learns it (the highest reply sequence wins). */
export interface ThreadCount {
  readonly replyCount: number;
  readonly lastReplyAt: string | null;
}

export type ThreadCounts = Readonly<Record<string, ThreadCount>>;

interface PageLike {
  readonly items?: readonly unknown[];
  readonly root?: unknown;
}

interface InfiniteLike {
  readonly pages: readonly PageLike[];
}

function isInfinite(data: unknown): data is InfiniteLike {
  return (
    typeof data === "object" &&
    data !== null &&
    Array.isArray((data as InfiniteLike).pages)
  );
}

function hasMessageId(value: unknown, id: string): value is ChatMessage {
  return (
    typeof value === "object" &&
    value !== null &&
    (value as { id?: unknown }).id === id
  );
}

/**
 * Applies `patch` to every copy of message `messageId` inside an infinite cache.
 * Returns the same object when the message is not in it.
 */
export function patchMessageInData<T>(
  data: T,
  messageId: string,
  patch: (message: ChatMessage) => ChatMessage,
): T {
  if (!isInfinite(data)) return data;
  let changed = false;
  const pages = data.pages.map((page) => {
    let pageChanged = false;
    let root = page.root;
    if (hasMessageId(root, messageId)) {
      root = patch(root);
      pageChanged = true;
    }
    let items: readonly unknown[] | undefined = page.items;
    if (items) {
      const next = items.map((item) => {
        if (hasMessageId(item, messageId)) return patch(item);
        const wrapped = (item as { message?: unknown } | null)?.message;
        if (hasMessageId(wrapped, messageId)) {
          return { ...(item as object), message: patch(wrapped) };
        }
        return item;
      });
      if (next.some((item, index) => item !== items![index])) {
        items = next;
        pageChanged = true;
      }
    }
    if (!pageChanged) return page;
    changed = true;
    return { ...page, ...(root !== page.root ? { root } : {}), items };
  });
  return changed ? ({ ...data, pages } as T) : data;
}

/** Whether a message id is in any page of an infinite cache (a root, an item or a pinned message). */
export function containsMessage(data: unknown, messageId: string): boolean {
  if (!isInfinite(data)) return false;
  return data.pages.some((page) => {
    if (hasMessageId(page.root, messageId)) return true;
    return (page.items ?? []).some((item) => {
      if (hasMessageId(item, messageId)) return true;
      const wrapped = (item as { message?: unknown } | null)?.message;
      return hasMessageId(wrapped, messageId);
    });
  });
}

/**
 * The palette emoji toggled by the caller (optimistic). Adding creates the
 * entry or raises its count; removing lowers the count and drops an empty entry.
 */
export function toggleReaction(
  reactions: readonly ChatReactionSummary[],
  emoji: string,
  add: boolean,
): readonly ChatReactionSummary[] {
  const index = reactions.findIndex((entry) => entry.emoji === emoji);
  const current = index >= 0 ? reactions[index]! : null;
  if (add) {
    if (current?.reactedByMe) return reactions;
    if (current === null) {
      return [...reactions, { emoji, count: 1, reactedByMe: true }];
    }
    return reactions.map((entry, at) =>
      at === index
        ? { ...entry, count: entry.count + 1, reactedByMe: true }
        : entry,
    );
  }
  if (current === null || !current.reactedByMe) return reactions;
  if (current.count <= 1) return reactions.filter((_, at) => at !== index);
  return reactions.map((entry, at) =>
    at === index
      ? { ...entry, count: entry.count - 1, reactedByMe: false }
      : entry,
  );
}

/**
 * A reaction change from the realtime stream. The caller's own change is
 * already applied (optimistically or by the REST answer), so it sets the state
 * instead of counting again; another person's change moves the count by one.
 */
export function applyReactionChange(
  reactions: readonly ChatReactionSummary[],
  change: { kind: string; change: "added" | "removed"; actorIsViewer: boolean },
): readonly ChatReactionSummary[] {
  const add = change.change === "added";
  if (change.actorIsViewer) {
    return toggleReaction(reactions, change.kind, add);
  }
  const index = reactions.findIndex((entry) => entry.emoji === change.kind);
  const current = index >= 0 ? reactions[index]! : null;
  if (add) {
    if (current === null) {
      return [
        ...reactions,
        { emoji: change.kind, count: 1, reactedByMe: false },
      ];
    }
    return reactions.map((entry, at) =>
      at === index ? { ...entry, count: entry.count + 1 } : entry,
    );
  }
  if (current === null) return reactions;
  if (current.count <= 1 && !current.reactedByMe) {
    return reactions.filter((_, at) => at !== index);
  }
  return reactions.map((entry, at) =>
    at === index ? { ...entry, count: Math.max(0, entry.count - 1) } : entry,
  );
}

/** Keeps the highest reply sequence and the latest reply time of each thread. */
export function bumpThreadCount(
  counts: ThreadCounts,
  rootId: string,
  replyCount: number,
  lastReplyAt: string | null,
): ThreadCounts {
  const current = counts[rootId];
  if (current && current.replyCount >= replyCount) return counts;
  return {
    ...counts,
    [rootId]: {
      replyCount,
      lastReplyAt: laterOf(current?.lastReplyAt ?? null, lastReplyAt),
    },
  };
}

function laterOf(a: string | null, b: string | null): string | null {
  if (a === null) return b;
  if (b === null) return a;
  return Date.parse(b) > Date.parse(a) ? b : a;
}

interface ThreadPageLike {
  readonly items: readonly ChatMessage[];
  readonly hasNewer: boolean;
  readonly root: ChatMessage;
  readonly thread: unknown;
}

/**
 * A reply that joins a loaded thread: the newest page only, once, in thread
 * sequence order. A thread window that is not at its end leaves the reply to the
 * reader's next page.
 */
export function appendThreadMessage<T>(data: T, reply: ChatMessage): T {
  if (!isInfinite(data) || data.pages.length === 0) return data;
  const pages = data.pages as unknown as readonly ThreadPageLike[];
  if (pages.some((page) => page.items.some((item) => item.id === reply.id))) {
    return data;
  }
  const last = pages[pages.length - 1]!;
  if (last.hasNewer) return data;
  const items = [...last.items, reply].sort(
    (a, b) => (a.threadSequence ?? 0) - (b.threadSequence ?? 0),
  );
  const nextPages = pages.slice();
  nextPages[pages.length - 1] = { ...last, items };
  return { ...data, pages: nextPages } as unknown as T;
}

/** Applies `patch` to the thread summary of every page of a thread cache. */
export function patchThreadSummary<T, S extends object>(
  data: T,
  patch: (summary: S) => S,
): T {
  if (!isInfinite(data)) return data;
  const pages = data.pages as unknown as readonly { thread?: S }[];
  let changed = false;
  const next = pages.map((page) => {
    if (!page.thread) return page;
    changed = true;
    return { ...page, thread: patch(page.thread) };
  });
  return changed ? ({ ...data, pages: next } as unknown as T) : data;
}
