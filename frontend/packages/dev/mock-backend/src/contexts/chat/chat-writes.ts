/**
 * The writes every Chat handler shares: a user message or a system message gets
 * the next sequence of its space (gap-free, the space row holds the counter),
 * is announced on the realtime frames, and a create that carries an
 * Idempotency-Key is answered from its first reply when it is replayed.
 */

import type { MockHttpResult } from "../../transport/create-response";
import type { MockChatState } from "./chat-state";
import {
  CHAT_ERROR,
  type ChatMessageRecord,
  type ChatSpaceRecord,
} from "./chat-model";
import { announceMessageCreated } from "./chat-realtime";
import { fail } from "./chat-access";

/** Stores a user message: the sequence is the space's next one, the space row moves with it. */
export function appendUserMessage(
  state: MockChatState,
  space: ChatSpaceRecord,
  authorId: string,
  content: string,
  clientMessageId: string,
  replyToId: string | null,
): ChatMessageRecord {
  const message = storeMessage(state, space, {
    authorId,
    kind: "user",
    content,
    clientMessageId,
    replyToId,
  });
  announceMessageCreated(state, space, message);
  return message;
}

/** Stores the system message that announces a change, attributed to the person who made it. */
export function appendSystemMessage(
  state: MockChatState,
  space: ChatSpaceRecord,
  actorId: string,
  content: string,
): ChatMessageRecord {
  const message = storeMessage(state, space, {
    authorId: actorId,
    kind: "system",
    content,
    clientMessageId: null,
    replyToId: null,
  });
  announceMessageCreated(state, space, message);
  return message;
}

function storeMessage(
  state: MockChatState,
  space: ChatSpaceRecord,
  fields: {
    authorId: string;
    kind: "user" | "system";
    content: string;
    clientMessageId: string | null;
    replyToId: string | null;
  },
): ChatMessageRecord {
  space.lastSequence += 1;
  const message: ChatMessageRecord = {
    id: state.nextId("message"),
    workspaceId: space.workspaceId,
    spaceId: space.id,
    sequence: space.lastSequence,
    authorId: fields.authorId,
    kind: fields.kind,
    content: fields.content,
    clientMessageId: fields.clientMessageId,
    replyToId: fields.replyToId,
    createdAt: state.stampWrite(),
    editedAt: null,
    deletedAt: null,
    deletedBy: null,
    version: 1,
  };
  state.appendMessage(message);
  return message;
}

/**
 * A create with an Idempotency-Key: the first reply is stored under the scope
 * and key; a replay with the same payload gets it back, a different payload is 409.
 */
export function idempotentCreate(
  state: MockChatState,
  scope: string,
  key: string,
  payload: unknown,
  create: () => { status: number; body: unknown },
): MockHttpResult<unknown> {
  const fingerprint = JSON.stringify(payload);
  const slot = `${scope}|${key}`;
  const stored = state.idempotentReplies.get(slot);
  if (stored) {
    if (stored.fingerprint !== fingerprint) {
      fail(
        409,
        CHAT_ERROR.idempotencyPayloadMismatch,
        "Idempotency key reused",
        "The Idempotency-Key was used with a different payload.",
      );
    }
    return { status: stored.status, body: stored.body };
  }
  const reply = create();
  state.idempotentReplies.set(slot, {
    fingerprint,
    status: reply.status,
    body: reply.body,
  });
  return reply;
}

/**
 * Dev only (browser checks of the Chat realtime path): another member writes a
 * message in a space, as if from their own device. It is stored and announced
 * like any user message (the channel frame, then the unread frames). Null when
 * the space is unknown. Exposed as `globalThis.__notrelixMockInjectChat`; the mock never ships it.
 */
export function injectIncomingMessage(
  state: MockChatState,
  spaceId: string,
  authorId: string,
  content: string,
): ChatMessageRecord | null {
  const space = state.spaces.get(spaceId);
  if (!space) return null;
  return appendUserMessage(
    state,
    space,
    authorId,
    content,
    `injected-${state.nextId("message")}`,
    null,
  );
}
