/**
 * Reactions of a message: react with a palette emoji (idempotent, 201 new or 200 repeat), remove the caller's reaction (idempotent),
 * and list who reacted (UC-CHT-G06, G07). The reaction summary (emoji, count, reactedByMe) travels with every message view; the
 * change answers with the message's summary. A system or deleted message takes no reaction; an archived channel is read-only.
 */

import { defineMockOperation } from "../../operations/types";
import { ok } from "../../transport/create-response";
import {
  archivedProblem,
  callerIn,
  fail,
  guarded,
  liveSpace,
  notFoundProblem,
  optionalInteger,
  reactionSummaryOf,
  requireAction,
  requireVisible,
  validationProblem,
} from "./chat-access";
import { requireChannelAction } from "./chat-permissions";
import {
  CHAT_ERROR,
  CHAT_LIMITS,
  CHAT_REACTION_PALETTE,
  type ChatReactionRecord,
} from "./chat-model";
import { announceReactionChanged } from "./chat-realtime";
import { streamIdOf } from "./chat-state";
import type { MockChatState } from "./chat-state";

type ReactionParams = {
  workspaceId: string;
  messageId: string;
  kind: string;
};

type ReactorsParams = {
  workspaceId: string;
  messageId: string;
};

/** The kind arrives percent-encoded in the path (an emoji); the router does not decode it. */
function kindOf(raw: string): string {
  let kind: string;
  try {
    kind = decodeURIComponent(raw);
  } catch {
    kind = raw;
  }
  if (!CHAT_REACTION_PALETTE.includes(kind)) {
    validationProblem("The emoji is not in the reaction palette.");
  }
  return kind;
}

/** The cursor of ListReactors: the reaction time and the reaction id of the last item, as `<epoch ms>.<id N form>`. */
function reactorCursorOf(row: ChatReactionRecord): string {
  return `${Date.parse(row.createdAt)}.${streamIdOf(row.id)}`;
}

function parseReactorCursor(
  value: string | undefined,
): { ms: number; id: string } | null {
  if (value === undefined || value === "") return null;
  const [ms, id, extra] = value.split(".");
  const parsed = Number(ms);
  if (
    ms === undefined ||
    id === undefined ||
    extra !== undefined ||
    !Number.isInteger(parsed) ||
    !/^[0-9a-f]{32}$/i.test(id)
  ) {
    validationProblem("cursor is not valid.");
  }
  return { ms: parsed, id: id.toLowerCase() };
}

/** The message a reaction targets, read the way the backend reads it: in the Workspace, else the same NotFound. */
function messageIn(
  state: MockChatState,
  workspaceId: string,
  messageId: string,
) {
  const message = state.messages.get(messageId);
  if (!message || message.workspaceId !== workspaceId) notFoundProblem();
  return message;
}

/** The change answer: the message's reaction summary as the caller now sees it. */
function summaryBody(
  state: MockChatState,
  messageId: string,
  viewerUserId: string,
) {
  return {
    messageId,
    reactions: reactionSummaryOf(state, messageId, viewerUserId),
  };
}

export const reactionsOperations = [
  // ─── PUT /messages/:messageId/reactions/:kind ─────────────────────────────

  defineMockOperation<ReactionParams>({
    id: "chat.reactions.add",
    contract: { kind: "openapi", operationId: "Chat.Reactions.Add" },
    method: "PUT",
    route: "/workspaces/:workspaceId/chat/messages/:messageId/reactions/:kind",
    async handle({ params, store }) {
      return guarded(async () => {
        const state = store.chat;
        const caller = callerIn(store, params.workspaceId);
        requireAction(caller, "messageReact");
        const kind = kindOf(params.kind);
        const message = messageIn(state, params.workspaceId, params.messageId);
        const space = liveSpace(state, params.workspaceId, message.spaceId);
        if (!state.activeMembershipOf(space.id, caller.userId)) {
          notFoundProblem();
        }
        if (space.archivedAt !== null) archivedProblem();
        requireChannelAction(state, space, caller, "react");
        if (message.kind === "system") {
          fail(
            409,
            CHAT_ERROR.messageNotReactable,
            "Message not reactable",
            "A system message cannot be reacted to.",
          );
        }
        if (message.deletedAt !== null) {
          fail(
            409,
            CHAT_ERROR.messageDeleted,
            "Message deleted",
            "A deleted message cannot be reacted to.",
          );
        }
        // A reaction the caller already has answers with the summary and changes nothing.
        const key = state.reactionKey(message.id, caller.userId, kind);
        if (state.reactions.has(key)) {
          return ok(summaryBody(state, message.id, caller.userId));
        }
        const rows = state.reactionsOfMessage(message.id);
        const distinctKinds = new Set(rows.map((row) => row.kind)).size;
        const kindIsNew = !rows.some((row) => row.kind === kind);
        const byCaller = rows.filter(
          (row) => row.userId === caller.userId,
        ).length;
        if (
          kindIsNew &&
          distinctKinds >= CHAT_LIMITS.reactionKindsPerMessageMax
        ) {
          fail(
            422,
            CHAT_ERROR.limitReached,
            "Limit reached",
            `A message holds at most ${CHAT_LIMITS.reactionKindsPerMessageMax} distinct reactions.`,
          );
        }
        if (byCaller >= CHAT_LIMITS.reactionsPerUserPerMessageMax) {
          fail(
            422,
            CHAT_ERROR.limitReached,
            "Limit reached",
            `A person holds at most ${CHAT_LIMITS.reactionsPerUserPerMessageMax} reactions on one message.`,
          );
        }
        const row: ChatReactionRecord = {
          id: state.nextId("reaction"),
          workspaceId: params.workspaceId,
          spaceId: space.id,
          messageId: message.id,
          userId: caller.userId,
          kind,
          createdAt: state.stampWrite(),
        };
        state.reactions.set(key, row);
        announceReactionChanged(
          state,
          space,
          message.id,
          kind,
          "added",
          caller.userId,
        );
        return {
          status: 201,
          body: summaryBody(state, message.id, caller.userId),
        };
      });
    },
  }),

  // ─── DELETE /messages/:messageId/reactions/:kind ──────────────────────────

  defineMockOperation<ReactionParams>({
    id: "chat.reactions.remove",
    contract: { kind: "openapi", operationId: "Chat.Reactions.Remove" },
    method: "DELETE",
    route: "/workspaces/:workspaceId/chat/messages/:messageId/reactions/:kind",
    async handle({ params, store }) {
      return guarded(async () => {
        const state = store.chat;
        const caller = callerIn(store, params.workspaceId);
        requireAction(caller, "messageReact");
        const kind = kindOf(params.kind);
        const message = messageIn(state, params.workspaceId, params.messageId);
        const space = liveSpace(state, params.workspaceId, message.spaceId);
        if (!state.activeMembershipOf(space.id, caller.userId)) {
          notFoundProblem();
        }
        if (space.archivedAt !== null) archivedProblem();
        const key = state.reactionKey(message.id, caller.userId, kind);
        // A reaction the caller does not have is a success that changes nothing.
        if (state.reactions.delete(key)) {
          announceReactionChanged(
            state,
            space,
            message.id,
            kind,
            "removed",
            caller.userId,
          );
        }
        return ok(summaryBody(state, message.id, caller.userId));
      });
    },
  }),

  // ─── GET /messages/:messageId/reactions ───────────────────────────────────

  defineMockOperation<ReactorsParams>({
    id: "chat.reactions.listReactors",
    contract: { kind: "openapi", operationId: "Chat.Reactions.ListReactors" },
    method: "GET",
    route: "/workspaces/:workspaceId/chat/messages/:messageId/reactions",
    async handle({ params, query, store }) {
      return guarded(async () => {
        const state = store.chat;
        const caller = callerIn(store, params.workspaceId);
        requireAction(caller, "viewWorkspace");
        const limit =
          optionalInteger(query.limit, "limit") ?? CHAT_LIMITS.pageSizeDefault;
        if (limit < 1 || limit > CHAT_LIMITS.pageSizeMax) {
          validationProblem(
            `limit must be between 1 and ${CHAT_LIMITS.pageSizeMax}.`,
          );
        }
        const kind = query.kind === undefined ? null : kindOf(query.kind);
        const cursor = parseReactorCursor(query.cursor);
        const message = messageIn(state, params.workspaceId, params.messageId);
        const space = liveSpace(state, params.workspaceId, message.spaceId);
        requireVisible(state, space, caller);
        const rows = state
          .reactionsOfMessage(message.id)
          .filter((row) => kind === null || row.kind === kind)
          .filter(
            (row) =>
              cursor === null ||
              Date.parse(row.createdAt) > cursor.ms ||
              (Date.parse(row.createdAt) === cursor.ms &&
                streamIdOf(row.id) > cursor.id),
          )
          .slice(0, limit + 1);
        const page = rows.slice(0, limit);
        const last = page[page.length - 1];
        return ok({
          items: page.map((row) => ({
            emoji: row.kind,
            userId: row.userId,
            reactedAt: row.createdAt,
          })),
          nextCursor:
            rows.length > limit && last ? reactorCursorOf(last) : null,
        });
      });
    },
  }),
];
