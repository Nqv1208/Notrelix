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
import { announceMessageCreated, announcePinChanged } from "./chat-realtime";
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
    threadRootId: null,
    threadSequence: null,
    alsoSentToChannel: false,
  });
  announceMessageCreated(state, space, message);
  return message;
}

/**
 * Stores a reply in the thread of a root message (UC-CHT-F01). The reply takes the next space sequence and the next
 * thread sequence; the thread row is opened by the first reply and moves on each reply. The root's author follows
 * the thread by default when they still belong to the space, and the replier follows it and has read their reply.
 */
export function appendThreadReply(
  state: MockChatState,
  space: ChatSpaceRecord,
  root: ChatMessageRecord,
  authorId: string,
  content: string,
  clientMessageId: string,
  alsoSendToChannel: boolean,
): ChatMessageRecord {
  const thread = state.threads.get(root.id);
  const threadSequence = (thread?.replyCount ?? 0) + 1;
  const message = storeMessage(state, space, {
    authorId,
    kind: "user",
    content,
    clientMessageId,
    replyToId: null,
    threadRootId: root.id,
    threadSequence,
    alsoSentToChannel: alsoSendToChannel,
  });
  const record = thread ?? {
    rootMessageId: root.id,
    workspaceId: space.workspaceId,
    spaceId: space.id,
    replyCount: 0,
    lastReplyAt: message.createdAt,
  };
  record.replyCount = threadSequence;
  record.lastReplyAt = message.createdAt;
  state.threads.set(root.id, record);
  if (
    root.authorId !== authorId &&
    state.activeMembershipOf(space.id, root.authorId)
  ) {
    ensureThreadFollower(state, root.id, space.workspaceId, root.authorId, 0);
  }
  ensureThreadFollower(
    state,
    root.id,
    space.workspaceId,
    authorId,
    threadSequence,
  );
  announceMessageCreated(state, space, message);
  return message;
}

/**
 * A default follow (not muted) when the person has no follow row yet; an existing row is never overwritten, and its
 * read pointer only moves forward (ThreadFollower.MarkReadThrough).
 */
export function ensureThreadFollower(
  state: MockChatState,
  rootMessageId: string,
  workspaceId: string,
  userId: string,
  readReplySequence: number,
): void {
  const existing = state.threadFollowerOf(rootMessageId, userId);
  if (!existing) {
    state.threadFollowers.set(`${rootMessageId}|${userId}`, {
      rootMessageId,
      workspaceId,
      userId,
      muted: false,
      readReplySequence,
    });
    return;
  }
  existing.readReplySequence = Math.max(
    existing.readReplySequence,
    readReplySequence,
  );
}

/** Removes the pin of a message and announces the end on the channel frame (UC-CHT-F07). False when it had none. */
export function removePin(
  state: MockChatState,
  space: ChatSpaceRecord,
  messageId: string,
): boolean {
  if (!state.pins.delete(messageId)) return false;
  announcePinChanged(state, space, messageId, "unpinned");
  return true;
}

/**
 * Clears the reactions of a message whose content is deleted (backend DeleteMessage). No reaction frame: the message's delete fact
 * already tells the readers, and the tombstone carries no reactions.
 */
export function removeReactionsOf(state: MockChatState, messageId: string): void {
  for (const row of state.reactionsOfMessage(messageId)) {
    state.reactions.delete(state.reactionKey(row.messageId, row.userId, row.kind));
  }
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
    threadRootId: null,
    threadSequence: null,
    alsoSentToChannel: false,
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
    threadRootId: string | null;
    threadSequence: number | null;
    alsoSentToChannel: boolean;
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
    threadRootId: fields.threadRootId,
    threadSequence: fields.threadSequence,
    alsoSentToChannel: fields.alsoSentToChannel,
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
