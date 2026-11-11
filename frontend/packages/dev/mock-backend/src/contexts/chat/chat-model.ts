/**
 * Chat context: the records the mock keeps and the limits and stable codes the
 * backend fixes (backend/src/Notrelix.Domain/Chat/Rules). The mock mirrors the
 * backend's behaviour; it does not own any of these values.
 *
 * Plan: docs/workstreams/chat-delivery-plan.md (S1.6), docs/workstreams/specs/chat-foundation.md §15-§22
 */

export type ChatSpaceKind =
  "channel_public" | "channel_private" | "direct" | "group";
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
  /** When the person hid a conversation from their own list (UC-CHT-E07); null when shown. */
  hiddenAt: string | null;
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
  /** The root of the thread a reply belongs to; null for a message of the main flow. */
  readonly threadRootId: string | null;
  /** The reply's place in its thread: 1 for the first reply, gap-free; null outside a thread. */
  readonly threadSequence: number | null;
  /** A thread reply the author also sent to the channel's main flow. */
  readonly alsoSentToChannel: boolean;
  readonly createdAt: string;
  editedAt: string | null;
  deletedAt: string | null;
  deletedBy: string | null;
  version: number;
}

/** A thread: the replies of one root message (backend MessageThread, keyed by the root). */
export interface ChatThreadRecord {
  readonly rootMessageId: string;
  readonly workspaceId: string;
  readonly spaceId: string;
  /** Replies stored so far, which is also the sequence of the last reply. */
  replyCount: number;
  lastReplyAt: string;
}

/** A person's follow state on a thread (backend ThreadFollower, keyed by root and person). */
export interface ChatThreadFollowerRecord {
  readonly rootMessageId: string;
  readonly workspaceId: string;
  readonly userId: string;
  muted: boolean;
  /** The highest thread sequence the person has read; replies above it are unread. */
  readReplySequence: number;
}

/** A message pinned in its space (backend Pin): at most one per message. */
export interface ChatPinRecord {
  readonly id: string;
  readonly workspaceId: string;
  readonly spaceId: string;
  readonly messageId: string;
  readonly pinnedBy: string;
  readonly pinnedAt: string;
}

/**
 * A person's reaction of one palette kind to a message (backend Reaction): one per message, person and kind. A tombstone keeps
 * none; the delete of the message removes them.
 */
export interface ChatReactionRecord {
  readonly id: string;
  readonly workspaceId: string;
  readonly spaceId: string;
  readonly messageId: string;
  readonly userId: string;
  readonly kind: string;
  readonly createdAt: string;
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
  /** Pins a space holds at most (backend ChatLimits.PinLimit). */
  pinLimit: 50,
  /** People a group conversation holds at most, the caller included (backend ChatLimits.GroupCap). */
  groupCap: 8,
  /** Distinct reaction kinds one message holds at most (backend ChatLimits.ReactionKindsPerMessageMax). */
  reactionKindsPerMessageMax: 6,
  /** Reactions one person holds on one message at most (backend ChatLimits.ReactionsPerUserPerMessageMax). */
  reactionsPerUserPerMessageMax: 6,
  /** Days without a reply after which a thread is archived (backend ChatLimits.ThreadInactivityDays). */
  threadInactivityDays: 7,
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
  limitReached: "limit-reached",
  messageNotPinnable: "message-not-pinnable",
  messageNotReactable: "message-not-reactable",
  threadRootInvalid: "thread-root-invalid",
  /** The errorCode of a 400 business rule the backend does not map by name; the rule itself travels as ruleCode. */
  businessRule: "business_rule.violation",
} as const;

/** Rule codes of the Chat conversations (ChatRuleCodes) that answer 400 with the generic business-rule problem. */
export const CHAT_RULE = {
  selfTarget: "self-target",
  groupTooSmall: "group-too-small",
  kindNotSupported: "kind-not-supported",
} as const;

/**
 * The reaction palette (backend Domain ReactionKinds, the Collaboration palette, COL-009): the only kinds a person may react
 * with. Any other kind is a 400.
 */
export const CHAT_REACTION_PALETTE: readonly string[] = [
  "👍",
  "❤️",
  "😄",
  "🎉",
  "👀",
  "🚀",
];

/** The announcements the backend writes as system messages (ChatSystemMessages). */
export const CHAT_SYSTEM_TEXT = {
  memberJoined: "joined the channel",
  memberLeft: "left the channel",
  memberRemoved: "was removed from the channel",
  memberAdded: "added a member to the channel",
  archived: "archived the channel",
  unarchived: "unarchived the channel",
  renamed: (name: string) => `renamed the channel to ${name}`,
  pinned: "pinned a message",
} as const;
