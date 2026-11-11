/**
 * The realtime frames the backend sends for Chat, built as the gateway would
 * deliver them (protocol v1.1) and handed to the mock transports. Nothing here
 * opens a socket: `publishMockScopeFrame` calls the listeners of the mock
 * transports that subscribed the channel or the person.
 *
 * Mirrors ChatRealtime (Application), the per-message consumers (Infrastructure)
 * and ChatUnreadFanOut:
 *   - chat.message.created|updated|deleted on the channel stream (scope Channel,
 *     payloadContract chat.message.v1), for every message fact, system ones too;
 *   - chat.unread.changed on the per-user stream (payloadContract chat.unread.v1)
 *     for each active, non-muted member except the author, when a message is created;
 *   - the same unread frame for the caller's own other devices after a read change.
 */

import type { RealtimeEnvelope } from "@notrelix/realtime";
import type { GeneratedRealtimeMessage } from "@notrelix/contracts";
import { publishMockScopeFrame } from "../../realtime/mock-realtime-transport";
import type { MockChatState } from "./chat-state";
import { streamIdOf } from "./chat-state";
import { mentionCountIn } from "./chat-mention-views";
import {
  CHAT_LIMITS,
  type ChatMessageRecord,
  type ChatSpaceRecord,
} from "./chat-model";
import {
  effectiveLevel,
  unreadIn,
  type ChatSpaceUnreadRow,
} from "./chat-access";

export const CHAT_MESSAGE_CONTRACT = "chat.message.v1";
export const CHAT_UNREAD_CONTRACT = "chat.unread.v1";
export const CHAT_SPACE_CONTRACT = "chat.space.v1";
export const CHAT_REACTION_CONTRACT = "chat.reaction.v1";

export type ChatMessageEventType =
  "chat.message.created" | "chat.message.updated" | "chat.message.deleted";

type MessageEventPayload = Extract<
  GeneratedRealtimeMessage,
  { eventType: ChatMessageEventType }
>["payload"];

type UnreadEventPayload = Extract<
  GeneratedRealtimeMessage,
  { eventType: "chat.unread.changed" }
>["payload"];

/** The compact payload of a message frame, as ChatRealtime.MessageFrame serialises it. */
function messagePayload(
  message: ChatMessageRecord,
  eventType: ChatMessageEventType,
): MessageEventPayload {
  const base = {
    id: message.id,
    sequence: message.sequence,
    authorId: message.authorId,
    kind: message.kind,
    content: message.content,
    contentOmitted: false,
    deleted: message.deletedAt !== null,
    createdAt: message.createdAt,
    editedAt: message.editedAt,
    replyToId: message.replyToId,
    version: message.version,
    threadRootId: message.threadRootId,
    threadSequence: message.threadSequence,
  };
  if (eventType === "chat.message.created") {
    return base;
  }
  if (eventType === "chat.message.updated") {
    // Only a user message is edited, so the kind is fixed; the reply link is not carried.
    return {
      ...base,
      kind: "user",
      createdAt: null,
      replyToId: null,
      deleted: false,
    };
  }
  // A tombstone keeps its id, sequence, author and version; the content is gone.
  return {
    ...base,
    kind: "user",
    content: null,
    contentOmitted: false,
    deleted: true,
    createdAt: null,
    editedAt: null,
    replyToId: null,
  };
}

/** Leaves the content out when the frame would exceed the payload limit (ChatRealtime.Fit). */
function fitMessagePayload(payload: MessageEventPayload): MessageEventPayload {
  if (payload.content === null) return payload;
  const size = new TextEncoder().encode(JSON.stringify(payload)).length;
  if (size <= CHAT_LIMITS.realtimePayloadMaxBytes) return payload;
  return { ...payload, content: null, contentOmitted: true };
}

function channelStreamKey(spaceId: string): string {
  return `chat:Channel:${streamIdOf(spaceId)}`;
}

function userStreamKey(userId: string): string {
  return `user:notifications:${streamIdOf(userId)}`;
}

interface FrameInput {
  readonly state: MockChatState;
  readonly eventType: string;
  readonly subscriptionId: string;
  readonly streamKey: string;
  readonly resourceKind: string;
  readonly resourceId: string;
  readonly workspaceId: string;
  readonly occurredAt: string;
  readonly payloadContract: string;
  readonly payload: Readonly<Record<string, unknown>>;
  readonly resourceVersion?: number;
}

function frame(input: FrameInput): RealtimeEnvelope {
  const eventId = input.state.nextEventId();
  return {
    schemaVersion: 1,
    eventId,
    eventType: input.eventType,
    changeKind: input.eventType,
    subscriptionId: input.subscriptionId,
    streamKey: input.streamKey,
    streamVersion: input.state.nextStreamVersion(input.streamKey),
    resourceKind: input.resourceKind,
    resourceId: input.resourceId,
    aggregateId: input.resourceId,
    ...(input.resourceVersion === undefined
      ? {}
      : { resourceVersion: input.resourceVersion }),
    workspaceId: input.workspaceId,
    correlationId: eventId,
    timestamp: input.occurredAt,
    payloadContract: input.payloadContract,
    payload: input.payload,
  };
}

/** The channel-stream frame of a message fact: created, updated or deleted. */
export function announceMessageEvent(
  state: MockChatState,
  space: ChatSpaceRecord,
  message: ChatMessageRecord,
  eventType: ChatMessageEventType,
): void {
  const raw = messagePayload(message, eventType);
  const payload =
    eventType === "chat.message.deleted" ? raw : fitMessagePayload(raw);
  const occurredAt =
    eventType === "chat.message.created"
      ? message.createdAt
      : eventType === "chat.message.updated"
        ? (message.editedAt ?? message.createdAt)
        : (message.deletedAt ?? message.createdAt);
  publishMockScopeFrame(
    frame({
      state,
      eventType,
      subscriptionId: `channel:${space.id}`,
      streamKey: channelStreamKey(space.id),
      resourceKind: "Channel",
      resourceId: space.id,
      workspaceId: space.workspaceId,
      occurredAt,
      payloadContract: CHAT_MESSAGE_CONTRACT,
      payload: payload as unknown as Readonly<Record<string, unknown>>,
      resourceVersion: message.version,
    }),
  );
}

function unreadFrame(
  state: MockChatState,
  workspaceId: string,
  userId: string,
  spaceId: string,
  unread: number,
  mentions: number,
): void {
  const payload: UnreadEventPayload = { spaceId, unread, mentions };
  publishMockScopeFrame(
    frame({
      state,
      eventType: "chat.unread.changed",
      subscriptionId: `user:${userId}`,
      streamKey: userStreamKey(userId),
      resourceKind: "notifications",
      resourceId: userId,
      workspaceId,
      occurredAt: state.store().getClock().isoNow(),
      payloadContract: CHAT_UNREAD_CONTRACT,
      payload: payload as unknown as Readonly<Record<string, unknown>>,
    }),
  );
}

/**
 * A pin of a space changed (backend ChatPinEventMapper): `pinned` when a message is pinned, `unpinned` when the pin
 * ends by an unpin or by the deletion of the message. Notify-only: the frame carries no content and readers refetch
 * the pins over REST.
 */
export function announcePinChanged(
  state: MockChatState,
  space: ChatSpaceRecord,
  messageId: string,
  change: "pinned" | "unpinned",
): void {
  publishMockScopeFrame(
    frame({
      state,
      eventType: "chat.space.changed",
      subscriptionId: `channel:${space.id}`,
      streamKey: channelStreamKey(space.id),
      resourceKind: "Channel",
      resourceId: space.id,
      workspaceId: space.workspaceId,
      occurredAt: state.store().getClock().isoNow(),
      payloadContract: CHAT_SPACE_CONTRACT,
      payload: { change, messageId } as Readonly<Record<string, unknown>>,
    }),
  );
}

/**
 * A reaction of a message changed (backend ChatReactionEventMapper and ChatRealtime.ForReactionChanged): `added` or `removed`, the
 * message, the palette kind and the person. Compact: no counts and no content; readers apply the change or refetch the summary.
 */
export function announceReactionChanged(
  state: MockChatState,
  space: ChatSpaceRecord,
  messageId: string,
  kind: string,
  change: "added" | "removed",
  actorId: string,
): void {
  publishMockScopeFrame(
    frame({
      state,
      eventType: "chat.reaction.changed",
      subscriptionId: `channel:${space.id}`,
      streamKey: channelStreamKey(space.id),
      resourceKind: "Channel",
      resourceId: space.id,
      workspaceId: space.workspaceId,
      occurredAt: state.store().getClock().isoNow(),
      payloadContract: CHAT_REACTION_CONTRACT,
      payload: { messageId, kind, change, actorId } as Readonly<
        Record<string, unknown>
      >,
    }),
  );
}

/**
 * A message is created: the channel frame first, then the unread frame of each
 * recipient who is active, not the author and not muted (ChatUnreadFanOut).
 */
export function announceMessageCreated(
  state: MockChatState,
  space: ChatSpaceRecord,
  message: ChatMessageRecord,
): void {
  announceMessageEvent(state, space, message, "chat.message.created");
  const now = state.nowDate();
  const recipients = state
    .membershipsOfSpace(space.id)
    .filter((row) => row.leftAt === null && row.userId !== message.authorId)
    .sort((a, b) => (a.userId < b.userId ? -1 : a.userId > b.userId ? 1 : 0))
    .slice(0, CHAT_LIMITS.unreadRecipientsMax);
  for (const row of recipients) {
    if (effectiveLevel(row.notificationLevel, row.mutedUntil, now) === "none") {
      continue;
    }
    unreadFrame(
      state,
      space.workspaceId,
      row.userId,
      space.id,
      unreadIn(state, space.id, row.userId, row.readSequence).count,
      mentionCountIn(
        state,
        space.id,
        row.userId,
        row.readSequence,
        row.joinedAt,
      ),
    );
  }
}

/** The caller's other devices learn the new count of each space that moved (UC-CHT-C09). */
export function announceOwnReadState(
  state: MockChatState,
  workspaceId: string,
  userId: string,
  rows: readonly ChatSpaceUnreadRow[],
): void {
  for (const row of rows) {
    unreadFrame(
      state,
      workspaceId,
      userId,
      row.spaceId,
      row.unreadCount,
      row.mentionCount,
    );
  }
}
