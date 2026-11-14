/**
 * Direct and group conversations: start, add people, leave, list and hide (UC-CHT-E01 to E05, E07). A conversation is a space
 * whose participants are fixed people; messages, threads, pins, reactions and unread counts come from the space operations.
 * Only participants see it: the Workspace Owner and Admin do not (CHT-004), and every refusal is the same NotFound.
 */

import { defineMockOperation } from "../../operations/types";
import { ok } from "../../transport/create-response";
import {
  callerIn,
  fail,
  guarded,
  isConversationKind,
  liveSpace,
  notFoundProblem,
  optionalInteger,
  optionalText,
  requireAction,
  requireIdempotencyKey,
  ruleProblem,
  unreadRow,
  validationProblem,
  type ChatCaller,
} from "./chat-access";
import {
  CHAT_ERROR,
  CHAT_LIMITS,
  CHAT_RULE,
  CHAT_SYSTEM_TEXT,
  type ChatSpaceRecord,
} from "./chat-model";
import { appendSystemMessage, idempotentCreate } from "./chat-writes";
import { isActiveWorkspaceMember } from "./members.handlers";
import type { MockChatState } from "./chat-state";
import type { MockStore } from "../../state/mock-store";

type WorkspaceParams = { workspaceId: string };
type ConversationParams = WorkspaceParams & { conversationId: string };

const PREVIEW_MAX_LENGTH = 140;
const ADD_AT_ONCE_MAX = 100;

/** A live conversation the caller participates in; anything else is the NotFound of a missing one. */
function participantSpace(
  state: MockChatState,
  caller: ChatCaller,
  conversationId: string,
): ChatSpaceRecord {
  const space = liveSpace(state, caller.workspaceId, conversationId);
  if (
    !isConversationKind(space.kind) ||
    state.activeMembershipOf(space.id, caller.userId) === undefined
  ) {
    notFoundProblem();
  }
  return space;
}

/** True when both people hold an active membership in one live space of the Workspace. */
function sharesSpace(
  state: MockChatState,
  workspaceId: string,
  first: string,
  second: string,
): boolean {
  return state
    .spacesOfWorkspace(workspaceId)
    .some(
      (space) =>
        space.deletedAt === null &&
        state.activeMembershipOf(space.id, first) !== undefined &&
        state.activeMembershipOf(space.id, second) !== undefined,
    );
}

/** A Guest reaches a person only through a space they share; everyone else reaches any active member. */
function reachable(
  store: MockStore,
  caller: ChatCaller,
  userId: string,
): boolean {
  if (!isActiveWorkspaceMember(store, caller.workspaceId, userId)) return false;
  return (
    caller.role !== "Guest" ||
    sharesSpace(store.chat, caller.workspaceId, caller.userId, userId)
  );
}

/** Adds a person to a conversation, or brings a former participant back. False when already active. */
function addParticipant(
  state: MockChatState,
  space: ChatSpaceRecord,
  userId: string,
  joinedAt: string,
): boolean {
  const existing = state.membershipOf(space.id, userId);
  if (existing && existing.leftAt === null) return false;
  if (existing) {
    existing.role = "member";
    existing.leftAt = null;
    existing.hiddenAt = null;
    return true;
  }
  state.memberships.set(state.membershipKey(space.id, userId), {
    spaceId: space.id,
    workspaceId: space.workspaceId,
    userId,
    role: "member",
    notificationLevel: "all",
    mutedUntil: null,
    readSequence: 0,
    mentionCount: 0,
    joinedAt,
    leftAt: null,
    hiddenAt: null,
  });
  return true;
}

function activeCount(state: MockChatState, space: ChatSpaceRecord): number {
  return state.membershipsOfSpace(space.id).filter((row) => row.leftAt === null)
    .length;
}

function newConversation(
  state: MockChatState,
  workspaceId: string,
  kind: "direct" | "group",
  name: string,
  createdBy: string,
  people: readonly string[],
): ChatSpaceRecord {
  const createdAt = state.stampWrite();
  const space: ChatSpaceRecord = {
    id: state.nextId("space"),
    workspaceId,
    kind,
    name,
    topic: null,
    groupId: null,
    position: 0,
    archivedAt: null,
    deletedAt: null,
    lastSequence: 0,
    version: 1,
    createdAt,
    createdBy,
  };
  state.spaces.set(space.id, space);
  for (const person of people) addParticipant(state, space, person, createdAt);
  return space;
}

/** ConversationDto: the viewer's read model. Participants are ordered by join time, then id. */
function conversationView(
  state: MockChatState,
  space: ChatSpaceRecord,
  viewerUserId: string,
  now: Date,
) {
  const mine = state.activeMembershipOf(space.id, viewerUserId);
  const participants = state
    .membershipsOfSpace(space.id)
    .filter((row) => row.leftAt === null)
    .sort(
      (a, b) =>
        Date.parse(a.joinedAt) - Date.parse(b.joinedAt) ||
        (a.userId < b.userId ? -1 : a.userId > b.userId ? 1 : 0),
    )
    .map((row) => ({
      userId: row.userId,
      role: row.role,
      joinedAt: row.joinedAt,
    }));
  const newest = state.messagesOf(space.id).at(-1);
  const lastMessage = newest
    ? {
        sequence: newest.sequence,
        authorId: newest.authorId,
        kind: newest.kind,
        text:
          newest.deletedAt === null && newest.content !== null
            ? newest.content.trim().slice(0, PREVIEW_MAX_LENGTH)
            : null,
        createdAt: newest.createdAt,
      }
    : null;
  return {
    id: space.id,
    kind: space.kind,
    name: space.name === "" ? null : space.name,
    participants,
    isHidden: mine !== undefined && mine.hiddenAt !== null,
    notificationLevel: mine ? mine.notificationLevel : "all",
    unreadCount: mine ? unreadRow(state, space, mine, now).unreadCount : 0,
    lastMessage,
    lastActivityAt: lastMessage?.createdAt ?? space.createdAt,
    version: space.version,
    createdAt: space.createdAt,
  };
}

interface StartBody {
  kind?: string | null;
  userId?: string | null;
  userIds?: string[] | null;
  name?: string | null;
}

function startReply(
  created: boolean,
  conversation: unknown,
  skipped: string[],
): { status: number; body: unknown } {
  return {
    status: created ? 201 : 200,
    body: { conversation, created, skipped },
  };
}

export const conversationsOperations = [
  // ─── GET /conversations ───────────────────────────────────────────────────

  defineMockOperation<WorkspaceParams>({
    id: "chat.conversations.list",
    contract: { kind: "openapi", operationId: "Chat.Conversations.List" },
    method: "GET",
    route: "/workspaces/:workspaceId/chat/conversations",
    async handle({ params, query, store }) {
      return guarded(async () => {
        const state = store.chat;
        const caller = callerIn(store, params.workspaceId);
        requireAction(caller, "viewWorkspace");
        const limit = optionalInteger(query.limit, "limit") ?? 50;
        if (limit < 1 || limit > 100) {
          validationProblem("limit is 1 to 100.");
        }
        const now = state.nowDate();
        const rows = state
          .spacesOfWorkspace(params.workspaceId)
          .filter((space) => {
            if (space.deletedAt !== null || !isConversationKind(space.kind)) {
              return false;
            }
            const mine = state.activeMembershipOf(space.id, caller.userId);
            return mine !== undefined && mine.hiddenAt === null;
          })
          .map((space) => conversationView(state, space, caller.userId, now))
          .sort(
            (a, b) =>
              Date.parse(b.lastActivityAt) - Date.parse(a.lastActivityAt) ||
              (a.id < b.id ? -1 : a.id > b.id ? 1 : 0),
          )
          .slice(0, limit);
        return ok(rows);
      });
    },
  }),

  // ─── POST /conversations ──────────────────────────────────────────────────

  defineMockOperation<WorkspaceParams, StartBody>({
    id: "chat.conversations.start",
    contract: { kind: "openapi", operationId: "Chat.Conversations.Start" },
    method: "POST",
    route: "/workspaces/:workspaceId/chat/conversations",
    async handle({ params, body, request, store }) {
      return guarded(async () => {
        const state = store.chat;
        const { workspaceId } = params;
        const caller = callerIn(store, workspaceId);
        const key = requireIdempotencyKey(request.headers);
        const kind = body?.kind;
        if (kind !== "direct" && kind !== "group") {
          validationProblem("kind is required: direct or group.");
        }
        const name = optionalText(
          body?.name,
          "name",
          CHAT_LIMITS.channelNameMaxLength,
        );
        if (kind === "direct") {
          if (typeof body?.userId !== "string" || body.userId === "") {
            validationProblem("userId is required for a direct conversation.");
          }
        } else if (
          !Array.isArray(body?.userIds) ||
          body.userIds.length < 2 ||
          body.userIds.some((id) => typeof id !== "string" || id === "")
        ) {
          validationProblem(
            "A group conversation names at least two other people.",
          );
        }
        requireAction(caller, "messagePost");
        return idempotentCreate(
          state,
          `${workspaceId}|${caller.userId}|chat.conversations.start`,
          key,
          {
            kind,
            userId: body?.userId ?? null,
            userIds: body?.userIds ?? null,
            name,
          },
          () =>
            kind === "direct"
              ? startDirect(store, caller, body!.userId as string)
              : startGroup(store, caller, body!.userIds as string[], name),
        );
      });
    },
  }),

  // ─── POST /conversations/:conversationId/participants ─────────────────────

  defineMockOperation<ConversationParams, { userIds?: string[] | null }>({
    id: "chat.conversations.addParticipants",
    contract: {
      kind: "openapi",
      operationId: "Chat.Conversations.AddParticipants",
    },
    method: "POST",
    route:
      "/workspaces/:workspaceId/chat/conversations/:conversationId/participants",
    async handle({ params, body, store }) {
      return guarded(async () => {
        const state = store.chat;
        const caller = callerIn(store, params.workspaceId);
        if (!Array.isArray(body?.userIds)) {
          validationProblem("userIds is required.");
        }
        if (
          body.userIds.length === 0 ||
          body.userIds.length > ADD_AT_ONCE_MAX
        ) {
          validationProblem(
            body.userIds.length === 0
              ? "userIds is required."
              : `At most ${ADD_AT_ONCE_MAX} people can be added at once.`,
          );
        }
        requireAction(caller, "memberInvite");
        const space = participantSpace(state, caller, params.conversationId);
        if (space.kind !== "group") {
          ruleProblem(
            CHAT_RULE.kindNotSupported,
            "Only a group conversation takes new participants.",
          );
        }
        const added: string[] = [];
        const skipped: string[] = [];
        for (const candidate of Array.from(new Set(body.userIds))) {
          if (!reachable(store, caller, candidate)) {
            skipped.push(candidate);
            continue;
          }
          if (state.activeMembershipOf(space.id, candidate)) continue;
          if (activeCount(state, space) >= CHAT_LIMITS.groupCap) {
            fail(
              422,
              CHAT_ERROR.limitReached,
              "Limit reached",
              `A group conversation holds at most ${CHAT_LIMITS.groupCap} people.`,
            );
          }
          addParticipant(state, space, candidate, state.stampWrite());
          added.push(candidate);
          appendSystemMessage(
            state,
            space,
            caller.userId,
            CHAT_SYSTEM_TEXT.memberAdded,
          );
        }
        return ok({ added, skipped });
      });
    },
  }),

  // ─── DELETE /conversations/:conversationId/participants/me ────────────────

  defineMockOperation<ConversationParams>({
    id: "chat.conversations.leave",
    contract: { kind: "openapi", operationId: "Chat.Conversations.Leave" },
    method: "DELETE",
    route:
      "/workspaces/:workspaceId/chat/conversations/:conversationId/participants/me",
    async handle({ params, store }) {
      return guarded(async () => {
        const state = store.chat;
        const caller = callerIn(store, params.workspaceId);
        requireAction(caller, "viewWorkspace");
        const space = participantSpace(state, caller, params.conversationId);
        if (space.kind !== "group") {
          ruleProblem(
            CHAT_RULE.kindNotSupported,
            "Only a group conversation can be left.",
          );
        }
        const mine = state.activeMembershipOf(space.id, caller.userId)!;
        mine.leftAt = state.stampWrite();
        if (activeCount(state, space) === 0) {
          // The last participant closes the conversation (a soft delete the retention purge follows).
          space.deletedAt = state.stampWrite();
        } else {
          appendSystemMessage(
            state,
            space,
            caller.userId,
            CHAT_SYSTEM_TEXT.memberLeft,
          );
        }
        return { status: 204 };
      });
    },
  }),

  // ─── PUT /conversations/:conversationId/visibility ────────────────────────

  defineMockOperation<ConversationParams, { hidden?: boolean | null }>({
    id: "chat.conversations.setVisibility",
    contract: {
      kind: "openapi",
      operationId: "Chat.Conversations.SetVisibility",
    },
    method: "PUT",
    route:
      "/workspaces/:workspaceId/chat/conversations/:conversationId/visibility",
    async handle({ params, body, store }) {
      return guarded(async () => {
        const state = store.chat;
        const caller = callerIn(store, params.workspaceId);
        if (typeof body?.hidden !== "boolean") {
          validationProblem("hidden is required.");
        }
        requireAction(caller, "viewWorkspace");
        const space = participantSpace(state, caller, params.conversationId);
        const mine = state.activeMembershipOf(space.id, caller.userId)!;
        if (body.hidden) mine.hiddenAt ??= state.stampWrite();
        else mine.hiddenAt = null;
        return ok({ conversationId: space.id, hidden: mine.hiddenAt !== null });
      });
    },
  }),
];

function startDirect(
  store: MockStore,
  caller: ChatCaller,
  other: string,
): { status: number; body: unknown } {
  const state = store.chat;
  const { workspaceId } = caller;
  if (other === caller.userId) {
    ruleProblem(
      CHAT_RULE.selfTarget,
      "A direct conversation needs two different people.",
    );
  }
  if (
    !isActiveWorkspaceMember(store, workspaceId, other) ||
    state.isBlockedBetween(workspaceId, caller.userId, other) ||
    !reachable(store, caller, other)
  ) {
    notFoundProblem();
  }
  const now = state.nowDate();
  const pairKey = state.pairKey(workspaceId, caller.userId, other);
  const existingId = state.directPairs.get(pairKey);
  if (existingId !== undefined) {
    const existing = liveSpace(state, workspaceId, existingId);
    const mine = state.activeMembershipOf(existing.id, caller.userId);
    // Starting it again brings it back to the caller's list, as a new message does.
    if (mine) mine.hiddenAt = null;
    return startReply(
      false,
      conversationView(state, existing, caller.userId, now),
      [],
    );
  }
  const space = newConversation(
    state,
    workspaceId,
    "direct",
    "",
    caller.userId,
    [caller.userId, other],
  );
  state.directPairs.set(pairKey, space.id);
  return startReply(
    true,
    conversationView(state, space, caller.userId, now),
    [],
  );
}

function startGroup(
  store: MockStore,
  caller: ChatCaller,
  userIds: string[],
  name: string | null,
): { status: number; body: unknown } {
  const state = store.chat;
  const others = Array.from(new Set(userIds)).filter(
    (id) => id !== caller.userId,
  );
  if (others.length + 1 > CHAT_LIMITS.groupCap) {
    fail(
      422,
      CHAT_ERROR.limitReached,
      "Limit reached",
      `A group conversation holds at most ${CHAT_LIMITS.groupCap} people.`,
    );
  }
  const added = others.filter((id) => reachable(store, caller, id));
  const skipped = others.filter((id) => !added.includes(id));
  if (added.length + 1 < 3) {
    ruleProblem(
      CHAT_RULE.groupTooSmall,
      "A group conversation has at least three people.",
    );
  }
  const space = newConversation(
    state,
    caller.workspaceId,
    "group",
    name ?? "",
    caller.userId,
    [caller.userId, ...added],
  );
  return startReply(
    true,
    conversationView(state, space, caller.userId, state.nowDate()),
    skipped,
  );
}
