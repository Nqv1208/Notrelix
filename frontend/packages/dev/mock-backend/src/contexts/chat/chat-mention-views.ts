/**
 * The reads of a message's mentions that need no access helpers: the reader's view of them (backend MessageMentions) and the
 * mention counter (backend MentionCounts). Kept apart from the write rules in chat-mentions so the wire views in chat-access can
 * use them without a cycle.
 */

import type { ChatMentionKind, ChatMentionRecord } from "./chat-model";
import type { MockChatState } from "./chat-state";

/** MentionDto: one stored mention as a reader sees it. */
export interface MentionView {
  kind: ChatMentionKind;
  start: number;
  length: number;
  userId: string | null;
  spaceId: string | null;
}

/** The channels a person may see (backend VisibleChannelsAsync): a live channel that is public (not for a Guest) or that they are in. */
export function canSeeChannel(
  state: MockChatState,
  workspaceId: string,
  userId: string,
  isGuest: boolean,
  spaceId: string,
): boolean {
  const space = state.spaces.get(spaceId);
  if (!space || space.workspaceId !== workspaceId || space.deletedAt !== null) {
    return false;
  }
  if (!isGuest && space.kind === "channel_public") return true;
  return state.activeMembershipOf(spaceId, userId) !== undefined;
}

/**
 * The mentions of a message as the viewer sees them (backend MessageMentions): a channel link the viewer cannot see is left out, so a
 * private channel is never revealed to someone outside it.
 */
export function mentionViewsOf(
  state: MockChatState,
  workspaceId: string,
  viewerUserId: string,
  messageId: string,
): MentionView[] {
  return (state.mentions.get(messageId) ?? [])
    .filter(
      (span) =>
        span.kind !== "channel-link" ||
        canSeeChannel(state, workspaceId, viewerUserId, false, span.targetId!),
    )
    .map((span) => ({
      kind: span.kind,
      start: span.start,
      length: span.length,
      userId: span.kind === "user" ? span.targetId : null,
      spaceId: span.kind === "channel-link" ? span.targetId : null,
    }));
}

/**
 * How many unread messages of the space mention the person (backend MentionCounts): a user message that is not deleted, not their own,
 * after their read boundary, sent after they joined, that mentions them by id or carries an @channel or @here. A message with several
 * mentions of the person counts once.
 */
export function mentionCountIn(
  state: MockChatState,
  spaceId: string,
  userId: string,
  readSequence: number,
  joinedAt: string,
): number {
  const joined = Date.parse(joinedAt);
  let count = 0;
  for (const message of state.messagesOf(spaceId)) {
    if (
      message.sequence <= readSequence ||
      message.authorId === userId ||
      message.kind !== "user" ||
      message.deletedAt !== null ||
      Date.parse(message.createdAt) < joined
    ) {
      continue;
    }
    const spans = state.mentions.get(message.id) ?? [];
    if (
      spans.some(
        (span) =>
          (span.kind === "user" && span.targetId === userId) ||
          span.kind === "broadcast-channel" ||
          span.kind === "broadcast-here",
      )
    ) {
      count += 1;
    }
  }
  return count;
}
