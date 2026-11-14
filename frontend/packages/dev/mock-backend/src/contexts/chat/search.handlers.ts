/**
 * Message search (UC-CHT-H07, CHT-022, CHT-004): `Chat.Search.Query`. The mock mirrors the backend: the typed text is a phrase matched
 * ignoring case and Vietnamese marks, only inside the spaces `visibleSpaceIds` returns (the rule of `requireVisible`), tombstones, system
 * messages and messages past the retention window are never returned, newest first by cursor, a snippet of plain text with the match ranges.
 * A space the caller cannot see gives an empty page. The text is never kept.
 */

import { defineMockOperation } from "../../operations/types";
import { ok } from "../../transport/create-response";
import {
  callerIn,
  guarded,
  requireAction,
  validationProblem,
  visibleSpaceIds,
} from "./chat-access";
import { requireWithinLimit } from "./chat-limits";
import type { ChatMessageRecord } from "./chat-model";
import type { MockChatState } from "./chat-state";

type WorkspaceParams = { workspaceId: string };

const MIN_LENGTH = 2;
const MAX_LENGTH = 100;
const PAGE_DEFAULT = 20;
const PAGE_MAX = 50;
const SNIPPET_BEFORE = 40;
const SNIPPET_AFTER = 100;

/** Lower case, without combining marks, "đ" as "d" (the backend SearchTextFolding.Fold). */
export function foldSearchText(text: string): string {
  return text
    .replace(/đ/g, "d")
    .replace(/Đ/g, "D")
    .normalize("NFD")
    .replace(/\p{M}/gu, "")
    .toLowerCase()
    .normalize("NFC");
}

function cleanText(typed: string): string {
  return typed.trim().split(/\s+/).filter(Boolean).join(" ");
}

interface Snippet {
  text: string;
  ranges: { start: number; length: number }[];
  cutBefore: boolean;
  cutAfter: boolean;
}

/** A window of the content around the first match and the ranges of every match inside it, in the original letters. */
export function buildSnippet(content: string, typed: string): Snippet {
  const needle = foldSearchText(cleanText(typed));
  let folded = "";
  const origin: number[] = [];
  for (let at = 0; at < content.length; at += 1) {
    const piece = foldSearchText(content[at]!);
    for (const character of piece) {
      folded += character;
      origin.push(at);
    }
  }
  const matches: { start: number; end: number }[] = [];
  if (needle.length > 0) {
    let from = 0;
    while (from <= folded.length - needle.length) {
      const found = folded.indexOf(needle, from);
      if (found < 0) break;
      matches.push({
        start: origin[found]!,
        end: origin[found + needle.length - 1]! + 1,
      });
      from = found + needle.length;
    }
  }
  const first = matches[0] ?? { start: 0, end: 0 };
  let start = Math.max(0, first.start - SNIPPET_BEFORE);
  let end = Math.min(
    content.length,
    Math.max(first.end, first.start) + SNIPPET_AFTER,
  );
  const low = (code: number) => code >= 0xdc00 && code <= 0xdfff;
  if (start > 0 && low(content.charCodeAt(start))) start -= 1;
  if (end < content.length && low(content.charCodeAt(end))) end += 1;
  return {
    text: content.slice(start, end),
    ranges: matches
      .filter((match) => match.start >= start && match.end <= end)
      .map((match) => ({
        start: match.start - start,
        length: match.end - match.start,
      })),
    cutBefore: start > 0,
    cutAfter: end < content.length,
  };
}

function parseLimit(raw: string | null): number {
  if (raw === null) return PAGE_DEFAULT;
  const value = Number(raw);
  if (!Number.isInteger(value) || value < 1 || value > PAGE_MAX) {
    validationProblem(`limit must be between 1 and ${PAGE_MAX}.`);
  }
  return value;
}

/** An instant as epoch milliseconds (the mock reads no wall clock; `Date.parse` only reads the given text). */
function parseInstant(raw: string | null, name: string): number | null {
  if (raw === null) return null;
  const time = Date.parse(raw);
  if (Number.isNaN(time)) validationProblem(`${name} is not a valid time.`);
  return time;
}

function parseCursor(raw: string | null): { at: number; id: string } | null {
  if (raw === null) return null;
  const dot = raw.indexOf(".");
  const ms = Number(raw.slice(0, dot));
  if (dot < 1 || !Number.isInteger(ms) || ms < 0 || raw.length === dot + 1) {
    validationProblem("cursor is not valid.");
  }
  return { at: ms, id: raw.slice(dot + 1) };
}

function newestFirst(a: ChatMessageRecord, b: ChatMessageRecord): number {
  return a.createdAt < b.createdAt
    ? 1
    : a.createdAt > b.createdAt
      ? -1
      : a.id < b.id
        ? 1
        : a.id > b.id
          ? -1
          : 0;
}

/** The hits of one search, newest first, before paging. */
function matchesOf(
  state: MockChatState,
  workspaceId: string,
  userId: string,
  visible: ReadonlySet<string>,
  filters: {
    needle: string;
    spaceId: string | null;
    authorId: string | null;
    from: number | null;
    to: number | null;
    hasAttachment: boolean;
    mentionsMe: boolean;
  },
): ChatMessageRecord[] {
  const days = state.retentionPolicies.get(workspaceId)?.retentionDays ?? null;
  const cutoff =
    days === null
      ? null
      : state
          .store()
          .getClock()
          .offsetSeconds(state.virtualSeconds() - days * 86_400);
  const rows: ChatMessageRecord[] = [];
  for (const spaceId of visible) {
    if (filters.spaceId !== null && filters.spaceId !== spaceId) continue;
    for (const message of state.messagesOf(spaceId)) {
      if (
        message.kind !== "user" ||
        message.deletedAt !== null ||
        message.content === null ||
        !foldSearchText(message.content).includes(filters.needle)
      ) {
        continue;
      }
      if (filters.authorId !== null && message.authorId !== filters.authorId)
        continue;
      const at = Date.parse(message.createdAt);
      if (filters.from !== null && at < filters.from) continue;
      if (filters.to !== null && at >= filters.to) continue;
      if (cutoff !== null && at < Date.parse(cutoff)) continue;
      if (
        filters.hasAttachment &&
        state.attachmentsOfMessage(message.id).length === 0
      )
        continue;
      if (
        filters.mentionsMe &&
        !(state.mentions.get(message.id) ?? []).some(
          (mention) => mention.kind === "user" && mention.targetId === userId,
        )
      ) {
        continue;
      }
      rows.push(message);
    }
  }
  return rows.sort(newestFirst);
}

export const searchOperations = [
  defineMockOperation<WorkspaceParams>({
    id: "chat.search.query",
    contract: { kind: "openapi", operationId: "Chat.Search.Query" },
    method: "GET",
    route: "/workspaces/:workspaceId/chat/search",
    async handle({ params, query, store }) {
      return guarded(async () => {
        const state = store.chat;
        const caller = callerIn(store, params.workspaceId);
        requireAction(caller, "search");
        // Counted per person and per Workspace, before the input is judged (the backend's order after the pipeline's validation).
        const raw = query.q ?? "";
        const cleaned = cleanText(raw);
        if (cleaned.length < MIN_LENGTH)
          validationProblem(`q needs at least ${MIN_LENGTH} characters.`);
        if (raw.trim().length > MAX_LENGTH)
          validationProblem(`q is at most ${MAX_LENGTH} characters.`);
        const limit = parseLimit(query.limit ?? null);
        const from = parseInstant(query.from ?? null, "from");
        const to = parseInstant(query.to ?? null, "to");
        if (from !== null && to !== null && to <= from)
          validationProblem("The range must end after it starts.");
        const cursor = parseCursor(query.cursor ?? null);
        requireWithinLimit(state, "search", params.workspaceId, caller.userId);

        const visible = visibleSpaceIds(state, caller);
        let rows = matchesOf(
          state,
          params.workspaceId,
          caller.userId,
          visible,
          {
            needle: foldSearchText(cleaned),
            spaceId: query.spaceId ?? null,
            authorId: query.authorId ?? null,
            from,
            to,
            hasAttachment: query.hasAttachment === "true",
            mentionsMe: query.mentionsMe === "true",
          },
        );
        if (cursor !== null) {
          rows = rows.filter(
            (row) =>
              Date.parse(row.createdAt) < cursor.at ||
              (Date.parse(row.createdAt) === cursor.at && row.id < cursor.id),
          );
        }
        const page = rows.slice(0, limit);
        const last = page[page.length - 1];
        const items = page.map((message) => {
          const space = state.spaces.get(message.spaceId)!;
          return {
            messageId: message.id,
            spaceId: space.id,
            spaceName: space.name,
            spaceKind: space.kind,
            spaceArchived: space.archivedAt !== null,
            authorId: message.authorId,
            createdAt: message.createdAt,
            sequence: message.sequence,
            threadRootId: message.threadRootId,
            alsoSentToChannel: message.alsoSentToChannel,
            snippet: buildSnippet(message.content!, cleaned),
          };
        });
        return ok({
          items,
          nextCursor:
            rows.length > limit && last
              ? `${Date.parse(last.createdAt)}.${last.id}`
              : null,
        });
      });
    },
  }),
];
