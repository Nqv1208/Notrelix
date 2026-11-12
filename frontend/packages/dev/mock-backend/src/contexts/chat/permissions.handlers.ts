/**
 * Channel permission overrides (UC-CHT-H01) and the moderator delete of another person's message (UC-CHT-H03): read, replace and
 * clear the overrides of a channel, and remove a message as a moderator with a reason. The mock mirrors the backend's order of checks;
 * the Audit facts are the backend's and have no mock surface.
 */

import { defineMockOperation } from "../../operations/types";
import { ok } from "../../transport/create-response";
import {
  archivedProblem,
  callerIn,
  fail,
  guarded,
  liveChannel,
  liveSpace,
  notFoundProblem,
  optionalInteger,
  parseVersion,
  requireAction,
  requireManager,
  requireVersion,
  requireVisible,
  ruleProblem,
  validationProblem,
} from "./chat-access";
import { CHAT_ERROR, CHAT_RULE } from "./chat-model";
import {
  PERMISSION_OVERRIDES_MAX,
  normalizeEntries,
  parseEntry,
  permissionEntriesOf,
  permissionsView,
  requireAllowsWithinRole,
} from "./chat-permissions";
import {
  announceMessageEvent,
  announcePermissionsChanged,
} from "./chat-realtime";
import {
  removeAttachmentsOf,
  removeMentionsOf,
  removePin,
  removeReactionsOf,
} from "./chat-writes";
import type { ChatCaller } from "./chat-access";
import type { ChatPermissionEntryRecord } from "./chat-permissions";
import type { ChatSpaceRecord } from "./chat-model";
import type { MockChatState } from "./chat-state";

type ChannelParams = { workspaceId: string; channelId: string };
type MessageParams = { workspaceId: string; messageId: string };
type SetBody = { entries?: unknown; expectedVersion?: unknown };
type ModerateBody = { reason?: unknown; expectedVersion?: unknown };

/** The reason of a moderator delete: 1 to 500 characters once trimmed (ChatLimits.ModerationReasonMaxLength). */
const MODERATION_REASON_MAX = 500;

/** The one path the set and the clear share (backend ChannelPermissionUpdates.ApplyAsync). */
function applyPermissions(
  state: MockChatState,
  caller: ChatCaller,
  space: ChatSpaceRecord,
  expectedVersion: number,
  requested: readonly ChatPermissionEntryRecord[],
) {
  requireVisible(state, space, caller);
  requireManager(state, space, caller);
  requireVersion(expectedVersion, space.version);
  if (space.archivedAt !== null) archivedProblem();
  requireAllowsWithinRole(requested, caller);
  const wanted = normalizeEntries(requested);
  const current = permissionEntriesOf(state, space.id);
  const same =
    current.length === wanted.length &&
    current.every(
      (entry, index) =>
        entry.subject === wanted[index]!.subject &&
        entry.action === wanted[index]!.action &&
        entry.effect === wanted[index]!.effect,
    );
  if (!same) {
    if (wanted.length === 0) state.permissionOverrides.delete(space.id);
    else state.permissionOverrides.set(space.id, wanted);
    space.version += 1;
    announcePermissionsChanged(state, space);
  }
  return ok(permissionsView(state, space));
}

export const permissionsOperations = [
  // ─── GET /channels/:channelId/permissions ─────────────────────────────────

  defineMockOperation<ChannelParams>({
    id: "chat.permissions.get",
    contract: { kind: "openapi", operationId: "Chat.Permissions.Get" },
    method: "GET",
    route: "/workspaces/:workspaceId/chat/channels/:channelId/permissions",
    async handle({ params, store }) {
      return guarded(async () => {
        const state = store.chat;
        const caller = callerIn(store, params.workspaceId);
        requireAction(caller, "viewWorkspace");
        const space = liveChannel(state, params.workspaceId, params.channelId);
        requireVisible(state, space, caller);
        return ok(permissionsView(state, space));
      });
    },
  }),

  // ─── PUT /channels/:channelId/permissions ─────────────────────────────────

  defineMockOperation<ChannelParams, SetBody>({
    id: "chat.permissions.set",
    contract: { kind: "openapi", operationId: "Chat.Permissions.Set" },
    method: "PUT",
    route: "/workspaces/:workspaceId/chat/channels/:channelId/permissions",
    async handle({ params, body, store }) {
      return guarded(async () => {
        const state = store.chat;
        const caller = callerIn(store, params.workspaceId);
        requireAction(caller, "channelManage");
        if (!Array.isArray(body?.entries)) {
          validationProblem("entries is required.");
        }
        if (body.entries.length > PERMISSION_OVERRIDES_MAX * 2) {
          validationProblem(
            `At most ${PERMISSION_OVERRIDES_MAX} overrides can be set.`,
          );
        }
        const requested = (body.entries as unknown[]).map((row) =>
          parseEntry(row, validationProblem),
        );
        const expected = parseVersion(body?.expectedVersion, "expectedVersion");
        const space = liveChannel(state, params.workspaceId, params.channelId);
        return applyPermissions(state, caller, space, expected, requested);
      });
    },
  }),

  // ─── DELETE /channels/:channelId/permissions ──────────────────────────────

  defineMockOperation<ChannelParams>({
    id: "chat.permissions.delete",
    contract: { kind: "openapi", operationId: "Chat.Permissions.Delete" },
    method: "DELETE",
    route: "/workspaces/:workspaceId/chat/channels/:channelId/permissions",
    async handle({ params, query, store }) {
      return guarded(async () => {
        const state = store.chat;
        const caller = callerIn(store, params.workspaceId);
        requireAction(caller, "channelManage");
        const expected = parseVersion(
          optionalInteger(query.expectedVersion, "expectedVersion"),
          "expectedVersion",
        );
        const space = liveChannel(state, params.workspaceId, params.channelId);
        return applyPermissions(state, caller, space, expected, []);
      });
    },
  }),

  // ─── POST /messages/:messageId/moderation ─────────────────────────────────

  defineMockOperation<MessageParams, ModerateBody>({
    id: "chat.messages.moderate",
    contract: { kind: "openapi", operationId: "Chat.Messages.Moderate" },
    method: "POST",
    route: "/workspaces/:workspaceId/chat/messages/:messageId/moderation",
    async handle({ params, body, store }) {
      return guarded(async () => {
        const state = store.chat;
        const caller = callerIn(store, params.workspaceId);
        requireAction(caller, "messageModerate");
        const reason =
          typeof body?.reason === "string" ? body.reason.trim() : "";
        if (reason.length < 1 || reason.length > MODERATION_REASON_MAX) {
          validationProblem(
            `A reason of 1 to ${MODERATION_REASON_MAX} characters is required.`,
          );
        }
        const expected = parseVersion(body?.expectedVersion, "expectedVersion");
        const message = state.messages.get(params.messageId);
        if (!message || message.workspaceId !== params.workspaceId) {
          notFoundProblem();
        }
        const space = liveSpace(state, params.workspaceId, message.spaceId);
        requireVisible(state, space, caller);
        requireVersion(expected, message.version);
        // A message that is already a tombstone changes nothing.
        if (message.deletedAt !== null) return { status: 204 };
        if (space.archivedAt !== null) archivedProblem();
        if (message.kind === "system") {
          fail(
            409,
            CHAT_ERROR.systemMessageImmutable,
            "System message",
            "A system message cannot be deleted.",
          );
        }
        if (message.authorId === caller.userId) {
          ruleProblem(
            CHAT_RULE.selfTarget,
            "Delete your own message as its author, not as a moderator.",
          );
        }
        message.content = null;
        message.deletedAt = state.stampWrite();
        message.deletedBy = caller.userId;
        message.version += 1;
        announceMessageEvent(state, space, message, "chat.message.deleted");
        removePin(state, space, message.id);
        removeReactionsOf(state, message.id);
        removeAttachmentsOf(state, message.id);
        removeMentionsOf(state, message.id);
        return { status: 204 };
      });
    },
  }),
];
