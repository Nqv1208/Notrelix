/**
 * Chat wire types (Chat.* operations, docs/workstreams/chat-delivery-plan.md S1.7).
 *
 * The generated REST contract types the Chat response bodies as `unknown`, so
 * these DTOs mirror the producer's ChannelDto, MessageDto and read-state
 * records. A channel is a space: `ChatChannel.id` is the `spaceId` of the
 * message and read-state routes.
 */

/** Limits the backend fixes (ChatLimits). The composer counts characters, not bytes. */
export const CHAT_LIMITS = {
  /** A group conversation holds at most this many people, the caller included. */
  groupCap: 8,
  /** A group conversation needs at least this many people, the caller included. */
  groupMin: 3,
  messageMaxLength: 4000,
  /** The composer shows its counter from this many characters. */
  counterFrom: 3800,
  clientMessageIdMaxLength: 128,
  channelNameMaxLength: 80,
  channelTopicMaxLength: 250,
  pageSize: 50,
  pageSizeMax: 100,
  /** The most mentions one message holds (ChatLimits.MentionsPerMessageMax). */
  mentionsPerMessageMax: 50,
  /** Files one message holds at most (ChatLimits / AttachmentUploadOptions.MaxAttachmentsPerMessage). */
  attachmentsPerMessageMax: 10,
  /** The size cap of one file in bytes (AttachmentUploadOptions.MaxFileSizeBytes, 100 MB). */
  attachmentMaxBytes: 100 * 1024 * 1024,
} as const;

export type ChatChannelKind = "channel_public" | "channel_private";
export type ChatMemberRole = "manager" | "member";
export type ChatNotificationLevel = "all" | "mentions" | "none";
export type ChatMessageKind = "user" | "system";

/** The kinds of a mention (backend MentionKind wire spelling). */
export type ChatMentionKind =
  "user" | "channel-link" | "broadcast-channel" | "broadcast-here";

/**
 * MentionDto: one stored mention as a reader sees it. `start` and `length` count UTF-16 characters of
 * `content`; a person is `userId`, a channel link is `spaceId`, a broadcast names neither.
 */
export interface ChatMention {
  readonly kind: ChatMentionKind;
  readonly start: number;
  readonly length: number;
  readonly userId: string | null;
  readonly spaceId: string | null;
}

/** What a send or an edit did not do for one mention (MentionNoticeDto); shown to the sender only. */
export type ChatMentionNoticeCode =
  | "cannot-view"
  | "unknown"
  | "link-not-visible"
  | "broadcast-not-allowed"
  | "no-audience";

export interface ChatMentionNotice {
  readonly code: ChatMentionNoticeCode | (string & {});
  readonly start: number;
  readonly length: number;
  readonly userId: string | null;
}

/** MentionableDto: a person a message in the space can mention; `inSpace` false means they will not be notified. */
export interface ChatMentionable {
  readonly userId: string;
  readonly inSpace: boolean;
}

/** MentionablesDto: the picker of one space. */
export interface ChatMentionables {
  readonly people: readonly ChatMentionable[];
  readonly canBroadcast: boolean;
  /** A broadcast in a space of more members than this needs confirmBroadcast. */
  readonly broadcastConfirmAbove: number;
  readonly memberCount: number;
}

/** ChannelDto: the membership fields are the caller's own. */
export interface ChatChannel {
  readonly id: string;
  readonly name: string;
  readonly topic: string | null;
  readonly kind: ChatChannelKind;
  readonly groupId: string | null;
  readonly position: number;
  readonly isArchived: boolean;
  readonly isMember: boolean;
  readonly role: ChatMemberRole | null;
  readonly notificationLevel: ChatNotificationLevel | null;
  readonly version: number;
  readonly createdAt: string;
}

export interface ChatChannelMember {
  readonly userId: string;
  readonly role: ChatMemberRole;
  readonly joinedAt: string;
}

/** `GET /channels/{channelId}`: the channel and its active members, oldest first. */
export interface ChatChannelDetail {
  readonly channel: ChatChannel;
  readonly members: readonly ChatChannelMember[];
}

/** MessageAttachmentDto: a file of a message. `downloadPath` is the permission-checked content route. */
export interface ChatAttachment {
  readonly id: string;
  readonly messageId?: string;
  readonly fileName: string;
  readonly sizeBytes: number;
  readonly contentType: string;
  readonly uploadedBy: string;
  readonly createdAt: string;
  readonly downloadPath: string;
}

/** MessageDto. A tombstone keeps its author, time and sequence, with null content. */
export interface ChatMessage {
  readonly id: string;
  readonly spaceId: string;
  readonly sequence: number;
  readonly kind: ChatMessageKind;
  readonly authorId: string;
  readonly content: string | null;
  readonly clientMessageId: string | null;
  readonly replyToId: string | null;
  /** The root of the thread this reply belongs to; null for a message of the main flow. */
  readonly threadRootId?: string | null;
  /** The reply's place in its thread (gap-free per thread); null outside a thread. */
  readonly threadSequence?: number | null;
  /** A thread reply the author also sent to the channel is in the main flow too. */
  readonly alsoSentToChannel?: boolean;
  /** The live quote of `replyToId`; null when the message quotes nothing. */
  readonly replyPreview?: ChatReplyPreview | null;
  /** The reaction summary, one entry per palette emoji that has reactions. */
  readonly reactions?: readonly ChatReactionSummary[];
  /** The stored mentions of `content` the reader may see (a channel link the reader cannot see is left out). */
  readonly mentions?: readonly ChatMention[];
  /** What the send or edit that returned this message did not do for the sender; empty on every other read. */
  readonly mentionNotices?: readonly ChatMentionNotice[];
  /** The files of the message (UC-CHT-G08); a tombstone has none. */
  readonly attachments?: readonly ChatAttachment[];
  readonly edited: boolean;
  readonly deleted: boolean;
  readonly createdAt: string;
  readonly editedAt: string | null;
  readonly deletedAt: string | null;
  readonly version: number;
}

/** The message a reply quotes, as the caller may see it (a tombstone keeps author and sequence). */
export interface ChatReplyPreview {
  readonly messageId: string;
  readonly sequence: number;
  readonly authorId: string;
  readonly content: string | null;
  readonly deleted: boolean;
}

export interface ChatReactionSummary {
  readonly emoji: string;
  readonly count: number;
  readonly reactedByMe: boolean;
}

export interface ChatMessageReactions {
  readonly messageId: string;
  readonly reactions: readonly ChatReactionSummary[];
}

export interface ChatReactorPage {
  readonly items: readonly {
    emoji: string;
    userId: string;
    reactedAt: string;
  }[];
  readonly nextCursor: string | null;
}

/** ThreadSummaryDto: the reply count and the caller's follow state of one thread. */
export interface ChatThreadSummary {
  readonly rootMessageId: string;
  readonly spaceId: string;
  readonly replyCount: number;
  readonly lastReplyAt: string | null;
  readonly archived: boolean;
  readonly following: boolean;
  readonly muted: boolean;
  readonly unreadReplies: number;
}

/** `GET /messages/{rootId}/replies`: the root, its summary and one page of replies (ascending). */
export interface ChatThreadRepliesPage {
  readonly root: ChatMessage;
  readonly thread: ChatThreadSummary;
  readonly items: readonly ChatMessage[];
  readonly hasNewer: boolean;
}

export interface ChatFollowedThread {
  readonly root: ChatMessage;
  readonly thread: ChatThreadSummary;
}

export interface ChatFollowedThreadPage {
  readonly items: readonly ChatFollowedThread[];
  readonly nextCursor: string | null;
}

export interface ChatPin {
  readonly message: ChatMessage;
  readonly pinnedBy: string;
  readonly pinnedAt: string;
}

export interface ChatPinPage {
  readonly items: readonly ChatPin[];
  readonly nextCursor: string | null;
}

/** One page of messages, ascending by sequence. */
export interface ChatMessagePage {
  readonly items: readonly ChatMessage[];
  readonly hasOlder: boolean;
  readonly hasNewer: boolean;
}

export interface ChatUnreadRow {
  readonly spaceId: string;
  readonly name: string;
  readonly readSequence: number;
  readonly lastSequence: number;
  readonly unreadCount: number;
  readonly firstUnreadSequence: number | null;
  readonly mentionCount: number;
  readonly notificationLevel: ChatNotificationLevel;
  readonly muted: boolean;
}

/** `GET /me/unread`: one row per channel the caller is an active member of. */
export interface ChatUnreadSummary {
  readonly spaces: readonly ChatUnreadRow[];
  readonly totalUnread: number;
  readonly totalMentions: number;
}

export interface ChatReadBoundary {
  readonly spaceId: string;
  readonly readSequence: number;
  readonly lastSequence: number;
}

export interface ChatNotificationState {
  readonly spaceId: string;
  readonly notificationLevel: ChatNotificationLevel;
  readonly muted: boolean;
  readonly mutedUntil: string | null;
}

export interface ChatAddMembersResult {
  readonly added: readonly string[];
  readonly skipped: readonly string[];
}

export interface ChatCreateChannelInput {
  readonly name: string;
  readonly topic?: string | null;
  readonly isPrivate: boolean;
}

export interface ChatUpdateChannelInput {
  readonly name?: string;
  readonly topic?: string | null;
  readonly expectedVersion: number;
}

/** Which page of the history to read: the newest, or a window around a sequence (D03). */
export type ChatHistoryAnchor =
  | { readonly kind: "latest" }
  | { readonly kind: "around"; readonly sequence: number };

export interface ChatMessageCursor {
  readonly before?: number;
  readonly after?: number;
  readonly around?: number;
}

/** The reply thread cursor: the thread sequence of the last reply already held. */
export interface ChatThreadCursor {
  readonly after?: number;
}

/** The reaction palette the server accepts (ReactionKinds.Palette); anything else is refused. */
export const CHAT_REACTION_PALETTE = [
  "👍",
  "❤️",
  "😄",
  "🎉",
  "👀",
  "🚀",
] as const;

export type ChatConversationKind = "direct" | "group";

export interface ChatConversationParticipant {
  readonly userId: string;
  readonly role: ChatMemberRole;
  readonly joinedAt: string;
}

/** The newest message of a conversation as a list preview (text is null for a tombstone). */
export interface ChatConversationPreview {
  readonly sequence: number;
  readonly authorId: string;
  readonly kind: ChatMessageKind;
  readonly text: string | null;
  readonly createdAt: string;
}

/** ConversationDto: a direct or group conversation as the caller reads it. A conversation is a space. */
export interface ChatConversation {
  readonly id: string;
  readonly kind: ChatConversationKind;
  /** The group name; null for a direct conversation and for a group without one. */
  readonly name: string | null;
  readonly participants: readonly ChatConversationParticipant[];
  readonly isHidden: boolean;
  readonly notificationLevel: ChatNotificationLevel;
  readonly unreadCount: number;
  readonly lastMessage: ChatConversationPreview | null;
  readonly lastActivityAt: string;
  readonly version: number;
  readonly createdAt: string;
}

export type ChatStartConversationInput =
  | { readonly kind: "direct"; readonly userId: string }
  | {
      readonly kind: "group";
      readonly userIds: readonly string[];
      readonly name?: string | null;
    };

/** StartConversationResultDto: `created` is false when the direct conversation already existed. */
export interface ChatStartConversationResult {
  readonly conversation: ChatConversation;
  readonly created: boolean;
  readonly skipped: readonly string[];
}

export interface ChatConversationVisibility {
  readonly conversationId: string;
  readonly hidden: boolean;
}

export interface ChatPersonBlock {
  readonly userId: string;
  readonly blocked: boolean;
}

/** MentionRequest: one mention a client sends with a message; positions count UTF-16 characters of the content as sent. */
export interface ChatMentionInput {
  readonly kind: ChatMentionKind;
  readonly start: number;
  readonly length: number;
  readonly userId?: string | null;
  readonly spaceId?: string | null;
}

/** The mentions and the broadcast confirmation that travel with a send, an edit or a thread reply. */
export interface ChatMentionPayload {
  readonly mentions?: readonly ChatMentionInput[];
  readonly confirmBroadcast?: boolean;
}
