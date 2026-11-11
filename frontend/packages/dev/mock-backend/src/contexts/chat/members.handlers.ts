/**
 * Channel members: add Workspace members, join a public channel, leave, and
 * remove a member (UC-CHT-A05 to A07). Each change of membership writes a
 * system message attributed to the person who made it.
 */

import { defineMockOperation } from "../../operations/types";
import { ok } from "../../transport/create-response";
import type { MockStore } from "../../state/mock-store";
import {
  archivedProblem,
  callerIn,
  channelView,
  fail,
  forbiddenProblem,
  guarded,
  liveChannel,
  notFoundProblem,
  requireAction,
  requireManager,
  requireVisible,
  validationProblem,
} from "./chat-access";
import {
  CHAT_ERROR,
  CHAT_SYSTEM_TEXT,
  type ChatSpaceRecord,
} from "./chat-model";
import { appendSystemMessage } from "./chat-writes";
import type { MockChatState } from "./chat-state";

type ChannelParams = {
  workspaceId: string;
  channelId: string;
};

/** A person who is an active member of the Workspace (a suspended one is not). */
export function isActiveWorkspaceMember(
  store: MockStore,
  workspaceId: string,
  userId: string,
): boolean {
  const row = store
    .getWorkspaceMembers(workspaceId)
    .find((m) => m.userId === userId);
  return row !== undefined && !store.isMembershipSuspended(row.id);
}

/** Adds a person to a space as a member, or brings a former member back. False when already active. */
function addMember(
  state: MockChatState,
  space: ChatSpaceRecord,
  userId: string,
): boolean {
  const existing = state.membershipOf(space.id, userId);
  if (existing && existing.leftAt === null) return false;
  const joinedAt = state.stampWrite();
  if (existing) {
    existing.role = "member";
    existing.leftAt = null;
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

export const membersOperations = [
  // ─── POST /channels/:channelId/members ────────────────────────────────────

  defineMockOperation<ChannelParams, { userIds?: string[] | null }>({
    id: "chat.members.add",
    contract: { kind: "openapi", operationId: "Chat.Members.Add" },
    method: "POST",
    route: "/workspaces/:workspaceId/chat/channels/:channelId/members",
    async handle({ params, body, store }) {
      return guarded(async () => {
        const state = store.chat;
        const caller = callerIn(store, params.workspaceId);
        requireAction(caller, "memberInvite");
        if (!Array.isArray(body?.userIds))
          validationProblem("userIds is required.");
        const space = liveChannel(state, params.workspaceId, params.channelId);
        requireVisible(state, space, caller);
        if (space.kind === "channel_public")
          requireManager(state, space, caller);
        const candidates = Array.from(new Set(body.userIds));
        const added: string[] = [];
        const skipped: string[] = [];
        for (const userId of candidates) {
          if (isActiveWorkspaceMember(store, params.workspaceId, userId))
            continue;
          skipped.push(userId);
        }
        const reachable = candidates.filter(
          (userId) => !skipped.includes(userId),
        );
        // The backend refuses the whole batch when the channel is archived and someone would be added.
        if (reachable.length > 0 && space.archivedAt !== null)
          archivedProblem();
        for (const userId of reachable) {
          if (addMember(state, space, userId)) {
            added.push(userId);
            appendSystemMessage(
              state,
              space,
              caller.userId,
              CHAT_SYSTEM_TEXT.memberAdded,
            );
          }
        }
        return ok({ added, skipped });
      });
    },
  }),

  // ─── POST /channels/:channelId/members/me ─────────────────────────────────

  defineMockOperation<ChannelParams>({
    id: "chat.members.join",
    contract: { kind: "openapi", operationId: "Chat.Members.Join" },
    method: "POST",
    route: "/workspaces/:workspaceId/chat/channels/:channelId/members/me",
    async handle({ params, store }) {
      return guarded(async () => {
        const state = store.chat;
        const caller = callerIn(store, params.workspaceId);
        if (caller.role === "Guest") {
          forbiddenProblem(
            "A Guest joins a channel only when they are added to it.",
          );
        }
        const space = liveChannel(state, params.workspaceId, params.channelId);
        // A private channel is not there for a person who is not in it.
        if (space.kind === "channel_private") notFoundProblem();
        if (space.archivedAt !== null) archivedProblem();
        if (addMember(state, space, caller.userId)) {
          appendSystemMessage(
            state,
            space,
            caller.userId,
            CHAT_SYSTEM_TEXT.memberJoined,
          );
        }
        return ok(channelView(state, space, caller.userId));
      });
    },
  }),

  // ─── DELETE /channels/:channelId/members/:userId ──────────────────────────

  defineMockOperation<ChannelParams & { userId: string }>({
    id: "chat.members.remove",
    contract: { kind: "openapi", operationId: "Chat.Members.Remove" },
    method: "DELETE",
    route: "/workspaces/:workspaceId/chat/channels/:channelId/members/:userId",
    async handle({ params, store }) {
      return guarded(async () => {
        const state = store.chat;
        const caller = callerIn(store, params.workspaceId);
        const space = liveChannel(state, params.workspaceId, params.channelId);
        requireVisible(state, space, caller);
        const self = params.userId === caller.userId;
        if (!self) requireManager(state, space, caller);
        // Leaving or removal is refused in an archived channel, even for a person not in it.
        if (space.archivedAt !== null) archivedProblem();
        const member = state.activeMembershipOf(space.id, params.userId);
        if (!member) return { status: 204 };
        if (member.role === "manager") {
          const managers = state
            .membershipsOfSpace(space.id)
            .filter(
              (row) => row.leftAt === null && row.role === "manager",
            ).length;
          if (managers <= 1) {
            fail422LastManager();
          }
        }
        member.leftAt = state.stampWrite();
        appendSystemMessage(
          state,
          space,
          caller.userId,
          self ? CHAT_SYSTEM_TEXT.memberLeft : CHAT_SYSTEM_TEXT.memberRemoved,
        );
        return { status: 204 };
      });
    },
  }),
];

/** A live channel keeps at least one manager (422 last-manager, ChatRuleCodes.LastManager). */
function fail422LastManager(): never {
  return fail(
    422,
    CHAT_ERROR.lastManager,
    "Last manager",
    "A channel keeps at least one manager.",
  );
}
