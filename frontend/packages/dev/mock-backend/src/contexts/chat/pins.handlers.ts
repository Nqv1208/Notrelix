/**
 * Pins of a space: pin a message (idempotent, a system message announces it),
 * unpin it (idempotent), and list the pins of a space, newest pin first
 * (UC-CHT-F06, F07). A space holds at most 50 pins; a pin ends with its message.
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
  pinView,
  requireAction,
  requireVisible,
  validationProblem,
} from "./chat-access";
import {
  CHAT_ERROR,
  CHAT_LIMITS,
  CHAT_SYSTEM_TEXT,
  type ChatPinRecord,
} from "./chat-model";
import { announcePinChanged } from "./chat-realtime";
import { streamIdOf } from "./chat-state";
import { appendSystemMessage, removePin } from "./chat-writes";

type MessageParams = {
  workspaceId: string;
  messageId: string;
};

type SpaceParams = {
  workspaceId: string;
  spaceId: string;
};

/** The cursor of ListPins: the pin time and the pin id of the last item, as `<epoch ms>.<id N form>`. */
function pinCursorOf(pin: ChatPinRecord): string {
  return `${Date.parse(pin.pinnedAt)}.${streamIdOf(pin.id)}`;
}

function parsePinCursor(
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

export const pinsOperations = [
  // ─── PUT /messages/:messageId/pin ─────────────────────────────────────────

  defineMockOperation<MessageParams>({
    id: "chat.pins.pin",
    contract: { kind: "openapi", operationId: "Chat.Pins.Pin" },
    method: "PUT",
    route: "/workspaces/:workspaceId/chat/messages/:messageId/pin",
    async handle({ params, store }) {
      return guarded(async () => {
        const state = store.chat;
        const caller = callerIn(store, params.workspaceId);
        requireAction(caller, "messagePin");
        const message = state.messages.get(params.messageId);
        if (!message || message.workspaceId !== params.workspaceId) {
          notFoundProblem();
        }
        const space = liveSpace(state, params.workspaceId, message.spaceId);
        if (!state.activeMembershipOf(space.id, caller.userId)) {
          notFoundProblem();
        }
        if (space.archivedAt !== null) archivedProblem();
        if (message.kind === "system") {
          fail(
            409,
            CHAT_ERROR.messageNotPinnable,
            "Message not pinnable",
            "A system message cannot be pinned.",
          );
        }
        if (message.deletedAt !== null) {
          fail(
            409,
            CHAT_ERROR.messageDeleted,
            "Message deleted",
            "A deleted message cannot be pinned.",
          );
        }
        // A message already pinned answers with its pin and changes nothing.
        const existing = state.pins.get(message.id);
        if (existing) return ok(pinView(state, existing));
        if (state.pinsOfSpace(space.id).length >= CHAT_LIMITS.pinLimit) {
          fail(
            422,
            CHAT_ERROR.limitReached,
            "Limit reached",
            `A channel holds at most ${CHAT_LIMITS.pinLimit} pins.`,
          );
        }
        const pin: ChatPinRecord = {
          id: state.nextId("pin"),
          workspaceId: params.workspaceId,
          spaceId: space.id,
          messageId: message.id,
          pinnedBy: caller.userId,
          pinnedAt: state.stampWrite(),
        };
        state.pins.set(message.id, pin);
        appendSystemMessage(
          state,
          space,
          caller.userId,
          CHAT_SYSTEM_TEXT.pinned,
        );
        announcePinChanged(state, space, message.id, "pinned");
        return { status: 201, body: pinView(state, pin) };
      });
    },
  }),

  // ─── DELETE /messages/:messageId/pin ──────────────────────────────────────

  defineMockOperation<MessageParams>({
    id: "chat.pins.unpin",
    contract: { kind: "openapi", operationId: "Chat.Pins.Unpin" },
    method: "DELETE",
    route: "/workspaces/:workspaceId/chat/messages/:messageId/pin",
    async handle({ params, store }) {
      return guarded(async () => {
        const state = store.chat;
        const caller = callerIn(store, params.workspaceId);
        requireAction(caller, "messagePin");
        const message = state.messages.get(params.messageId);
        if (!message || message.workspaceId !== params.workspaceId) {
          notFoundProblem();
        }
        const space = liveSpace(state, params.workspaceId, message.spaceId);
        if (!state.activeMembershipOf(space.id, caller.userId)) {
          notFoundProblem();
        }
        if (space.archivedAt !== null) archivedProblem();
        // A message that is not pinned is a success that changes nothing.
        removePin(state, space, message.id);
        return { status: 204 };
      });
    },
  }),

  // ─── GET /spaces/:spaceId/pins ────────────────────────────────────────────

  defineMockOperation<SpaceParams>({
    id: "chat.pins.list",
    contract: { kind: "openapi", operationId: "Chat.Pins.List" },
    method: "GET",
    route: "/workspaces/:workspaceId/chat/spaces/:spaceId/pins",
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
        const cursor = parsePinCursor(query.cursor);
        const space = liveSpace(state, params.workspaceId, params.spaceId);
        requireVisible(state, space, caller);
        const rows = state
          .pinsOfSpace(space.id)
          .filter(
            (pin) =>
              cursor === null ||
              Date.parse(pin.pinnedAt) < cursor.ms ||
              (Date.parse(pin.pinnedAt) === cursor.ms &&
                streamIdOf(pin.id) < cursor.id),
          )
          .slice(0, limit + 1);
        const page = rows.slice(0, limit);
        const last = page[page.length - 1];
        return ok({
          items: page.map((pin) => pinView(state, pin)),
          nextCursor: rows.length > limit && last ? pinCursorOf(last) : null,
        });
      });
    },
  }),
];
