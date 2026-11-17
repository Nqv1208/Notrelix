/**
 * Voice and video calls of a space. The calls UI is not built, so the mock holds no call: the list is empty, there is no
 * active call (204), and every call-id operation answers the NotFound of a call that does not exist. The mock grows real
 * call state with the call UI (Phase 5c).
 */

import type { operations } from "@notrelix/contracts";
import { defineMockOperation } from "../../operations/types";
import { noContent, ok } from "../../transport/create-response";
import {
  callerIn,
  guarded,
  liveSpace,
  notFoundProblem,
  requireAction,
} from "./chat-access";

type SpaceParams = { workspaceId: string; spaceId: string };
type CallParams = { workspaceId: string; callId: string };

const SPACE_ROUTE = "/workspaces/:workspaceId/chat/spaces/:spaceId/calls";
const CALL_ROUTE = "/workspaces/:workspaceId/chat/calls/:callId";

/** A call-id operation: the call never exists in the mock. */
function missingCall(
  id: string,
  operationId: keyof operations,
  method: "POST" | "PUT",
  path: string,
) {
  return defineMockOperation<CallParams>({
    id,
    contract: { kind: "openapi", operationId },
    method,
    route: `${CALL_ROUTE}/${path}`,
    async handle({ params, store }) {
      return guarded(async () => {
        requireAction(callerIn(store, params.workspaceId), "viewWorkspace");
        return notFoundProblem();
      });
    },
  });
}

export const callsOperations = [
  defineMockOperation<SpaceParams>({
    id: "chat.calls.list",
    contract: { kind: "openapi", operationId: "Chat.Calls.List" },
    method: "GET",
    route: SPACE_ROUTE,
    async handle({ params, store }) {
      return guarded(async () => {
        const caller = callerIn(store, params.workspaceId);
        requireAction(caller, "viewWorkspace");
        liveSpace(store.chat, params.workspaceId, params.spaceId);
        return ok({ items: [], nextCursor: null });
      });
    },
  }),

  defineMockOperation<SpaceParams>({
    id: "chat.calls.start",
    contract: { kind: "openapi", operationId: "Chat.Calls.Start" },
    method: "POST",
    route: SPACE_ROUTE,
    async handle({ params, store }) {
      return guarded(async () => {
        const caller = callerIn(store, params.workspaceId);
        requireAction(caller, "viewWorkspace");
        liveSpace(store.chat, params.workspaceId, params.spaceId);
        return notFoundProblem();
      });
    },
  }),

  defineMockOperation<SpaceParams>({
    id: "chat.calls.getActive",
    contract: { kind: "openapi", operationId: "Chat.Calls.GetActive" },
    method: "GET",
    route: `${SPACE_ROUTE}/active`,
    async handle({ params, store }) {
      return guarded(async () => {
        const caller = callerIn(store, params.workspaceId);
        requireAction(caller, "viewWorkspace");
        liveSpace(store.chat, params.workspaceId, params.spaceId);
        return noContent();
      });
    },
  }),

  missingCall("chat.calls.answer", "Chat.Calls.Answer", "POST", "answer"),
  missingCall("chat.calls.decline", "Chat.Calls.Decline", "POST", "decline"),
  missingCall("chat.calls.cancel", "Chat.Calls.Cancel", "POST", "cancel"),
  missingCall("chat.calls.end", "Chat.Calls.End", "POST", "end"),
  missingCall("chat.calls.join", "Chat.Calls.Join", "POST", "join"),
  missingCall("chat.calls.leave", "Chat.Calls.Leave", "POST", "leave"),
  missingCall(
    "chat.calls.updateMediaState",
    "Chat.Calls.UpdateMediaState",
    "PUT",
    "participants/me",
  ),
];
