/**
 * The Chat side of notification intents in the mock (UC-CHT-G09, CHT-019): a new message becomes an Inbox notification for the signed-in
 * person, with the rules the backend decides at delivery. The recipient must still be an active member of a live, not archived space;
 * a muted space gives nothing and a space at the Mentions level gives only mentions, broadcasts and followed threads; the author is never
 * notified; one reason per message (the most specific: mention, thread reply, direct message, broadcast); a repeat of the same fact adds
 * nothing. @here reaches present members and presence is not modelled, so it gives no notification. The mock inbox is the signed-in
 * person's own, so only their intent is written.
 */

import type { ChatMessageRecord, ChatSpaceRecord } from "./chat-model";
import type { MockChatState } from "./chat-state";
import { effectiveLevel, isConversationKind } from "./chat-access";

export type ChatNotificationReason =
  "mention" | "direct-message" | "thread-reply" | "broadcast";

const INBOX_TYPE = {
  mention: "chat_mention",
  "direct-message": "chat_direct",
  "thread-reply": "chat_thread",
  broadcast: "chat_broadcast",
} as const;

/** The longest preview the Inbox keeps (NotificationExcerpt.Length). */
export const NOTIFICATION_EXCERPT_LENGTH = 200;

function excerptOf(content: string | null): string | null {
  const plain = (content ?? "").replace(/\s+/g, " ").trim();
  if (plain.length === 0) return null;
  return plain.length <= NOTIFICATION_EXCERPT_LENGTH
    ? plain
    : `${plain.slice(0, NOTIFICATION_EXCERPT_LENGTH - 1)}…`;
}

/** The one reason a recipient is notified, or null (ChatNotificationHandoff.ReasonFor). */
export function reasonFor(
  level: "all" | "mentions" | "none",
  mentioned: boolean,
  followsThread: boolean,
  conversationMessage: boolean,
  broadcast: boolean,
): ChatNotificationReason | null {
  if (level === "none") return null;
  if (mentioned) return "mention";
  if (followsThread) return "thread-reply";
  if (conversationMessage && level === "all") return "direct-message";
  if (broadcast) return "broadcast";
  return null;
}

/**
 * Hands the message's intents to the mock Inbox: one per active member who is not the author and has a reason. Returns the number
 * written.
 */
export function deliverChatNotifications(
  state: MockChatState,
  space: ChatSpaceRecord,
  message: ChatMessageRecord,
): number {
  if (
    message.kind !== "user" ||
    message.deletedAt !== null ||
    space.deletedAt !== null ||
    space.archivedAt !== null
  ) {
    return 0;
  }
  const feed = state.store().feed;
  const mentions = state.mentions.get(message.id) ?? [];
  const broadcast = mentions.some((row) => row.kind === "broadcast-channel");
  const conversationMessage =
    isConversationKind(space.kind) && message.threadRootId === null;
  let written = 0;
  for (const membership of state.membershipsOfSpace(space.id)) {
    const recipient = membership.userId;
    if (membership.leftAt !== null || recipient === message.authorId) continue;
    const follower = message.threadRootId
      ? state.threadFollowers.get(`${message.threadRootId}|${recipient}`)
      : undefined;
    const reason = reasonFor(
      effectiveLevel(
        membership.notificationLevel,
        membership.mutedUntil,
        state.nowDate(),
      ),
      mentions.some((row) => row.kind === "user" && row.targetId === recipient),
      follower !== undefined && !follower.muted,
      conversationMessage,
      broadcast,
    );
    if (!reason) continue;
    const added = feed.addChatNotification({
      id: `chat-${reason}-${message.id}-${recipient}`,
      recipientId: recipient,
      createdAt: message.createdAt,
      type: INBOX_TYPE[reason],
      actorId: message.authorId,
      chatSpaceId: space.id,
      excerpt: excerptOf(message.content),
    });
    if (added) written += 1;
  }
  return written;
}

/**
 * Tells the author that a moderator removed their message (UC-CHT-H03): one notice with no text, only while the author is still an
 * active member of the space (ChatNotificationHandoff.HandOffRemovalAsync).
 */
export function deliverRemovalNotification(
  state: MockChatState,
  space: ChatSpaceRecord,
  message: ChatMessageRecord,
  moderatorId: string,
): boolean {
  if (message.authorId === moderatorId || space.deletedAt !== null)
    return false;
  const stays = state
    .membershipsOfSpace(space.id)
    .some((m) => m.userId === message.authorId && m.leftAt === null);
  if (!stays) return false;
  return state.store().feed.addChatNotification({
    id: `chat-message-removed-${message.id}-${message.authorId}`,
    recipientId: message.authorId,
    createdAt: message.deletedAt ?? message.createdAt,
    type: "chat_removed",
    actorId: moderatorId,
    chatSpaceId: space.id,
    excerpt: null,
  });
}
