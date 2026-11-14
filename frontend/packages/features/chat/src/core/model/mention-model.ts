/**
 * Mentions in the composer and in the message list (UC-CHT-G01 to G04). Pure rules, no React:
 * the trigger the caret sits in, the insertion of a picked entry, the structured spans a send carries
 * (positions are UTF-16 characters of the content as sent), the reader's split of a message into
 * text and chips, and the warnings the composer shows before a send.
 */

import type {
  ChatMention,
  ChatMentionInput,
  ChatMentionable,
  ChatMentionables,
  ChatMessage,
} from "../types/chat";

/** The longest query a trigger keeps open (a name with spaces still matches; a sentence does not). */
export const MENTION_QUERY_MAX = 30;

export type MentionTrigger = "@" | "#";

/** The trigger the caret is in: `start` is the index of the @ or #, `query` the text typed after it. */
export interface ActiveTrigger {
  readonly trigger: MentionTrigger;
  readonly start: number;
  readonly query: string;
}

/** A person or channel the caller picked from the list and the text that stands for it in the draft. */
export interface TrackedMention {
  readonly kind: "user" | "channel-link";
  /** The text inserted in the draft, with its @ or # (for example "@Minh Quân"). */
  readonly label: string;
  readonly targetId: string;
}

const WORD = /[\p{L}\p{N}_]/u;

/** The @ or # the caret sits right after, when it starts a word and no whitespace break or long query follows it. */
export function activeTrigger(
  text: string,
  caret: number,
): ActiveTrigger | null {
  const before = text.slice(0, caret);
  for (let i = before.length - 1; i >= 0; i -= 1) {
    const ch = before[i]!;
    if (ch === "\n") return null;
    if (ch === "@" || ch === "#") {
      const prev = i === 0 ? "" : before[i - 1]!;
      if (prev !== "" && !/\s/u.test(prev)) return null;
      const query = before.slice(i + 1);
      if (query.length > MENTION_QUERY_MAX || /^\s/u.test(query)) return null;
      return { trigger: ch, start: i, query };
    }
    if (before.length - 1 - i > MENTION_QUERY_MAX) return null;
  }
  return null;
}

/** Lower-cased and without accents, so "quan" finds "Quân" and "dung" finds "Đũng". */
export function foldText(text: string): string {
  return text
    .normalize("NFD")
    .replace(/\p{M}/gu, "")
    .replace(/đ/gi, "d")
    .toLowerCase();
}

/** Entries whose name contains the query (names that start with it first), in a stable order. */
export function filterByQuery<T>(
  entries: readonly T[],
  query: string,
  nameOf: (entry: T) => string,
): T[] {
  const needle = foldText(query.trim());
  if (needle === "") return [...entries];
  const starts: T[] = [];
  const contains: T[] = [];
  for (const entry of entries) {
    const name = foldText(nameOf(entry));
    if (name.startsWith(needle)) starts.push(entry);
    else if (name.includes(needle)) contains.push(entry);
  }
  return [...starts, ...contains];
}

/** Replaces the trigger and its query with the label and one space; the caret goes after the space. */
export function insertMention(
  text: string,
  active: ActiveTrigger,
  caret: number,
  label: string,
): { readonly text: string; readonly caret: number } {
  const head = text.slice(0, active.start);
  const tail = text.slice(caret);
  const spacer = tail.startsWith(" ") ? "" : " ";
  const next = `${head}${label}${spacer}${tail}`;
  return { text: next, caret: head.length + label.length + 1 };
}

const BROADCAST = /@(channel|here)/giu;

function isBoundaryBefore(content: string, index: number): boolean {
  if (index === 0) return true;
  const prev = content[index - 1]!;
  return !WORD.test(prev) && prev !== "@" && prev !== "#";
}

function isBoundaryAfter(content: string, end: number): boolean {
  if (end >= content.length) return true;
  return !WORD.test(content[end]!);
}

/** Every place a tracked label (and, when allowed, @channel or @here) stands in the content, ordered and without overlap. */
export function buildMentionInputs(
  content: string,
  tracked: readonly TrackedMention[],
  options: { readonly canBroadcast: boolean },
): ChatMentionInput[] {
  const found: ChatMentionInput[] = [];
  for (const entry of tracked) {
    if (entry.label.length < 2) continue;
    let from = 0;
    for (;;) {
      const at = content.indexOf(entry.label, from);
      if (at === -1) break;
      from = at + 1;
      if (
        !isBoundaryBefore(content, at) ||
        !isBoundaryAfter(content, at + entry.label.length)
      ) {
        continue;
      }
      found.push(
        entry.kind === "user"
          ? {
              kind: "user",
              start: at,
              length: entry.label.length,
              userId: entry.targetId,
            }
          : {
              kind: "channel-link",
              start: at,
              length: entry.label.length,
              spaceId: entry.targetId,
            },
      );
    }
  }
  if (options.canBroadcast) {
    for (const match of content.matchAll(BROADCAST)) {
      const at = match.index ?? 0;
      if (
        !isBoundaryBefore(content, at) ||
        !isBoundaryAfter(content, at + match[0].length)
      ) {
        continue;
      }
      found.push({
        kind:
          match[1]!.toLowerCase() === "channel"
            ? "broadcast-channel"
            : "broadcast-here",
        start: at,
        length: match[0].length,
      });
    }
  }
  // Left to right; at one position the longer label wins, so "@An Binh" is not read as "@An".
  found.sort((a, b) => a.start - b.start || b.length - a.length);
  const result: ChatMentionInput[] = [];
  let end = 0;
  for (const span of found) {
    if (span.start < end) continue;
    result.push(span);
    end = span.start + span.length;
  }
  return result.slice(0, 50);
}

/** Tracked mentions of a stored message (an edit starts from them, so its spans are kept). */
export function trackedFromMessage(
  content: string,
  mentions: readonly ChatMention[] | undefined,
): TrackedMention[] {
  const result: TrackedMention[] = [];
  for (const mention of mentions ?? []) {
    const label = content.slice(mention.start, mention.start + mention.length);
    if (mention.kind === "user" && mention.userId) {
      result.push({ kind: "user", label, targetId: mention.userId });
    } else if (mention.kind === "channel-link" && mention.spaceId) {
      result.push({ kind: "channel-link", label, targetId: mention.spaceId });
    }
  }
  return result;
}

export function hasBroadcast(inputs: readonly ChatMentionInput[]): boolean {
  return inputs.some(
    (input) =>
      input.kind === "broadcast-channel" || input.kind === "broadcast-here",
  );
}

/** A piece of a message: plain text or one mention with the text it covers. */
export type MentionSegment =
  | { readonly type: "text"; readonly text: string }
  | {
      readonly type: "mention";
      readonly mention: ChatMention;
      readonly text: string;
    };

/** Splits the content at its mentions; a span that does not fit the text or overlaps another is shown as text. */
export function splitByMentions(
  content: string,
  mentions: readonly ChatMention[] | undefined,
): MentionSegment[] {
  const ordered = [...(mentions ?? [])].sort((a, b) => a.start - b.start);
  const segments: MentionSegment[] = [];
  let at = 0;
  for (const mention of ordered) {
    if (
      mention.start < at ||
      mention.length < 1 ||
      mention.start + mention.length > content.length
    ) {
      continue;
    }
    if (mention.start > at) {
      segments.push({ type: "text", text: content.slice(at, mention.start) });
    }
    segments.push({
      type: "mention",
      mention,
      text: content.slice(mention.start, mention.start + mention.length),
    });
    at = mention.start + mention.length;
  }
  if (at < content.length) {
    segments.push({ type: "text", text: content.slice(at) });
  }
  return segments;
}

/** Whether the message mentions the viewer by id or with @channel or @here (the highlight of the design). */
export function mentionsViewer(
  message: Pick<ChatMessage, "mentions" | "authorId" | "deleted" | "kind">,
  viewerId: string,
): boolean {
  if (message.deleted || message.kind !== "user") return false;
  if (message.authorId === viewerId) return false;
  return (message.mentions ?? []).some(
    (mention) =>
      (mention.kind === "user" && mention.userId === viewerId) ||
      mention.kind === "broadcast-channel" ||
      mention.kind === "broadcast-here",
  );
}

/** What the composer warns about before a send. */
export interface MentionWarning {
  readonly code: "cannot-view" | "broadcast-not-allowed" | "no-audience";
  readonly userId: string | null;
  readonly label: string;
}

/**
 * The warnings of a draft's mentions (UC-CHT-G02, G03): a person outside the space is stored and not notified; a broadcast
 * the caller may not use, or with no one else to reach, stays plain text.
 */
export function mentionWarnings(
  inputs: readonly ChatMentionInput[],
  tracked: readonly TrackedMention[],
  mentionables: ChatMentionables | undefined,
  content: string,
  broadcastTyped: boolean,
): MentionWarning[] {
  if (!mentionables) return [];
  const outside = new Map<string, ChatMentionable>();
  for (const person of mentionables.people) {
    if (!person.inSpace) outside.set(person.userId, person);
  }
  const warnings: MentionWarning[] = [];
  const seen = new Set<string>();
  for (const input of inputs) {
    if (input.kind !== "user" || !input.userId) continue;
    if (!outside.has(input.userId) || seen.has(input.userId)) continue;
    seen.add(input.userId);
    warnings.push({
      code: "cannot-view",
      userId: input.userId,
      label:
        tracked.find((entry) => entry.targetId === input.userId)?.label ??
        content.slice(input.start, input.start + input.length),
    });
  }
  if (broadcastTyped) {
    if (!mentionables.canBroadcast) {
      warnings.push({
        code: "broadcast-not-allowed",
        userId: null,
        label: "@channel",
      });
    } else if (mentionables.memberCount < 2) {
      warnings.push({ code: "no-audience", userId: null, label: "@channel" });
    }
  }
  return warnings;
}

/** Whether the content has an @channel or @here the caller would send as a broadcast. */
export function typesBroadcast(content: string): boolean {
  for (const match of content.matchAll(BROADCAST)) {
    const at = match.index ?? 0;
    if (
      isBoundaryBefore(content, at) &&
      isBoundaryAfter(content, at + match[0].length)
    ) {
      return true;
    }
  }
  return false;
}

/** A broadcast needs the confirmation step above the member threshold (UC-CHT-G03). */
export function needsBroadcastConfirm(
  inputs: readonly ChatMentionInput[],
  mentionables: ChatMentionables | undefined,
): boolean {
  return (
    mentionables !== undefined &&
    hasBroadcast(inputs) &&
    mentionables.memberCount > mentionables.broadcastConfirmAbove
  );
}
