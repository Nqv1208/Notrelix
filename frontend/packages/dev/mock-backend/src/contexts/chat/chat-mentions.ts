/**
 * Mentions and broadcast (UC-CHT-G01 to G04, D04): the shape rules of a message's mentions (backend MentionSpans), the checks
 * against the space and the Workspace (backend MentionResolver), the counters (backend MentionCounts) and the reader's view of a
 * message's mentions (backend MessageMentions). The mock mirrors the backend; it does not own these rules.
 *
 * The server never trusts the client: a person in the space is stored; a Workspace member outside it is stored and the sender is
 * warned (cannot-view: nobody is notified, nothing is granted, the person is never counted); anyone else is plain text. A channel
 * link is stored when the sender can see the channel and is plain text otherwise, without telling a missing channel from a private
 * one. A broadcast needs the broadcast action, a non-Guest sender and someone else to reach, and above the member threshold it
 * needs the confirmation.
 */

import {
  ChatFailure,
  fail,
  requireAction,
  type ChatCaller,
} from "./chat-access";
import { allowsAction } from "./chat-permissions";
import {
  CHAT_ERROR,
  CHAT_LIMITS,
  CHAT_MENTION_NOTICE,
  type ChatMentionKind,
  type ChatMentionRecord,
  type ChatSpaceRecord,
} from "./chat-model";
import type { MockChatState } from "./chat-state";
import { canSeeChannel } from "./chat-mention-views";

/** One mention a client sends (backend MentionRequest). Start and Length count UTF-16 characters of the content as sent. */
export interface MentionInputBody {
  kind?: string | null;
  start?: number | null;
  length?: number | null;
  userId?: string | null;
  spaceId?: string | null;
}

/** MentionNoticeDto: what the send did not do for one mention. */
export interface MentionNotice {
  code: string;
  start: number;
  length: number;
  userId: string | null;
}

export interface MentionResolution {
  readonly spans: readonly ChatMentionRecord[];
  readonly notices: readonly MentionNotice[];
}

const KINDS: readonly ChatMentionKind[] = [
  "user",
  "channel-link",
  "broadcast-channel",
  "broadcast-here",
];
const CHANNEL_TOKEN = "@channel";
const HERE_TOKEN = "@here";

/** A malformed mention is a domain rule: 400 with the business-rule code (backend mention-invalid, mention-limit-exceeded). */
function invalid(detail: string): never {
  return fail(400, CHAT_ERROR.businessRule, "Business rule violation", detail);
}

/**
 * The shape rules (backend MentionSpans.Normalize): spans fit the text, do not overlap, and match their kind; the result is ordered
 * by position with the positions moved onto the trimmed text the message stores.
 */
export function normalizeSpans(
  sentContent: string,
  inputs: readonly MentionInputBody[],
): ChatMentionRecord[] {
  if (inputs.length === 0) return [];
  if (inputs.length > CHAT_LIMITS.mentionsPerMessageMax) {
    invalid(
      `A message has at most ${CHAT_LIMITS.mentionsPerMessageMax} mentions.`,
    );
  }
  const lead = sentContent.length - sentContent.trimStart().length;
  const text = sentContent.trim();
  const spans = inputs.map((input) => {
    const kind = KINDS.find((k) => k === input.kind);
    if (!kind) invalid("The mention kind is not known.");
    const start = typeof input.start === "number" ? input.start : -1;
    const length = typeof input.length === "number" ? input.length : 0;
    const target =
      kind === "user"
        ? (input.userId ?? null)
        : kind === "channel-link"
          ? (input.spaceId ?? null)
          : null;
    return { kind: kind!, start, length, targetId: target };
  });
  spans.sort((a, b) => a.start - b.start);
  const result: ChatMentionRecord[] = [];
  let end = 0;
  for (const span of spans) {
    const start = span.start - lead;
    if (start < end || span.length < 1 || start + span.length > text.length) {
      invalid("A mention is outside the text or overlaps another one.");
    }
    const covered = text.substring(start, start + span.length);
    switch (span.kind) {
      case "user":
        if (span.targetId === null || span.targetId === "") {
          invalid(
            "A person's mention and a channel link name their target by id.",
          );
        }
        if (covered.length < 2 || !covered.startsWith("@")) {
          invalid("A person's mention covers text that starts with @.");
        }
        break;
      case "channel-link":
        if (span.targetId === null || span.targetId === "") {
          invalid(
            "A person's mention and a channel link name their target by id.",
          );
        }
        if (covered.length < 2 || !covered.startsWith("#")) {
          invalid("A channel link covers text that starts with #.");
        }
        break;
      case "broadcast-channel":
        if (covered.toLowerCase() !== CHANNEL_TOKEN) {
          invalid("A broadcast covers the text @channel.");
        }
        break;
      case "broadcast-here":
        if (covered.toLowerCase() !== HERE_TOKEN) {
          invalid("A broadcast covers the text @here.");
        }
        break;
    }
    if (
      (span.kind === "broadcast-channel" || span.kind === "broadcast-here") &&
      span.targetId !== null
    ) {
      invalid("A broadcast names no target.");
    }
    result.push({ ...span, start });
    end = start + span.length;
  }
  return result;
}

/** Whether the person is an active member of the Workspace (the mock lists members of the Workspace only). */
function isWorkspaceMember(
  state: MockChatState,
  workspaceId: string,
  userId: string,
): boolean {
  return state
    .store()
    .getWorkspaceMembers(workspaceId)
    .some((row) => row.userId === userId);
}

/** Decides which mentions are stored and what the sender is told (backend MentionResolver.ResolveAsync). */
export function resolveMentions(
  state: MockChatState,
  caller: ChatCaller,
  space: ChatSpaceRecord,
  sentContent: string,
  inputs: readonly MentionInputBody[] | null | undefined,
  confirmBroadcast: boolean,
): MentionResolution {
  if (!inputs || inputs.length === 0) return { spans: [], notices: [] };
  const spans = normalizeSpans(sentContent, inputs);
  const isGuest = caller.role === "Guest";
  const activeMembers = state
    .membershipsOfSpace(space.id)
    .filter((row) => row.leftAt === null).length;
  const stored: ChatMentionRecord[] = [];
  const notices: MentionNotice[] = [];
  let sawBroadcast = false;
  for (const span of spans) {
    if (span.kind === "user") {
      const target = span.targetId!;
      if (state.activeMembershipOf(space.id, target)) {
        stored.push(span);
      } else if (
        !isGuest &&
        isWorkspaceMember(state, space.workspaceId, target)
      ) {
        stored.push(span);
        notices.push({
          code: CHAT_MENTION_NOTICE.cannotView,
          start: span.start,
          length: span.length,
          userId: target,
        });
      } else {
        notices.push({
          code: CHAT_MENTION_NOTICE.unknown,
          start: span.start,
          length: span.length,
          userId: null,
        });
      }
    } else if (span.kind === "channel-link") {
      if (
        canSeeChannel(
          state,
          space.workspaceId,
          caller.userId,
          isGuest,
          span.targetId!,
        )
      ) {
        stored.push(span);
      } else {
        notices.push({
          code: CHAT_MENTION_NOTICE.linkNotVisible,
          start: span.start,
          length: span.length,
          userId: null,
        });
      }
    } else if (!canBroadcast(state, space, caller)) {
      notices.push({
        code: CHAT_MENTION_NOTICE.broadcastNotAllowed,
        start: span.start,
        length: span.length,
        userId: null,
      });
    } else if (activeMembers < 2 || space.kind === "direct") {
      // A direct conversation has one other person: there is no audience to broadcast to (UC-CHT-G03).
      notices.push({
        code: CHAT_MENTION_NOTICE.noAudience,
        start: span.start,
        length: span.length,
        userId: null,
      });
    } else {
      sawBroadcast = true;
      stored.push(span);
    }
  }
  if (
    sawBroadcast &&
    activeMembers > CHAT_LIMITS.broadcastConfirmThreshold &&
    !confirmBroadcast
  ) {
    fail(
      409,
      CHAT_ERROR.broadcastConfirmationRequired,
      "Broadcast confirmation required",
      `@channel and @here in a channel of more than ${CHAT_LIMITS.broadcastConfirmThreshold} members need confirmation.`,
    );
  }
  return { spans: stored, notices };
}

/** The broadcast action (chat.mention.broadcast): Owner and Admin by the baseline; a Member or a Guest is denied (a Guest never broadcasts). */
export function canBroadcast(
  state: MockChatState,
  space: ChatSpaceRecord,
  caller: ChatCaller,
): boolean {
  if (caller.role === "Guest") return false;
  if (!allowsAction(state, space, caller, "broadcast")) return false;
  try {
    requireAction(caller, "mentionBroadcast");
    return true;
  } catch (error) {
    if (error instanceof ChatFailure) return false;
    throw error;
  }
}

/** Replaces a message's stored mentions with the resolved set (a send stores the first set; an edit replaces it). */
export function storeMentions(
  state: MockChatState,
  messageId: string,
  spans: readonly ChatMentionRecord[],
): void {
  if (spans.length === 0) state.mentions.delete(messageId);
  else state.mentions.set(messageId, [...spans]);
}

/** The stored warnings of a retry (backend StoredWarnings): each stored person who is not an active member of the space now. */
export function storedWarnings(
  state: MockChatState,
  spaceId: string,
  spans: readonly ChatMentionRecord[],
): MentionNotice[] {
  return spans
    .filter(
      (span) =>
        span.kind === "user" &&
        !state.activeMembershipOf(spaceId, span.targetId!),
    )
    .map((span) => ({
      code: CHAT_MENTION_NOTICE.cannotView,
      start: span.start,
      length: span.length,
      userId: span.targetId,
    }));
}
