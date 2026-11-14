/**
 * Read state of the caller: the unread summary, the read boundary of a space,
 * read-all and the notification level or timed mute of a space (UC-CHT-D01 to D07).
 */

import { defineMockOperation } from "../../operations/types";
import { ok } from "../../transport/create-response";
import {
  callerIn,
  effectiveLevel,
  guarded,
  liveSpace,
  notFoundProblem,
  requireAction,
  requireVisible,
  unreadSummaryOf,
  validationProblem,
  type ChatSpaceUnreadRow,
} from "./chat-access";
import { announceOwnReadState } from "./chat-realtime";
import { isoOfMs } from "../../state/clock";
import type { ChatNotificationLevel } from "./chat-model";

type SpaceParams = {
  workspaceId: string;
  spaceId: string;
};

const LEVELS: readonly ChatNotificationLevel[] = ["all", "mentions", "none"];

/** The boundary only moves forward, never past the space's last sequence (ReadBoundary). */
function advance(
  readSequence: number,
  requested: number,
  lastSequence: number,
): number {
  return Math.max(readSequence, Math.min(requested, lastSequence));
}

export const readStateOperations = [
  // ─── GET /me/unread ───────────────────────────────────────────────────────

  defineMockOperation<{ workspaceId: string }>({
    id: "chat.readState.unread",
    contract: { kind: "openapi", operationId: "Chat.ReadState.Unread" },
    method: "GET",
    route: "/workspaces/:workspaceId/chat/me/unread",
    async handle({ params, store }) {
      return guarded(async () => {
        const caller = callerIn(store, params.workspaceId);
        requireAction(caller, "viewWorkspace");
        return ok(
          unreadSummaryOf(
            store.chat,
            params.workspaceId,
            caller.userId,
            store.chat.nowDate(),
          ),
        );
      });
    },
  }),

  // ─── PUT /spaces/:spaceId/read ────────────────────────────────────────────

  defineMockOperation<SpaceParams, { upToSequence?: number | null }>({
    id: "chat.readState.markRead",
    contract: { kind: "openapi", operationId: "Chat.ReadState.MarkRead" },
    method: "PUT",
    route: "/workspaces/:workspaceId/chat/spaces/:spaceId/read",
    async handle({ params, body, store }) {
      return guarded(async () => {
        const state = store.chat;
        const caller = callerIn(store, params.workspaceId);
        requireAction(caller, "viewWorkspace");
        const requested = body?.upToSequence;
        if (
          typeof requested !== "number" ||
          !Number.isInteger(requested) ||
          requested < 0
        ) {
          validationProblem("upToSequence is required.");
        }
        const space = liveSpace(state, params.workspaceId, params.spaceId);
        const membership = state.activeMembershipOf(space.id, caller.userId);
        if (!membership) notFoundProblem();
        membership.readSequence = advance(
          membership.readSequence,
          requested,
          space.lastSequence,
        );
        // The caller's other devices learn the new count of this space (UC-CHT-C09).
        const rows = unreadSummaryOf(
          state,
          params.workspaceId,
          caller.userId,
          state.nowDate(),
        ).spaces.filter((row: ChatSpaceUnreadRow) => row.spaceId === space.id);
        announceOwnReadState(state, params.workspaceId, caller.userId, rows);
        return ok({
          spaceId: space.id,
          readSequence: membership.readSequence,
          lastSequence: space.lastSequence,
        });
      });
    },
  }),

  // ─── POST /me/read-all ────────────────────────────────────────────────────

  defineMockOperation<{ workspaceId: string }>({
    id: "chat.readState.markAllRead",
    contract: { kind: "openapi", operationId: "Chat.ReadState.MarkAllRead" },
    method: "POST",
    route: "/workspaces/:workspaceId/chat/me/read-all",
    async handle({ params, store }) {
      return guarded(async () => {
        const state = store.chat;
        const caller = callerIn(store, params.workspaceId);
        requireAction(caller, "viewWorkspace");
        const before = unreadSummaryOf(
          state,
          params.workspaceId,
          caller.userId,
          state.nowDate(),
        );
        for (const membership of state.memberships.values()) {
          if (
            membership.userId !== caller.userId ||
            membership.workspaceId !== params.workspaceId ||
            membership.leftAt !== null
          ) {
            continue;
          }
          const space = state.spaces.get(membership.spaceId);
          if (!space || space.deletedAt !== null) continue;
          membership.readSequence = advance(
            membership.readSequence,
            space.lastSequence,
            space.lastSequence,
          );
        }
        const after = unreadSummaryOf(
          state,
          params.workspaceId,
          caller.userId,
          state.nowDate(),
        );
        // One frame per space whose count moved: the other devices clear those badges.
        const moved = new Set(
          before.spaces
            .filter((row) => row.unreadCount > 0)
            .map((row) => row.spaceId),
        );
        announceOwnReadState(
          state,
          params.workspaceId,
          caller.userId,
          after.spaces.filter((row) => moved.has(row.spaceId)),
        );
        return ok(after);
      });
    },
  }),

  // ─── PUT /spaces/:spaceId/notification-level ──────────────────────────────

  defineMockOperation<
    SpaceParams,
    { level?: string | null; mutedUntil?: string | null }
  >({
    id: "chat.readState.setNotificationLevel",
    contract: {
      kind: "openapi",
      operationId: "Chat.ReadState.SetNotificationLevel",
    },
    method: "PUT",
    route: "/workspaces/:workspaceId/chat/spaces/:spaceId/notification-level",
    async handle({ params, body, store }) {
      return guarded(async () => {
        const state = store.chat;
        const caller = callerIn(store, params.workspaceId);
        requireAction(caller, "viewWorkspace");
        const level = body?.level;
        if (
          typeof level !== "string" ||
          !LEVELS.includes(level as ChatNotificationLevel)
        ) {
          validationProblem("level must be all, mentions or none.");
        }
        const mutedUntil = body?.mutedUntil ?? null;
        if (mutedUntil !== null && level !== "none") {
          validationProblem(
            "An until time applies only to a mute, which is the level none.",
          );
        }
        let until: string | null = null;
        if (mutedUntil !== null) {
          const parsed = Date.parse(mutedUntil);
          if (Number.isNaN(parsed))
            validationProblem("mutedUntil must be a date and time.");
          until = isoOfMs(parsed);
        }
        const space = liveSpace(state, params.workspaceId, params.spaceId);
        requireVisible(state, space, caller);
        const membership = state.activeMembershipOf(space.id, caller.userId);
        if (!membership) notFoundProblem();
        const now = state.nowDate();
        // A mute whose end is not in the future changes nothing.
        const pastUntil = until !== null && Date.parse(until) <= now.getTime();
        if (!pastUntil) {
          const nextLevel = level as ChatNotificationLevel;
          if (
            membership.notificationLevel !== nextLevel ||
            membership.mutedUntil !== until
          ) {
            membership.notificationLevel = nextLevel;
            membership.mutedUntil = until;
          }
        }
        const effective = effectiveLevel(
          membership.notificationLevel,
          membership.mutedUntil,
          now,
        );
        const muted = effective === "none";
        return ok({
          spaceId: space.id,
          notificationLevel: effective,
          muted,
          mutedUntil: muted ? membership.mutedUntil : null,
        });
      });
    },
  }),
];
