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
  /** The longest slow mode in seconds (ChatLimits.SlowModeMaxSeconds). */
  slowModeMaxSeconds: 3600,
  /** The reason of a moderator delete (ChatLimits.ModerationReasonMaxLength). */
  moderationReasonMaxLength: 500,
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

/** The roles a channel permission override names (managers, members and guests; Owner and Admin are never subjects). */
export type ChatPermissionSubject = "manager" | "member" | "guest";
/** The channel actions an override can change. */
export type ChatChannelAction =
  "post" | "react" | "attach" | "pin" | "broadcast" | "invite";
export type ChatPermissionEffect = "allow" | "deny";

export const CHAT_PERMISSION_SUBJECTS: readonly ChatPermissionSubject[] = [
  "manager",
  "member",
  "guest",
];
export const CHAT_CHANNEL_ACTIONS: readonly ChatChannelAction[] = [
  "post",
  "react",
  "attach",
  "pin",
  "broadcast",
  "invite",
];

/** One stored override (ChannelPermissionEntryDto); a pair with no entry is on its default. */
export interface ChatPermissionEntry {
  readonly subject: ChatPermissionSubject;
  readonly action: ChatChannelAction;
  readonly effect: ChatPermissionEffect;
}

/** ChannelPermissionsDto: the whole set of a channel and its version. */
export interface ChatChannelPermissions {
  readonly channelId: string;
  readonly entries: readonly ChatPermissionEntry[];
  readonly version: number;
}

/** ChannelCapabilitiesDto: what the caller can do in the channel, resolved by the server. */
export interface ChatChannelCapabilities {
  readonly canPost: boolean;
  readonly canReact: boolean;
  readonly canAttach: boolean;
  readonly canPin: boolean;
  readonly canBroadcast: boolean;
  readonly canInvite: boolean;
  readonly canModerate: boolean;
  readonly canManagePermissions: boolean;
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
  /** The least seconds between two messages of one person (UC-CHT-H08); 0 or absent is off. */
  readonly slowModeSeconds?: number;
  /** What the caller can do here; absent from older servers (the UI then keeps today's behaviour). */
  readonly capabilities?: ChatChannelCapabilities;
  /** The Board, Page or Team this channel is linked to (UC-CHT-H10); absent or null when it is not linked. */
  readonly link?: ChatChannelLink | null;
}

/**
 * ChannelLinkDto: where a channel is discussed from. `title` is there only when the viewer can view the resource now;
 * `accessible` false is the generic chip (hidden, deleted and missing look the same).
 */
export interface ChatChannelLink {
  readonly kind: ChatLinkKind;
  readonly resourceId: string;
  readonly accessible: boolean;
  readonly title: string | null;
}

/** LinkedChannelDto: one channel linked to a resource, as the reverse lookup lists it (only channels the caller can see). */
export interface ChatLinkedChannel {
  readonly id: string;
  readonly name: string;
  readonly kind: ChatChannelKind;
  readonly isMember: boolean;
  readonly isArchived: boolean;
}

/** The body of `PUT .../channels/{id}/link`. */
export interface ChatLinkChannelInput {
  readonly kind: ChatLinkKind;
  readonly resourceId: string;
  readonly expectedVersion: number;
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

/** MessageAttachmentDto: a file of a message. `downloadPath` is the permission-checked content route; width and height are an image's pixel size. */
export interface ChatAttachment {
  readonly id: string;
  readonly messageId?: string;
  readonly fileName: string;
  readonly sizeBytes: number;
  readonly contentType: string;
  readonly uploadedBy: string;
  readonly createdAt: string;
  readonly downloadPath: string;
  /** The pixel size of an image file, read from its header at upload; absent or null for any other file. */
  readonly width?: number | null;
  readonly height?: number | null;
  /** Voice messages only (UC-CHT-I04): the length in milliseconds, from the header or the recorder's hint. */
  readonly durationMs?: number | null;
  /** Voice messages only: up to 64 peaks, each 0 to 255. */
  readonly waveform?: readonly number[] | null;
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
  /** For a thread root: the replies stored so far (0 for a message with no thread). Never decreases: a deleted reply stays counted. */
  readonly threadReplyCount?: number;
  /** For a thread root: the time of the last reply; null when there is none. */
  readonly threadLastReplyAt?: string | null;
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
  /** A tombstone left by a moderator rather than the author (UC-CHT-H03). */
  readonly removedByModerator?: boolean;
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

/** The kinds of resource a message link can point to (the ResourceRef kind of the producer). */
export type ChatLinkKind =
  | "work-management.board-item"
  | "work-management.board"
  | "documents.page"
  | "governance.team";

/** What names a link to resolve: a product URL of this Workspace, or its ResourceRef. */
export type ChatLinkRef =
  | { readonly url: string }
  | { readonly kind: ChatLinkKind; readonly resourceId: string };

/**
 * LinkPreviewDto: the preview of a link to a Board item or Page. `title` is there only when the caller can view the
 * resource now; `accessible` false is a generic link (hidden, trashed and missing look the same).
 */
export interface ChatLinkPreview {
  readonly kind: ChatLinkKind;
  readonly resourceId: string;
  readonly accessible: boolean;
  readonly title: string | null;
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
  /** 0 to 3600; 0 turns the slow mode off and leaving it out keeps it. */
  readonly slowModeSeconds?: number;
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

/** ChannelGroupDto: a sidebar category with its name (`Chat.ChannelGroups.List`). */
export interface ChatChannelGroup {
  readonly id: string;
  readonly name: string;
  readonly position: number;
  readonly version: number;
}

/** BlockedPersonDto: one person the caller blocked (`Chat.Blocks.List`; the caller's own list only). */
export interface ChatBlockedPerson {
  readonly userId: string;
  readonly blockedAt: string;
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

/** The two things retention does to a message past the window (UC-CHT-H05). */
export const CHAT_RETENTION_MODES = ["tombstone", "purge"] as const;
export type ChatRetentionMode = (typeof CHAT_RETENTION_MODES)[number];

/** The window the backend accepts (ChatLimits.RetentionMinDays / RetentionMaxDays). */
export const CHAT_RETENTION_DAYS = { min: 7, max: 3650 } as const;

/** RetentionPolicyDto: the Workspace rule. `retentionDays` null is no retention; `canChange` is true for the Owner only. */
export interface ChatRetentionPolicy {
  readonly retentionDays: number | null;
  readonly mode: ChatRetentionMode;
  readonly version: number;
  readonly updatedAt: string | null;
  readonly canChange: boolean;
}

export const CHAT_EXPORT_STATUSES = [
  "queued",
  "running",
  "ready",
  "failed",
  "expired",
] as const;
export type ChatExportStatus = (typeof CHAT_EXPORT_STATUSES)[number];

/** ChatExportDto: counts only, no names and no content. `downloadPath` is set only while the file can be downloaded. */
export interface ChatExport {
  readonly id: string;
  readonly subjectUserId: string;
  readonly rangeFrom: string | null;
  readonly rangeTo: string | null;
  readonly status: ChatExportStatus;
  readonly messageCount: number;
  readonly spaceCount: number;
  readonly excludedSpaceCount: number;
  readonly tombstoneCount: number;
  readonly sizeBytes: number;
  readonly failureCode: string | null;
  readonly createdAt: string;
  readonly completedAt: string | null;
  readonly expiresAt: string | null;
  readonly downloadPath: string | null;
}

// ─── Search (UC-CHT-H07) ─────────────────────────────────────────────────────

/** A matched stretch of a snippet, in UTF-16 code units of the snippet text. */
export interface ChatSearchRange {
  readonly start: number;
  readonly length: number;
}

/** ChatSearchSnippetDto: plain text and the ranges to highlight; never markup. */
export interface ChatSearchSnippet {
  readonly text: string;
  readonly ranges: readonly ChatSearchRange[];
  readonly cutBefore: boolean;
  readonly cutAfter: boolean;
}

/** ChatSearchHitDto: one message hit and where to jump to. */
export interface ChatSearchHit {
  readonly messageId: string;
  readonly spaceId: string;
  /** The channel or group name; empty for a direct conversation (name it by its people). */
  readonly spaceName: string;
  readonly spaceKind: "channel_public" | "channel_private" | "direct" | "group";
  readonly spaceArchived: boolean;
  readonly authorId: string;
  readonly createdAt: string;
  readonly sequence: number;
  /** The root of the thread when the hit is a thread reply. */
  readonly threadRootId: string | null;
  readonly alsoSentToChannel: boolean;
  readonly snippet: ChatSearchSnippet;
}

/** ChatSearchPageDto: newest first; `nextCursor` is null on the last page. There is no total. */
export interface ChatSearchPage {
  readonly items: readonly ChatSearchHit[];
  readonly nextCursor: string | null;
}

/** The query and filters of a search. Dates are ISO instants; `from` is inclusive and `to` exclusive. */
export interface ChatSearchFilters {
  readonly q: string;
  readonly spaceId?: string;
  readonly authorId?: string;
  readonly from?: string;
  readonly to?: string;
  readonly hasAttachment?: boolean;
  readonly mentionsMe?: boolean;
}

export const CHAT_SEARCH_LIMITS = {
  /** The least and most characters of the typed text (the server answers 400 outside it). */
  minLength: 2,
  maxLength: 100,
  pageSize: 20,
} as const;
