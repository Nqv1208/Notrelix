/**
 * Channel groups (the sidebar categories): create, rename or move, and delete.
 * Deleting a group does not delete its channels; they keep their place and
 * leave the group (ChannelGroups.Delete).
 */

import { defineMockOperation } from "../../operations/types";
import { ok } from "../../transport/create-response";
import {
  callerIn,
  groupIn,
  groupView,
  guarded,
  nextGroupPosition,
  parseVersion,
  requireAction,
  requireIdempotencyKey,
  requireUniqueGroupName,
  requireVersion,
  requiredPosition,
  requiredText,
  optionalInteger,
} from "./chat-access";
import { CHAT_LIMITS } from "./chat-model";
import { idempotentCreate } from "./chat-writes";

type GroupParams = {
  workspaceId: string;
  groupId: string;
};

const GROUPS = "/workspaces/:workspaceId/chat/channel-groups";

export const channelGroupsOperations = [
  // ─── GET /channel-groups ──────────────────────────────────────────────────
  // Every Workspace member reads all categories; a Guest only the ones that hold a channel they are in.
  defineMockOperation<{ workspaceId: string }>({
    id: "chat.channelGroups.list",
    contract: { kind: "openapi", operationId: "Chat.ChannelGroups.List" },
    method: "GET",
    route: GROUPS,
    async handle({ params, store }) {
      return guarded(async () => {
        const state = store.chat;
        const caller = callerIn(store, params.workspaceId);
        requireAction(caller, "viewWorkspace");
        const visibleGroupIds = new Set(
          state
            .spacesOfWorkspace(params.workspaceId)
            .filter(
              (space) =>
                space.deletedAt === null &&
                space.groupId !== null &&
                state.activeMembershipOf(space.id, caller.userId) !== undefined,
            )
            .map((space) => space.groupId),
        );
        const groups = [...state.groups.values()]
          .filter((group) => group.workspaceId === params.workspaceId)
          .filter(
            (group) => caller.role !== "Guest" || visibleGroupIds.has(group.id),
          )
          .sort(
            (a, b) => a.position - b.position || a.name.localeCompare(b.name),
          );
        return ok(groups.map(groupView));
      });
    },
  }),

  // ─── POST /channel-groups ─────────────────────────────────────────────────

  defineMockOperation<
    { workspaceId: string },
    { name?: string | null; position?: number | null }
  >({
    id: "chat.channelGroups.create",
    contract: { kind: "openapi", operationId: "Chat.ChannelGroups.Create" },
    method: "POST",
    route: GROUPS,
    async handle({ params, body, request, store }) {
      return guarded(async () => {
        const state = store.chat;
        const { workspaceId } = params;
        const caller = callerIn(store, workspaceId);
        const key = requireIdempotencyKey(request.headers);
        requireAction(caller, "groupManage");
        const name = requiredText(
          body?.name,
          "name",
          CHAT_LIMITS.channelGroupNameMaxLength,
        );
        const position =
          body?.position === undefined || body.position === null
            ? null
            : requiredPosition(body.position);
        return idempotentCreate(
          state,
          `${workspaceId}|${caller.userId}|chat.channel-groups.create`,
          key,
          { name, position },
          () => {
            requireUniqueGroupName(state, workspaceId, name, null);
            const group = {
              id: state.nextId("group"),
              workspaceId,
              name,
              position: position ?? nextGroupPosition(state, workspaceId),
              version: 1,
              createdAt: state.stampWrite(),
            };
            state.groups.set(group.id, group);
            return { status: 201, body: groupView(group) };
          },
        );
      });
    },
  }),

  // ─── PATCH /channel-groups/:groupId ───────────────────────────────────────

  defineMockOperation<
    GroupParams,
    {
      name?: string | null;
      position?: number | null;
      expectedVersion?: number | null;
    }
  >({
    id: "chat.channelGroups.update",
    contract: { kind: "openapi", operationId: "Chat.ChannelGroups.Update" },
    method: "PATCH",
    route: `${GROUPS}/:groupId`,
    async handle({ params, body, store }) {
      return guarded(async () => {
        const state = store.chat;
        const caller = callerIn(store, params.workspaceId);
        requireAction(caller, "groupManage");
        const name = requiredText(
          body?.name,
          "name",
          CHAT_LIMITS.channelGroupNameMaxLength,
        );
        const position = requiredPosition(body?.position);
        const expected = parseVersion(body?.expectedVersion, "expectedVersion");
        const group = groupIn(state, params.workspaceId, params.groupId);
        requireUniqueGroupName(state, params.workspaceId, name, group.id);
        requireVersion(expected, group.version);
        if (name !== group.name || position !== group.position) {
          group.name = name;
          group.position = position;
          group.version += 1;
        }
        return ok(groupView(group));
      });
    },
  }),

  // ─── DELETE /channel-groups/:groupId ──────────────────────────────────────

  defineMockOperation<GroupParams>({
    id: "chat.channelGroups.delete",
    contract: { kind: "openapi", operationId: "Chat.ChannelGroups.Delete" },
    method: "DELETE",
    route: `${GROUPS}/:groupId`,
    async handle({ params, query, store }) {
      return guarded(async () => {
        const state = store.chat;
        const caller = callerIn(store, params.workspaceId);
        requireAction(caller, "groupManage");
        const expected = parseVersion(
          optionalInteger(query.expectedVersion, "expectedVersion"),
          "expectedVersion",
        );
        const group = groupIn(state, params.workspaceId, params.groupId);
        requireVersion(expected, group.version);
        for (const space of state.spacesOfWorkspace(params.workspaceId)) {
          if (space.deletedAt === null && space.groupId === group.id) {
            space.groupId = null;
            space.version += 1;
          }
        }
        state.groups.delete(group.id);
        return { status: 204 };
      });
    },
  }),
];
