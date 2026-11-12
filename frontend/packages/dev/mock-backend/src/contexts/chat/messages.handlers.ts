/**
 * Messages of a space: send (idempotent by clientMessageId), read by cursor
 * (before, after, around), edit and delete (UC-CHT-B01 to B05). A sequence is
 * never reused; a tombstone keeps its sequence; a system message is immutable.
 */

import { defineMockOperation } from "../../operations/types";
import { ok } from "../../transport/create-response";
import {
  archivedProblem,
  callerIn,
  forbiddenProblem,
  guarded,
  liveSpace,
  messageView,
  notFoundProblem,
  optionalInteger,
  parseVersion,
  requireAction,
  requireIdempotencyKey,
  requireVersion,
  requireVisible,
  requiredText,
  validationProblem,
  fail,
  type ChatCaller,
} from "./chat-access";
import { CHAT_ERROR, CHAT_LIMITS, type ChatMessageRecord } from "./chat-model";
import {
  appendUserMessage,
  removeAttachmentsOf,
  removeMentionsOf,
  removePin,
  removeReactionsOf,
} from "./chat-writes";
import {
  resolveMentions,
  storedWarnings,
  storeMentions,
  type MentionInputBody,
} from "./chat-mentions";
import { announceMessageEvent } from "./chat-realtime";
import type { MockChatState } from "./chat-state";

type SpaceParams = {
  workspaceId: string;
  spaceId: string;
};

type MessageParams = {
  workspaceId: string;
  messageId: string;
};

interface SendBody {
  content?: string | null;
  clientMessageId?: string | null;
  replyToId?: string | null;
  mentions?: MentionInputBody[] | null;
  confirmBroadcast?: boolean | null;
}

interface EditBody {
  content?: string | null;
  expectedVersion?: number | null;
  mentions?: MentionInputBody[] | null;
  confirmBroadcast?: boolean | null;
}

/** One page, as ListMessages cuts it: ascending by sequence, with the two flags. */
export function messagePage(
  messages: readonly ChatMessageRecord[],
  cursor: { before?: number; after?: number; around?: number },
  limit: number,
) {
  if (cursor.before !== undefined) {
    const desc = messages.filter((m) => m.sequence < cursor.before!).reverse();
    const rows = desc.slice(0, limit + 1);
    return {
      items: rows.slice(0, limit).reverse(),
      hasOlder: rows.length > limit,
      hasNewer: messages.some((m) => m.sequence >= cursor.before!),
    };
  }
  if (cursor.after !== undefined) {
    const asc = messages.filter((m) => m.sequence > cursor.after!);
    const rows = asc.slice(0, limit + 1);
    return {
      items: rows.slice(0, limit),
      hasOlder: messages.some((m) => m.sequence <= cursor.after!),
      hasNewer: rows.length > limit,
    };
  }
  if (cursor.around !== undefined) {
    const lowerTake = Math.max(1, Math.floor(limit / 2));
    const lowerRows = messages
      .filter((m) => m.sequence <= cursor.around!)
      .reverse()
      .slice(0, lowerTake + 1);
    const lower = lowerRows.slice(0, lowerTake).reverse();
    const upperTake = limit - lower.length;
    const upperRows = messages
      .filter((m) => m.sequence > cursor.around!)
      .slice(0, upperTake + 1);
    return {
      items: [...lower, ...upperRows.slice(0, upperTake)],
      hasOlder: lowerRows.length > lowerTake,
      hasNewer: upperRows.length > upperTake,
    };
  }
  // The newest page.
  const rows = [...messages].reverse().slice(0, limit + 1);
  return {
    items: rows.slice(0, limit).reverse(),
    hasOlder: rows.length > limit,
    hasNewer: false,
  };
}

/** The message of the workspace, or the NotFound of a missing one. */
function messageIn(
  state: MockChatState,
  workspaceId: string,
  messageId: string,
): ChatMessageRecord {
  const message = state.messages.get(messageId);
  if (!message || message.workspaceId !== workspaceId) notFoundProblem();
  return message;
}

/** Only the author changes their own message. */
function requireAuthor(
  message: ChatMessageRecord,
  caller: ChatCaller,
  verb: string,
): void {
  if (message.authorId !== caller.userId) {
    forbiddenProblem(`Only the author can ${verb} this message.`);
  }
}

export const messagesOperations = [
  // ─── GET /spaces/:spaceId/messages ────────────────────────────────────────

  defineMockOperation<SpaceParams>({
    id: "chat.messages.list",
    contract: { kind: "openapi", operationId: "Chat.Messages.List" },
    method: "GET",
    route: "/workspaces/:workspaceId/chat/spaces/:spaceId/messages",
    async handle({ params, query, store }) {
      return guarded(async () => {
        const state = store.chat;
        const caller = callerIn(store, params.workspaceId);
        requireAction(caller, "viewWorkspace");
        const before = optionalInteger(query.before, "before");
        const after = optionalInteger(query.after, "after");
        const around = optionalInteger(query.around, "around");
        const limit =
          optionalInteger(query.limit, "limit") ?? CHAT_LIMITS.pageSizeDefault;
        const cursors = [before, after, around].filter((c) => c !== undefined);
        if (cursors.length > 1)
          validationProblem("Give at most one of before, after and around.");
        if (before !== undefined && before < 1)
          validationProblem("before must be 1 or more.");
        if (after !== undefined && after < 0)
          validationProblem("after must be 0 or more.");
        if (around !== undefined && around < 1)
          validationProblem("around must be 1 or more.");
        if (limit < 1 || limit > CHAT_LIMITS.pageSizeMax) {
          validationProblem(
            `limit must be between 1 and ${CHAT_LIMITS.pageSizeMax}.`,
          );
        }
        const space = liveSpace(state, params.workspaceId, params.spaceId);
        requireVisible(state, space, caller);
        // The channel's main flow: a thread reply shows here only when it was also sent to the channel.
        const mainFlow = state
          .messagesOf(space.id)
          .filter((m) => m.threadRootId === null || m.alsoSentToChannel);
        const page = messagePage(mainFlow, { before, after, around }, limit);
        return ok({
          items: page.items.map((m) => messageView(state, caller.userId, m)),
          hasOlder: page.hasOlder,
          hasNewer: page.hasNewer,
        });
      });
    },
  }),

  // ─── POST /spaces/:spaceId/messages ───────────────────────────────────────

  defineMockOperation<SpaceParams, SendBody>({
    id: "chat.messages.send",
    contract: { kind: "openapi", operationId: "Chat.Messages.Send" },
    method: "POST",
    route: "/workspaces/:workspaceId/chat/spaces/:spaceId/messages",
    async handle({ params, body, request, store }) {
      return guarded(async () => {
        const state = store.chat;
        const caller = callerIn(store, params.workspaceId);
        requireIdempotencyKey(request.headers);
        requireAction(caller, "messagePost");
        const content = requiredText(
          body?.content,
          "content",
          CHAT_LIMITS.messageMaxLength,
        );
        const clientMessageId = requiredText(
          body?.clientMessageId,
          "clientMessageId",
          CHAT_LIMITS.clientMessageIdMaxLength,
        );
        const space = liveSpace(state, params.workspaceId, params.spaceId);
        if (!state.activeMembershipOf(space.id, caller.userId))
          notFoundProblem();
        if (space.archivedAt !== null) archivedProblem();
        // A retry with the same clientMessageId answers with the stored message and stores nothing.
        const stored = state
          .messagesOf(space.id)
          .find(
            (m) =>
              m.authorId === caller.userId &&
              m.clientMessageId === clientMessageId,
          );
        if (stored) {
          // A retry also repeats the warnings of the stored mentions (UC-CHT-G02).
          return ok(
            messageView(
              state,
              caller.userId,
              stored,
              storedWarnings(
                state,
                space.id,
                state.mentions.get(stored.id) ?? [],
              ),
            ),
          );
        }
        const replyToId =
          typeof body?.replyToId === "string" ? body.replyToId : null;
        if (
          replyToId !== null &&
          state.messages.get(replyToId)?.spaceId !== space.id
        ) {
          notFoundProblem();
        }
        const resolution = resolveMentions(
          state,
          caller,
          space,
          body?.content ?? "",
          body?.mentions,
          body?.confirmBroadcast === true,
        );
        const message = appendUserMessage(
          state,
          space,
          caller.userId,
          content,
          clientMessageId,
          replyToId,
          resolution.spans,
        );
        return {
          status: 201,
          body: messageView(state, caller.userId, message, resolution.notices),
        };
      });
    },
  }),

  // ─── PATCH /messages/:messageId ───────────────────────────────────────────

  defineMockOperation<MessageParams, EditBody>({
    id: "chat.messages.edit",
    contract: { kind: "openapi", operationId: "Chat.Messages.Edit" },
    method: "PATCH",
    route: "/workspaces/:workspaceId/chat/messages/:messageId",
    async handle({ params, body, store }) {
      return guarded(async () => {
        const state = store.chat;
        const caller = callerIn(store, params.workspaceId);
        requireAction(caller, "messagePost");
        const content = requiredText(
          body?.content,
          "content",
          CHAT_LIMITS.messageMaxLength,
        );
        const expected = parseVersion(body?.expectedVersion, "expectedVersion");
        const message = messageIn(state, params.workspaceId, params.messageId);
        const space = liveSpace(state, params.workspaceId, message.spaceId);
        requireVisible(state, space, caller);
        requireAuthor(message, caller, "edit");
        if (space.archivedAt !== null) archivedProblem();
        if (message.kind === "system") {
          fail(
            409,
            CHAT_ERROR.systemMessageImmutable,
            "System message",
            "A system message cannot be edited.",
          );
        }
        if (message.deletedAt !== null) {
          fail(
            409,
            CHAT_ERROR.messageDeleted,
            "Message deleted",
            "A deleted message cannot be edited.",
          );
        }
        const changed = content !== message.content;
        const resolution = resolveMentions(
          state,
          caller,
          space,
          body?.content ?? "",
          body?.mentions,
          body?.confirmBroadcast === true,
        );
        requireVersion(expected, message.version);
        if (changed) {
          message.content = content;
          message.editedAt = state.stampWrite();
          message.version += 1;
          // The new text replaces the mentions: the positions of the old ones no longer fit.
          storeMentions(state, message.id, resolution.spans);
          announceMessageEvent(state, space, message, "chat.message.updated");
          return ok(
            messageView(state, caller.userId, message, resolution.notices),
          );
        }
        return ok(messageView(state, caller.userId, message));
      });
    },
  }),

  // ─── DELETE /messages/:messageId ──────────────────────────────────────────

  defineMockOperation<MessageParams>({
    id: "chat.messages.delete",
    contract: { kind: "openapi", operationId: "Chat.Messages.Delete" },
    method: "DELETE",
    route: "/workspaces/:workspaceId/chat/messages/:messageId",
    async handle({ params, query, store }) {
      return guarded(async () => {
        const state = store.chat;
        const caller = callerIn(store, params.workspaceId);
        requireAction(caller, "messagePost");
        const expected = parseVersion(
          optionalInteger(query.expectedVersion, "expectedVersion"),
          "expectedVersion",
        );
        const message = messageIn(state, params.workspaceId, params.messageId);
        const space = liveSpace(state, params.workspaceId, message.spaceId);
        requireVisible(state, space, caller);
        requireAuthor(message, caller, "delete");
        if (space.archivedAt !== null && message.deletedAt === null)
          archivedProblem();
        if (message.kind === "system") {
          fail(
            409,
            CHAT_ERROR.systemMessageImmutable,
            "System message",
            "A system message cannot be deleted.",
          );
        }
        const alreadyDeleted = message.deletedAt !== null;
        requireVersion(expected, message.version);
        if (!alreadyDeleted) {
          // A tombstone keeps its author, time and sequence; the content goes.
          message.content = null;
          message.deletedAt = state.stampWrite();
          message.deletedBy = caller.userId;
          message.version += 1;
          announceMessageEvent(state, space, message, "chat.message.deleted");
          // A pin ends with the deletion of its message (UC-CHT-F06).
          removePin(state, space, message.id);
          // A tombstone carries no reactions; the delete fact already tells readers (no reaction frame).
          removeReactionsOf(state, message.id);
          // A tombstone carries no files either: the attachments go with the message (UC-CHT-G08).
          removeAttachmentsOf(state, message.id);
          // A tombstone carries no mentions either, so a deleted message never counts as a mention.
          removeMentionsOf(state, message.id);
        }
        return { status: 204 };
      });
    },
  }),
];
