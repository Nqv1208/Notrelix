/**
 * Chat realtime payloads (contract `chat.message.v1` on the channel stream and
 * `chat.unread.v1` on the per-user stream, protocol v1.1). Untrusted input: each
 * parser checks the shape and returns null for anything it does not know, so a
 * malformed frame is ignored and never reaches the cache (FE-RT-017).
 */

export type ChatMessageEventType =
  "chat.message.created" | "chat.message.updated" | "chat.message.deleted";

export const CHAT_UNREAD_EVENT = "chat.unread.changed";
export const CHAT_REACTION_EVENT = "chat.reaction.changed";
export const CHAT_SPACE_EVENT = "chat.space.changed";

export function isChatMessageEvent(
  eventType: string,
): eventType is ChatMessageEventType {
  return (
    eventType === "chat.message.created" ||
    eventType === "chat.message.updated" ||
    eventType === "chat.message.deleted"
  );
}

/** The compact message payload (chat.message.v1). `contentOmitted` means the text did not fit the frame. */
export interface ChatMessageFramePayload {
  readonly id: string;
  readonly sequence: number;
  readonly authorId: string;
  readonly kind: "user" | "system";
  readonly content: string | null;
  readonly contentOmitted: boolean;
  readonly deleted: boolean;
  readonly createdAt: string | null;
  readonly editedAt: string | null;
  readonly replyToId: string | null;
  readonly version: number;
  /** Not in chat.message.v1 today; read when present so an own send can be reconciled. */
  readonly clientMessageId: string | null;
  /** The thread root of a reply (null for the main flow). */
  readonly threadRootId: string | null;
  /** The reply's thread sequence (null for the main flow). */
  readonly threadSequence: number | null;
}

/** chat.reaction.v1: one reaction of a message was added or removed (compact: no counts). */
export interface ChatReactionFramePayload {
  readonly messageId: string;
  readonly kind: string;
  readonly change: "added" | "removed";
  readonly actorId: string;
}

/** chat.space.v1: a pin of the space changed; readers refetch the pins. */
export interface ChatSpaceFramePayload {
  readonly messageId: string;
  readonly change: "pinned" | "unpinned";
}

/** The unread payload (chat.unread.v1): the counts of one space for this person. */
export interface ChatUnreadFramePayload {
  readonly spaceId: string;
  readonly unread: number;
  readonly mentions: number;
}

const isRecord = (value: unknown): value is Record<string, unknown> =>
  typeof value === "object" && value !== null && !Array.isArray(value);

const isNonEmpty = (value: unknown): value is string =>
  typeof value === "string" && value.length > 0;

const isNullableString = (value: unknown): value is string | null =>
  value === null || typeof value === "string";

const isCount = (value: unknown): value is number =>
  typeof value === "number" && Number.isInteger(value) && value >= 0;

export function parseChatMessageFrame(
  payload: unknown,
): ChatMessageFramePayload | null {
  if (!isRecord(payload)) return null;
  const { id, sequence, authorId, kind, content, contentOmitted, deleted } =
    payload;
  if (
    !isNonEmpty(id) ||
    !isCount(sequence) ||
    sequence < 1 ||
    !isNonEmpty(authorId) ||
    (kind !== "user" && kind !== "system") ||
    !isNullableString(content) ||
    typeof contentOmitted !== "boolean" ||
    typeof deleted !== "boolean" ||
    !isNullableString(payload.createdAt) ||
    !isNullableString(payload.editedAt) ||
    !isNullableString(payload.replyToId) ||
    !isCount(payload.version) ||
    payload.version < 1
  ) {
    return null;
  }
  return {
    id,
    sequence,
    authorId,
    kind,
    content,
    contentOmitted,
    deleted,
    createdAt: payload.createdAt,
    editedAt: payload.editedAt,
    replyToId: payload.replyToId,
    version: payload.version,
    clientMessageId: isNonEmpty(payload.clientMessageId)
      ? payload.clientMessageId
      : null,
    threadRootId: isNonEmpty(payload.threadRootId)
      ? payload.threadRootId
      : null,
    threadSequence:
      isCount(payload.threadSequence) && payload.threadSequence >= 1
        ? payload.threadSequence
        : null,
  };
}

export function parseChatReactionFrame(
  payload: unknown,
): ChatReactionFramePayload | null {
  if (!isRecord(payload)) return null;
  const { messageId, kind, change, actorId } = payload;
  if (
    !isNonEmpty(messageId) ||
    !isNonEmpty(kind) ||
    !isNonEmpty(actorId) ||
    (change !== "added" && change !== "removed")
  ) {
    return null;
  }
  return { messageId, kind, change, actorId };
}

export function parseChatSpaceFrame(
  payload: unknown,
): ChatSpaceFramePayload | null {
  if (!isRecord(payload)) return null;
  const { messageId, change } = payload;
  if (
    !isNonEmpty(messageId) ||
    (change !== "pinned" && change !== "unpinned")
  ) {
    return null;
  }
  return { messageId, change };
}

export function parseChatUnreadFrame(
  payload: unknown,
): ChatUnreadFramePayload | null {
  if (!isRecord(payload)) return null;
  if (!isNonEmpty(payload.spaceId)) return null;
  if (!isCount(payload.unread) || !isCount(payload.mentions)) return null;
  return {
    spaceId: payload.spaceId,
    unread: payload.unread,
    mentions: payload.mentions,
  };
}
