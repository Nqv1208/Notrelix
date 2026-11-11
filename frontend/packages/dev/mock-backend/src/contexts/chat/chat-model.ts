/**
 * Chat context: the records the mock keeps and the limits and stable codes the
 * backend fixes (backend/src/Notrelix.Domain/Chat/Rules). The mock mirrors the
 * backend's behaviour; it does not own any of these values.
 *
 * Plan: docs/workstreams/chat-delivery-plan.md (S1.6), docs/workstreams/specs/chat-foundation.md §15-§22
 */

export type ChatSpaceKind = "channel_public" | "channel_private";
export type ChatMembershipRole = "manager" | "member";
export type ChatNotificationLevel = "all" | "mentions" | "none";
export type ChatMessageKind = "user" | "system";

/** The workspace role names the backend's ChannelAccess compares against. */
export type ChatWorkspaceRole = "Owner" | "Admin" | "Member" | "Guest";

export interface ChatSpaceRecord {
  readonly id: string;
  readonly workspaceId: string;
  readonly kind: ChatSpaceKind;
  name: string;
  topic: string | null;
  groupId: string | null;
  position: number;
  archivedAt: string | null;
  deletedAt: string | null;
  /** Sequence of the last message stored in the space; starts at 0. */
  lastSequence: number;
  version: number;
  readonly createdAt: string;
  readonly createdBy: string;
}

export interface ChatMembershipRecord {
  readonly spaceId: string;
  readonly workspaceId: string;
  readonly userId: string;
  role: ChatMembershipRole;
  notificationLevel: ChatNotificationLevel;
  /** Set only with a timed mute (level "none"). */
  mutedUntil: string | null;
  readSequence: number;
  /** Mentions are not recorded before S2.4: always 0. */
  mentionCount: number;
  readonly joinedAt: string;
  leftAt: string | null;
}

export interface ChatMessageRecord {
  readonly id: string;
  readonly workspaceId: string;
  readonly spaceId: string;
  readonly sequence: number;
  readonly authorId: string;
  readonly kind: ChatMessageKind;
  /** Null for a tombstone. */
  content: string | null;
  /** Null for system messages. */
  readonly clientMessageId: string | null;
  readonly replyToId: string | null;
  readonly createdAt: string;
  editedAt: string | null;
  deletedAt: string | null;
  deletedBy: string | null;
  version: number;
}

export interface ChatGroupRecord {
  readonly id: string;
  readonly workspaceId: string;
  name: string;
  position: number;
  version: number;
  readonly createdAt: string;
}

/** The limits of ChatLimits (backend/src/Notrelix.Domain/Chat/Rules/ChatLimits.cs). */
export const CHAT_LIMITS = {
  messageMaxLength: 4000,
  pageSizeDefault: 50,
  pageSizeMax: 100,
  clientMessageIdMaxLength: 128,
  channelNameMaxLength: 80,
  channelTopicMaxLength: 250,
  channelGroupNameMaxLength: 60,
  reorderMax: 200,
  /** The largest message payload of a realtime frame, in UTF-8 bytes (ChatRealtime.MaxPayloadBytes). */
  realtimePayloadMaxBytes: 3000,
  /** Recipients of one message's unread fan-out (ChatRealtime.MaxUnreadRecipients). */
  unreadRecipientsMax: 2000,
} as const;

/** The stable error codes (ChatRuleCodes and the ProblemDetails mapper). */
export const CHAT_ERROR = {
  channelArchived: "channel-archived",
  nameConflict: "name-conflict",
  lastManager: "last-manager",
  messageDeleted: "message-deleted",
  systemMessageImmutable: "system-message-immutable",
  notFound: "resource.not_found",
  forbidden: "auth.forbidden",
  validation: "validation.failed",
  precondition: "common.precondition-failed",
  idempotencyPayloadMismatch: "idempotency_payload_mismatch",
} as const;

/** The announcements the backend writes as system messages (ChatSystemMessages). */
export const CHAT_SYSTEM_TEXT = {
  memberJoined: "joined the channel",
  memberLeft: "left the channel",
  memberRemoved: "was removed from the channel",
  memberAdded: "added a member to the channel",
  archived: "archived the channel",
  unarchived: "unarchived the channel",
  renamed: (name: string) => `renamed the channel to ${name}`,
} as const;
