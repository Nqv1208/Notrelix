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
  messageMaxLength: 4000,
  /** The composer shows its counter from this many characters. */
  counterFrom: 3800,
  clientMessageIdMaxLength: 128,
  channelNameMaxLength: 80,
  channelTopicMaxLength: 250,
  pageSize: 50,
  pageSizeMax: 100,
} as const;

export type ChatChannelKind = "channel_public" | "channel_private";
export type ChatMemberRole = "manager" | "member";
export type ChatNotificationLevel = "all" | "mentions" | "none";
export type ChatMessageKind = "user" | "system";

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
  readonly edited: boolean;
  readonly deleted: boolean;
  readonly createdAt: string;
  readonly editedAt: string | null;
  readonly deletedAt: string | null;
  readonly version: number;
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
