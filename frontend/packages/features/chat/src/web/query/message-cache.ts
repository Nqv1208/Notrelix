/**
 * Cache reducers of the message history (an infinite query of pages, oldest
 * first). Pure: no React, no client. Each reducer returns the same object when
 * nothing changes, so a list does not re-render for a no-op.
 */

import type { InfiniteData } from "@tanstack/react-query";
import type {
  ChatMessage,
  ChatMessageCursor,
  ChatMessagePage,
} from "../../core/types/chat";

export type ChatMessagesCache = InfiniteData<
  ChatMessagePage,
  ChatMessageCursor
>;

function hasId(cache: ChatMessagesCache, id: string): boolean {
  return cache.pages.some((page) => page.items.some((item) => item.id === id));
}

/**
 * A message the server just stored joins the newest page, once. When the
 * loaded window is not the newest (hasNewer), the message is not added here:
 * the newer page brings it when the user reaches the end.
 */
export function appendToNewest(
  cache: ChatMessagesCache,
  message: ChatMessage,
): ChatMessagesCache {
  if (cache.pages.length === 0 || hasId(cache, message.id)) return cache;
  const last = cache.pages[cache.pages.length - 1]!;
  if (last.hasNewer) return cache;
  const newest = last.items[last.items.length - 1];
  if (newest && newest.sequence >= message.sequence) return cache;
  const pages = cache.pages.slice();
  pages[pages.length - 1] = { ...last, items: [...last.items, message] };
  return { ...cache, pages };
}

/**
 * Replaces a loaded message with the server's copy when that copy is at least
 * as new (a version never goes back).
 */
export function replaceMessage(
  cache: ChatMessagesCache,
  message: ChatMessage,
): ChatMessagesCache {
  let changed = false;
  const pages = cache.pages.map((page) => {
    const index = page.items.findIndex((item) => item.id === message.id);
    if (index < 0) return page;
    const current = page.items[index]!;
    if (message.version < current.version) return page;
    changed = true;
    const items = page.items.slice();
    items[index] = message;
    return { ...page, items };
  });
  return changed ? { ...cache, pages } : cache;
}

/** A deleted message becomes a tombstone: no content, the same author, time and sequence. */
export function tombstoneMessage(
  cache: ChatMessagesCache,
  messageId: string,
  deletedAt: string,
): ChatMessagesCache {
  let changed = false;
  const pages = cache.pages.map((page) => {
    const index = page.items.findIndex((item) => item.id === messageId);
    if (index < 0) return page;
    const current = page.items[index]!;
    if (current.deleted) return page;
    changed = true;
    const items = page.items.slice();
    items[index] = {
      ...current,
      content: null,
      deleted: true,
      deletedAt,
      version: current.version + 1,
    };
    return { ...page, items };
  });
  return changed ? { ...cache, pages } : cache;
}
