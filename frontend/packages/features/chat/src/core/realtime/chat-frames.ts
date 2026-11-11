/**
 * Chat realtime payloads (contract `chat.message.v1` on the channel stream and
 * `chat.unread.v1` on the per-user stream, protocol v1.1). Untrusted input: each
 * parser checks the shape and returns null for anything it does not know, so a
 * malformed frame is ignored and never reaches the cache (FE-RT-017).
 */

export type ChatMessageEventType =
  "chat.message.created" | "chat.message.updated" | "chat.message.deleted";

export const CHAT_UNREAD_EVENT = "chat.unread.changed";

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
  };
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
