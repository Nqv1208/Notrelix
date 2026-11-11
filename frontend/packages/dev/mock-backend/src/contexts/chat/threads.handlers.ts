/**
 * Threads: reply in the thread of a message (idempotent by Idempotency-Key and
 * clientMessageId), read its replies, follow or mute it, move the read pointer
 * of a followed thread, and list the threads the caller follows (UC-CHT-F01 to
 * F04, F08). A thread is keyed by its root message; a reply takes the next
 * thread sequence and the next space sequence, both gap-free.
 */

import { defineMockOperation } from "../../operations/types";
import { ok } from "../../transport/create-response";
import {
  archivedProblem,
  callerIn,
  fail,
  guarded,
  liveSpace,
  messageView,
  notFoundProblem,
  optionalInteger,
  requireAction,
  requireIdempotencyKey,
  requireVisible,
  requiredText,
  threadSummaryView,
  validationProblem,
} from "./chat-access";
import { CHAT_ERROR, CHAT_LIMITS } from "./chat-model";
import { appendThreadReply, idempotentCreate } from "./chat-writes";
import {
  resolveMentions,
  storedWarnings,
  type MentionInputBody,
} from "./chat-mentions";

type RootParams = {
  workspaceId: string;
  rootId: string;
};

type ThreadParams = {
  workspaceId: string;
  threadId: string;
};

interface ReplyBody {
  content?: string | null;
  clientMessageId?: string | null;
  alsoSendToChannel?: boolean | null;
  mentions?: MentionInputBody[] | null;
  confirmBroadcast?: boolean | null;
}

interface FollowBody {
  following?: boolean | null;
  muted?: boolean | null;
}

interface MarkReadBody {
  upToReplySequence?: number | null;
}

/** The cursor of ListFollowedThreads: the last reply time of the previous page, as epoch milliseconds (opaque to clients). */
function parseThreadsCursor(value: string | undefined): number | null {
  if (value === undefined || value === "") return null;
  const parsed = Number(value);
  if (!Number.isInteger(parsed)) validationProblem("cursor is not valid.");
  return parsed;
}

export const threadsOperations = [
  // ─── POST /messages/:rootId/replies ───────────────────────────────────────

  defineMockOperation<RootParams, ReplyBody>({
    id: "chat.threads.reply",
    contract: { kind: "openapi", operationId: "Chat.Threads.Reply" },
    method: "POST",
    route: "/workspaces/:workspaceId/chat/messages/:rootId/replies",
    async handle({ params, body, request, store }) {
      return guarded(async () => {
        const state = store.chat;
        const caller = callerIn(store, params.workspaceId);
        const key = requireIdempotencyKey(request.headers);
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
        const alsoSendToChannel = body?.alsoSendToChannel === true;
        return idempotentCreate(
          state,
          `${params.workspaceId}|${caller.userId}|chat.threads.reply`,
          key,
          {
            rootId: params.rootId,
            content,
            clientMessageId,
            alsoSendToChannel,
            mentions: body?.mentions ?? null,
            confirmBroadcast: body?.confirmBroadcast === true,
          },
          () => {
            const root = state.messages.get(params.rootId);
            if (!root || root.workspaceId !== params.workspaceId) {
              notFoundProblem();
            }
            const space = liveSpace(state, params.workspaceId, root.spaceId);
            if (!state.activeMembershipOf(space.id, caller.userId)) {
              notFoundProblem();
            }
            if (space.archivedAt !== null) archivedProblem();
            if (root.threadRootId !== null || root.kind === "system") {
              fail(
                409,
                CHAT_ERROR.threadRootInvalid,
                "Thread root invalid",
                "Only a message of the main flow can have replies.",
              );
            }
            // A retry with the same clientMessageId answers with the stored reply and stores nothing.
            const stored = state
              .messagesOf(space.id)
              .find(
                (m) =>
                  m.authorId === caller.userId &&
                  m.clientMessageId === clientMessageId,
              );
            if (stored)
              return {
                status: 200,
                body: messageView(
                  state,
                  caller.userId,
                  stored,
                  storedWarnings(
                    state,
                    space.id,
                    state.mentions.get(stored.id) ?? [],
                  ),
                ),
              };
            if (
              root.deletedAt !== null &&
              (state.threads.get(root.id)?.replyCount ?? 0) === 0
            ) {
              fail(
                409,
                CHAT_ERROR.messageDeleted,
                "Message deleted",
                "The message was deleted and has no replies.",
              );
            }
            const resolution = resolveMentions(
              state,
              caller,
              space,
              body?.content ?? "",
              body?.mentions,
              body?.confirmBroadcast === true,
            );
            const reply = appendThreadReply(
              state,
              space,
              root,
              caller.userId,
              content,
              clientMessageId,
              alsoSendToChannel,
              resolution.spans,
            );
            return {
              status: 201,
              body: messageView(
                state,
                caller.userId,
                reply,
                resolution.notices,
              ),
            };
          },
        );
      });
    },
  }),

  // ─── GET /messages/:rootId/replies ────────────────────────────────────────

  defineMockOperation<RootParams>({
    id: "chat.threads.listReplies",
    contract: { kind: "openapi", operationId: "Chat.Threads.ListReplies" },
    method: "GET",
    route: "/workspaces/:workspaceId/chat/messages/:rootId/replies",
    async handle({ params, query, store }) {
      return guarded(async () => {
        const state = store.chat;
        const caller = callerIn(store, params.workspaceId);
        requireAction(caller, "viewWorkspace");
        const after = optionalInteger(query.after, "after");
        const limit =
          optionalInteger(query.limit, "limit") ?? CHAT_LIMITS.pageSizeDefault;
        if (after !== undefined && after < 0) {
          validationProblem("after must be 0 or more.");
        }
        if (limit < 1 || limit > CHAT_LIMITS.pageSizeMax) {
          validationProblem(
            `limit must be between 1 and ${CHAT_LIMITS.pageSizeMax}.`,
          );
        }
        const root = state.messages.get(params.rootId);
        if (!root || root.workspaceId !== params.workspaceId) notFoundProblem();
        // A reply is not a root: its own thread does not exist.
        if (root.threadRootId !== null) notFoundProblem();
        const space = liveSpace(state, params.workspaceId, root.spaceId);
        requireVisible(state, space, caller);
        const rows = state
          .messagesOf(space.id)
          .filter(
            (m) =>
              m.threadRootId === root.id &&
              (m.threadSequence ?? 0) > (after ?? 0),
          )
          .slice(0, limit + 1);
        return ok({
          root: messageView(state, caller.userId, root),
          thread: threadSummaryView(
            state,
            root,
            caller.userId,
            state.nowDate(),
          ),
          items: rows
            .slice(0, limit)
            .map((m) => messageView(state, caller.userId, m)),
          hasNewer: rows.length > limit,
        });
      });
    },
  }),

  // ─── PUT /threads/:threadId/follow ────────────────────────────────────────

  defineMockOperation<ThreadParams, FollowBody>({
    id: "chat.threads.follow",
    contract: { kind: "openapi", operationId: "Chat.Threads.Follow" },
    method: "PUT",
    route: "/workspaces/:workspaceId/chat/threads/:threadId/follow",
    async handle({ params, body, store }) {
      return guarded(async () => {
        const state = store.chat;
        const caller = callerIn(store, params.workspaceId);
        requireAction(caller, "viewWorkspace");
        if (typeof body?.following !== "boolean") {
          validationProblem("following is required.");
        }
        const following = body.following;
        const muted = body.muted === true;
        const thread = state.threads.get(params.threadId);
        if (!thread || thread.workspaceId !== params.workspaceId) {
          notFoundProblem();
        }
        const space = liveSpace(state, params.workspaceId, thread.spaceId);
        requireVisible(state, space, caller);
        if (!state.activeMembershipOf(space.id, caller.userId)) {
          notFoundProblem();
        }
        const key = `${thread.rootMessageId}|${caller.userId}`;
        if (!following) {
          state.threadFollowers.delete(key);
          return ok({
            rootMessageId: thread.rootMessageId,
            following: false,
            muted: false,
          });
        }
        const existing = state.threadFollowerOf(
          thread.rootMessageId,
          caller.userId,
        );
        if (existing) {
          existing.muted = muted;
        } else {
          // A new follow starts with the replies already posted read.
          state.threadFollowers.set(key, {
            rootMessageId: thread.rootMessageId,
            workspaceId: params.workspaceId,
            userId: caller.userId,
            muted,
            readReplySequence: thread.replyCount,
          });
        }
        return ok({
          rootMessageId: thread.rootMessageId,
          following: true,
          muted,
        });
      });
    },
  }),

  // ─── PUT /threads/:threadId/read ──────────────────────────────────────────

  defineMockOperation<ThreadParams, MarkReadBody>({
    id: "chat.threads.markRead",
    contract: { kind: "openapi", operationId: "Chat.Threads.MarkRead" },
    method: "PUT",
    route: "/workspaces/:workspaceId/chat/threads/:threadId/read",
    async handle({ params, body, store }) {
      return guarded(async () => {
        const state = store.chat;
        const caller = callerIn(store, params.workspaceId);
        requireAction(caller, "viewWorkspace");
        const requested = body?.upToReplySequence;
        if (
          typeof requested !== "number" ||
          !Number.isInteger(requested) ||
          requested < 0
        ) {
          validationProblem("upToReplySequence is required.");
        }
        const thread = state.threads.get(params.threadId);
        if (!thread || thread.workspaceId !== params.workspaceId) {
          notFoundProblem();
        }
        const space = liveSpace(state, params.workspaceId, thread.spaceId);
        requireVisible(state, space, caller);
        if (!state.activeMembershipOf(space.id, caller.userId)) {
          notFoundProblem();
        }
        // The pointer only moves forward, never past the last reply; a thread the caller does not follow has none.
        const follower = state.threadFollowerOf(
          thread.rootMessageId,
          caller.userId,
        );
        if (follower) {
          follower.readReplySequence = Math.max(
            follower.readReplySequence,
            Math.min(requested, thread.replyCount),
          );
        }
        return ok({
          rootMessageId: thread.rootMessageId,
          unreadReplies: follower
            ? Math.max(0, thread.replyCount - follower.readReplySequence)
            : 0,
        });
      });
    },
  }),

  // ─── GET /me/threads ──────────────────────────────────────────────────────

  defineMockOperation<{ workspaceId: string }>({
    id: "chat.threads.mine",
    contract: { kind: "openapi", operationId: "Chat.Threads.Mine" },
    method: "GET",
    route: "/workspaces/:workspaceId/chat/me/threads",
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
        const before = parseThreadsCursor(query.cursor);
        const now = state.nowDate();
        // Only threads of live spaces the caller is still an active member of are listed.
        const rows: { rootMessageId: string; lastReplyAt: string }[] = [];
        for (const follower of state.threadFollowers.values()) {
          if (
            follower.userId !== caller.userId ||
            follower.workspaceId !== params.workspaceId
          ) {
            continue;
          }
          const thread = state.threads.get(follower.rootMessageId);
          if (!thread) continue;
          const space = state.spaces.get(thread.spaceId);
          if (!space || space.deletedAt !== null) continue;
          if (!state.activeMembershipOf(space.id, caller.userId)) continue;
          if (before !== null && Date.parse(thread.lastReplyAt) >= before) {
            continue;
          }
          rows.push({
            rootMessageId: thread.rootMessageId,
            lastReplyAt: thread.lastReplyAt,
          });
        }
        // Most recent reply first; the root id breaks a tie so the order is stable.
        rows.sort(
          (a, b) =>
            Date.parse(b.lastReplyAt) - Date.parse(a.lastReplyAt) ||
            (a.rootMessageId < b.rootMessageId ? 1 : -1),
        );
        const page = rows.slice(0, limit);
        const last = page[page.length - 1];
        return ok({
          items: page.map(({ rootMessageId }) => {
            const root = state.messages.get(rootMessageId)!;
            return {
              root: messageView(state, caller.userId, root),
              thread: threadSummaryView(state, root, caller.userId, now),
            };
          }),
          nextCursor:
            rows.length > limit && last
              ? String(Date.parse(last.lastReplyAt))
              : null,
        });
      });
    },
  }),
];
