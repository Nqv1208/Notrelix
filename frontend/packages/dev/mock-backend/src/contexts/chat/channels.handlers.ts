/**
 * Channels of a Workspace: create, browse, read, rename, archive, delete and
 * order (UC-CHT-A01 to A08). Each handler checks in the backend's order: the
 * caller, the pipeline's permission, the request body, the channel and its
 * visibility, the manager rule, the business rule, then the expected version.
 */

import { defineMockOperation } from "../../operations/types";
import { ok } from "../../transport/create-response";
import {
  archivedProblem,
  callerIn,
  channelView,
  groupIn,
  guarded,
  liveSpace,
  nextChannelPosition,
  optionalInteger,
  optionalText,
  parseVersion,
  requireAction,
  requireIdempotencyKey,
  requireManager,
  requireUniqueChannelName,
  requireVersion,
  requireVisible,
  requiredPosition,
  requiredText,
  validationProblem,
} from "./chat-access";
import {
  CHAT_LIMITS,
  CHAT_SYSTEM_TEXT,
  type ChatSpaceRecord,
} from "./chat-model";
import { idempotentCreate, appendSystemMessage } from "./chat-writes";
import type { MockChatState } from "./chat-state";

const WS = "/workspaces/:workspaceId/chat/channels";

type WorkspaceParams = {
  workspaceId: string;
};

type ChannelParams = WorkspaceParams & {
  channelId: string;
};

interface CreateChannelBody {
  name?: string | null;
  topic?: string | null;
  isPrivate?: boolean;
  groupId?: string | null;
}

interface UpdateChannelBody {
  name?: string | null;
  topic?: string | null;
  expectedVersion?: number | null;
}

interface ArchiveChannelBody {
  archived?: boolean | null;
  expectedVersion?: number | null;
}

interface PlacementBody {
  channelId?: string;
  groupId?: string | null;
  position?: number;
  expectedVersion?: number | null;
}

/** The sidebar order: position, then name (ordinal, as the backend orders the rows). */
function sidebarOrder(a: ChatSpaceRecord, b: ChatSpaceRecord): number {
  if (a.position !== b.position) return a.position - b.position;
  return a.name < b.name ? -1 : a.name > b.name ? 1 : 0;
}

/** The members of a space who are active, oldest first (GetChannel). */
function activeMembersOldestFirst(state: MockChatState, spaceId: string) {
  return state
    .membershipsOfSpace(spaceId)
    .filter((row) => row.leftAt === null)
    .sort((a, b) => Date.parse(a.joinedAt) - Date.parse(b.joinedAt))
    .map((row) => ({
      userId: row.userId,
      role: row.role,
      joinedAt: row.joinedAt,
    }));
}

/** A channel's name is renamed: a system message announces it (ChatSystemMessages.Renamed). */
function announceRename(
  state: MockChatState,
  space: ChatSpaceRecord,
  actorId: string,
): void {
  appendSystemMessage(
    state,
    space,
    actorId,
    CHAT_SYSTEM_TEXT.renamed(space.name),
  );
}

export const channelsOperations = [
  // ─── GET /channels ────────────────────────────────────────────────────────

  defineMockOperation<WorkspaceParams>({
    id: "chat.channels.list",
    contract: { kind: "openapi", operationId: "Chat.Channels.List" },
    method: "GET",
    route: WS,
    async handle({ params, query, store }) {
      return guarded(async () => {
        const state = store.chat;
        const caller = callerIn(store, params.workspaceId);
        requireAction(caller, "viewWorkspace");
        const browse = query.browse === "true";
        let spaces: ChatSpaceRecord[];
        if (browse) {
          // A Guest browses nothing: they see only what they were added to.
          if (caller.role === "Guest") return ok([]);
          spaces = state
            .spacesOfWorkspace(params.workspaceId)
            .filter(
              (space) =>
                space.deletedAt === null &&
                space.kind === "channel_public" &&
                state.activeMembershipOf(space.id, caller.userId) === undefined,
            );
        } else {
          spaces = state
            .spacesOfWorkspace(params.workspaceId)
            .filter(
              (space) =>
                space.deletedAt === null &&
                state.activeMembershipOf(space.id, caller.userId) !== undefined,
            );
        }
        spaces.sort(sidebarOrder);
        return ok(
          spaces.map((space) => channelView(state, space, caller.userId)),
        );
      });
    },
  }),

  // ─── POST /channels ───────────────────────────────────────────────────────

  defineMockOperation<WorkspaceParams, CreateChannelBody>({
    id: "chat.channels.create",
    contract: { kind: "openapi", operationId: "Chat.Channels.Create" },
    method: "POST",
    route: WS,
    async handle({ params, body, request, store }) {
      return guarded(async () => {
        const state = store.chat;
        const { workspaceId } = params;
        const caller = callerIn(store, workspaceId);
        const key = requireIdempotencyKey(request.headers);
        requireAction(caller, "channelCreate");
        const name = requiredText(
          body?.name,
          "name",
          CHAT_LIMITS.channelNameMaxLength,
        );
        const topic = optionalText(
          body?.topic,
          "topic",
          CHAT_LIMITS.channelTopicMaxLength,
        );
        const isPrivate = body?.isPrivate === true;
        const groupId = body?.groupId ?? null;
        return idempotentCreate(
          state,
          `${workspaceId}|${caller.userId}|chat.channels.create`,
          key,
          { name, topic, isPrivate, groupId },
          () => {
            requireUniqueChannelName(state, workspaceId, name, null);
            if (groupId !== null) groupIn(state, workspaceId, groupId);
            const now = state.stampWrite();
            const space: ChatSpaceRecord = {
              id: state.nextId("space"),
              workspaceId,
              kind: isPrivate ? "channel_private" : "channel_public",
              name,
              topic,
              groupId,
              position: nextChannelPosition(state, workspaceId),
              archivedAt: null,
              deletedAt: null,
              lastSequence: 0,
              version: 1,
              createdAt: now,
              createdBy: caller.userId,
            };
            state.spaces.set(space.id, space);
            // The caller becomes the first manager.
            state.memberships.set(
              state.membershipKey(space.id, caller.userId),
              {
                spaceId: space.id,
                workspaceId,
                userId: caller.userId,
                role: "manager",
                notificationLevel: "all",
                mutedUntil: null,
                readSequence: 0,
                mentionCount: 0,
                joinedAt: now,
                leftAt: null,
              },
            );
            return {
              status: 201,
              body: channelView(state, space, caller.userId),
            };
          },
        );
      });
    },
  }),

  // ─── GET /channels/:channelId ─────────────────────────────────────────────

  defineMockOperation<ChannelParams>({
    id: "chat.channels.get",
    contract: { kind: "openapi", operationId: "Chat.Channels.Get" },
    method: "GET",
    route: `${WS}/:channelId`,
    async handle({ params, store }) {
      return guarded(async () => {
        const state = store.chat;
        const caller = callerIn(store, params.workspaceId);
        requireAction(caller, "viewWorkspace");
        const space = liveSpace(state, params.workspaceId, params.channelId);
        requireVisible(state, space, caller);
        return ok({
          channel: channelView(state, space, caller.userId),
          members: activeMembersOldestFirst(state, space.id),
        });
      });
    },
  }),

  // ─── PATCH /channels/order ────────────────────────────────────────────────

  defineMockOperation<WorkspaceParams, { placements?: PlacementBody[] | null }>(
    {
      id: "chat.channels.reorder",
      contract: { kind: "openapi", operationId: "Chat.Channels.Reorder" },
      method: "PATCH",
      route: `${WS}/order`,
      async handle({ params, body, store }) {
        return guarded(async () => {
          const state = store.chat;
          const caller = callerIn(store, params.workspaceId);
          requireAction(caller, "channelManage");
          const placements = body?.placements;
          if (!Array.isArray(placements) || placements.length === 0) {
            validationProblem("placements is required.");
          }
          if (placements.length > CHAT_LIMITS.reorderMax) {
            validationProblem(
              `placements has at most ${CHAT_LIMITS.reorderMax} channels.`,
            );
          }
          const checked = placements.map((placement) => {
            if (typeof placement.channelId !== "string")
              validationProblem("channelId is required.");
            const expected = parseVersion(
              placement.expectedVersion,
              "expectedVersion",
            );
            const position = requiredPosition(placement.position);
            return {
              placement,
              expected,
              position,
              groupId: placement.groupId ?? null,
              channelId: placement.channelId,
            };
          });
          // Every channel is checked before any is moved: a refusal changes nothing.
          const loaded = checked.map((item) => {
            const space = liveSpace(state, params.workspaceId, item.channelId);
            requireVisible(state, space, caller);
            requireManager(state, space, caller);
            requireVersion(item.expected, space.version);
            if (item.groupId !== null)
              groupIn(state, params.workspaceId, item.groupId);
            return { space, ...item };
          });
          for (const item of loaded) {
            if (item.space.archivedAt !== null) archivedProblem();
          }
          for (const item of loaded) {
            if (
              item.space.groupId !== item.groupId ||
              item.space.position !== item.position
            ) {
              item.space.groupId = item.groupId;
              item.space.position = item.position;
              item.space.version += 1;
            }
          }
          return { status: 200 };
        });
      },
    },
  ),

  // ─── PATCH /channels/:channelId ───────────────────────────────────────────

  defineMockOperation<ChannelParams, UpdateChannelBody>({
    id: "chat.channels.update",
    contract: { kind: "openapi", operationId: "Chat.Channels.Update" },
    method: "PATCH",
    route: `${WS}/:channelId`,
    async handle({ params, body, store }) {
      return guarded(async () => {
        const state = store.chat;
        const caller = callerIn(store, params.workspaceId);
        requireAction(caller, "channelManage");
        const name = requiredText(
          body?.name,
          "name",
          CHAT_LIMITS.channelNameMaxLength,
        );
        const topic = optionalText(
          body?.topic,
          "topic",
          CHAT_LIMITS.channelTopicMaxLength,
        );
        const expected = parseVersion(body?.expectedVersion, "expectedVersion");
        const space = liveSpace(state, params.workspaceId, params.channelId);
        requireVisible(state, space, caller);
        requireManager(state, space, caller);
        requireUniqueChannelName(state, params.workspaceId, name, space.id);
        if (space.archivedAt !== null) archivedProblem();
        const renamed = name !== space.name;
        const changed = renamed || topic !== space.topic;
        requireVersion(expected, space.version);
        if (changed) {
          space.name = name;
          space.topic = topic;
          space.version += 1;
        }
        if (renamed) announceRename(state, space, caller.userId);
        return ok(channelView(state, space, caller.userId));
      });
    },
  }),

  // ─── POST /channels/:channelId/archive ────────────────────────────────────

  defineMockOperation<ChannelParams, ArchiveChannelBody>({
    id: "chat.channels.archive",
    contract: { kind: "openapi", operationId: "Chat.Channels.Archive" },
    method: "POST",
    route: `${WS}/:channelId/archive`,
    async handle({ params, body, store }) {
      return guarded(async () => {
        const state = store.chat;
        const caller = callerIn(store, params.workspaceId);
        requireAction(caller, "channelManage");
        if (typeof body?.archived !== "boolean")
          validationProblem("archived is required.");
        const archive = body.archived;
        const expected = parseVersion(body.expectedVersion, "expectedVersion");
        const space = liveSpace(state, params.workspaceId, params.channelId);
        requireVisible(state, space, caller);
        requireManager(state, space, caller);
        // Unarchiving checks the name again: a name taken meanwhile is a conflict.
        if (!archive)
          requireUniqueChannelName(
            state,
            params.workspaceId,
            space.name,
            space.id,
          );
        const changed = archive !== (space.archivedAt !== null);
        requireVersion(expected, space.version);
        if (changed) {
          space.archivedAt = archive ? state.stampWrite() : null;
          space.version += 1;
          appendSystemMessage(
            state,
            space,
            caller.userId,
            archive ? CHAT_SYSTEM_TEXT.archived : CHAT_SYSTEM_TEXT.unarchived,
          );
        }
        return ok(channelView(state, space, caller.userId));
      });
    },
  }),

  // ─── DELETE /channels/:channelId ──────────────────────────────────────────

  defineMockOperation<ChannelParams>({
    id: "chat.channels.delete",
    contract: { kind: "openapi", operationId: "Chat.Channels.Delete" },
    method: "DELETE",
    route: `${WS}/:channelId`,
    async handle({ params, query, store }) {
      return guarded(async () => {
        const state = store.chat;
        const caller = callerIn(store, params.workspaceId);
        requireAction(caller, "channelManage");
        const expected = parseVersion(
          optionalInteger(query.expectedVersion, "expectedVersion"),
          "expectedVersion",
        );
        const space = liveSpace(state, params.workspaceId, params.channelId);
        requireVisible(state, space, caller);
        requireManager(state, space, caller);
        requireVersion(expected, space.version);
        // Soft delete: the row stays, the name is released (the live checks skip it).
        space.deletedAt = state.stampWrite();
        space.version += 1;
        return { status: 204 };
      });
    },
  }),
];
