/**
 * View rules of message search (UC-CHT-H07, framework neutral). The server decides what a caller may find; this file only shapes the
 * typed text, the filters and the snippet for display. A snippet is plain text plus ranges: it is cut into segments here and drawn as
 * text nodes, never as markup.
 */

import {
  CHAT_SEARCH_LIMITS,
  type ChatSearchFilters,
  type ChatSearchHit,
  type ChatSearchSnippet,
} from "../types/chat";

export interface SnippetSegment {
  readonly text: string;
  readonly match: boolean;
}

/**
 * The snippet as alternating plain and matched segments. A range that is outside the text, empty or overlapping an earlier one is
 * dropped, so a malformed answer can never break the display.
 */
export function segmentSnippet(
  snippet: ChatSearchSnippet,
): readonly SnippetSegment[] {
  const { text } = snippet;
  const ranges = [...snippet.ranges]
    .filter(
      (range) =>
        Number.isInteger(range.start) &&
        Number.isInteger(range.length) &&
        range.start >= 0 &&
        range.length > 0 &&
        range.start + range.length <= text.length,
    )
    .sort((a, b) => a.start - b.start);

  const segments: SnippetSegment[] = [];
  let at = 0;
  for (const range of ranges) {
    if (range.start < at) continue;
    if (range.start > at) {
      segments.push({ text: text.slice(at, range.start), match: false });
    }
    segments.push({
      text: text.slice(range.start, range.start + range.length),
      match: true,
    });
    at = range.start + range.length;
  }
  if (at < text.length) segments.push({ text: text.slice(at), match: false });
  return segments;
}

export type SearchTextState =
  | { readonly kind: "empty" }
  | { readonly kind: "too-short"; readonly q: string }
  | { readonly kind: "too-long"; readonly q: string }
  | { readonly kind: "ready"; readonly q: string };

/** The typed text trimmed with white space collapsed (what the server matches). */
export function cleanSearchText(raw: string): string {
  return raw.trim().split(/\s+/).filter(Boolean).join(" ");
}

/** Whether the typed text is worth sending: not empty, at least 2 and at most 100 characters. */
export function searchTextState(raw: string): SearchTextState {
  const q = cleanSearchText(raw);
  if (q.length === 0) return { kind: "empty" };
  if (q.length < CHAT_SEARCH_LIMITS.minLength) return { kind: "too-short", q };
  if (raw.trim().length > CHAT_SEARCH_LIMITS.maxLength) {
    return { kind: "too-long", q };
  }
  return { kind: "ready", q };
}

/** The filters with the empty values left out, so equal searches share one cache key. */
export function canonicalFilters(
  filters: ChatSearchFilters,
): ChatSearchFilters {
  return {
    q: cleanSearchText(filters.q),
    ...(filters.spaceId ? { spaceId: filters.spaceId } : {}),
    ...(filters.authorId ? { authorId: filters.authorId } : {}),
    ...(filters.from ? { from: filters.from } : {}),
    ...(filters.to ? { to: filters.to } : {}),
    ...(filters.hasAttachment ? { hasAttachment: true } : {}),
    ...(filters.mentionsMe ? { mentionsMe: true } : {}),
  };
}

/** How many filters (besides the typed text) are on. */
export function activeFilterCount(filters: ChatSearchFilters): number {
  return [
    filters.spaceId,
    filters.authorId,
    filters.from,
    filters.to,
    filters.hasAttachment,
    filters.mentionsMe,
  ].filter(Boolean).length;
}

const DAY = /^(\d{4})-(\d{2})-(\d{2})$/;

/** The start of a calendar day (YYYY-MM-DD) in the reader's time zone as an ISO instant, or null when it is not a date. */
export function dayStartIso(day: string): string | null {
  const match = DAY.exec(day);
  if (!match) return null;
  const date = new Date(
    Number(match[1]),
    Number(match[2]) - 1,
    Number(match[3]),
  );
  if (
    date.getFullYear() !== Number(match[1]) ||
    date.getMonth() !== Number(match[2]) - 1 ||
    date.getDate() !== Number(match[3])
  ) {
    return null;
  }
  return date.toISOString();
}

/** The end of a calendar day as the start of the next one (the server's `to` is exclusive), or null when it is not a date. */
export function dayEndExclusiveIso(day: string): string | null {
  const start = dayStartIso(day);
  if (start === null) return null;
  const next = new Date(start);
  next.setDate(next.getDate() + 1);
  return next.toISOString();
}

/** A range that ends before it starts is not sent. */
export function rangeIsValid(from: string, to: string): boolean {
  if (!from || !to) return true;
  return from <= to;
}

export type SearchKey = "ArrowDown" | "ArrowUp" | "Home" | "End";

/** The option the highlight moves to; the list wraps and an empty list has none. */
export function nextHitIndex(
  current: number,
  count: number,
  key: SearchKey,
): number {
  if (count <= 0) return -1;
  switch (key) {
    case "ArrowDown":
      return current < 0 ? 0 : (current + 1) % count;
    case "ArrowUp":
      return current < 0 ? count - 1 : (current - 1 + count) % count;
    case "Home":
      return 0;
    case "End":
      return count - 1;
  }
}

/** Recent searches kept in memory only: newest first, no repeats, at most `max`. */
export function pushRecentSearch(
  recent: readonly string[],
  q: string,
  max = 5,
): readonly string[] {
  const text = cleanSearchText(q);
  if (text.length < CHAT_SEARCH_LIMITS.minLength) return recent;
  return [text, ...recent.filter((item) => item !== text)].slice(0, max);
}

/** Where a hit sits for the jump: a thread reply that was not also sent to the channel lives in its thread. */
export function hitTarget(hit: ChatSearchHit): "thread" | "channel" {
  return hit.threadRootId !== null && !hit.alsoSentToChannel
    ? "thread"
    : "channel";
}

/** Whether the hit is in a direct or group conversation. */
export function hitIsConversation(hit: ChatSearchHit): boolean {
  return hit.spaceKind === "direct" || hit.spaceKind === "group";
}
