/**
 * Voice and video calls of a space (Chat.Calls.*). The mock holds real call state (`chat-calls.ts`): start with an
 * Idempotency-Key, the ringing or active call, answer, decline, cancel, end, join, leave and the caller's media state,
 * each guarded by the call version (a stale one is 412), with the call frames and the one call line in the timeline.
 * Access mirrors the backend: a person outside the space gets the NotFound of a missing call.
 */

import type { operations } from "@notrelix/contracts";
import { defineMockOperation } from "../../operations/types";
import { noContent, ok } from "../../transport/create-response";
import {
  callerIn,
  guarded,
  liveSpace,
  notFoundProblem,
  parseVersion,
  requireAction,
  requireIdempotencyKey,
  validationProblem,
} from "./chat-access";
import {
  answerCall,
  callView,
  cancelCall,
  declineCall,
  endCall,
  joinCall,
  leaveCall,
  liveCallOf,
  startCall,
  updateCallMedia,
  type MockCallKind,
} from "./chat-calls";
import { idempotentCreate } from "./chat-writes";

type SpaceParams = { workspaceId: string; spaceId: string };
type CallParams = { workspaceId: string; callId: string };
type VersionBody = { expectedVersion?: number | null };

const SPACE_ROUTE = "/workspaces/:workspaceId/chat/spaces/:spaceId/calls";
const CALL_ROUTE = "/workspaces/:workspaceId/chat/calls/:callId";

type CallCommand = typeof answerCall;

/** A call-id operation: the caller must be in the call's space; the version is checked first, then the call's own rules. */
function callCommand(
  id: string,
  operationId: keyof operations,
  path: string,
  run: CallCommand,
) {
  return defineMockOperation<CallParams, VersionBody>({
    id,
    contract: { kind: "openapi", operationId },
    method: "POST",
    route: `${CALL_ROUTE}/${path}`,
    async handle({ params, body, store }) {
      return guarded(async () => {
        const caller = callerIn(store, params.workspaceId);
        requireAction(caller, "viewWorkspace");
        const expected = parseVersion(body?.expectedVersion, "expectedVersion");
        const result = run(
          store,
          params.workspaceId,
          params.callId,
          caller.userId,
          expected,
        );
        return { status: result.status, body: result.body };
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
        const state = store.chat;
        liveSpace(state, params.workspaceId, params.spaceId);
        if (!state.activeMembershipOf(params.spaceId, caller.userId)) {
          notFoundProblem();
        }
        const items = Array.from(state.calls.values())
          .filter((call) => call.spaceId === params.spaceId)
          .sort((a, b) => b.startedAt.localeCompare(a.startedAt))
          .map(callView);
        return ok({ items, nextCursor: null });
      });
    },
  }),

  defineMockOperation<SpaceParams, { kind?: string | null }>({
    id: "chat.calls.start",
    contract: { kind: "openapi", operationId: "Chat.Calls.Start" },
    method: "POST",
    route: SPACE_ROUTE,
    async handle({ params, body, request, store }) {
      return guarded(async () => {
        const caller = callerIn(store, params.workspaceId);
        requireAction(caller, "messagePost");
        const key = requireIdempotencyKey(request.headers);
        const kind = body?.kind;
        if (kind !== "voice" && kind !== "video") {
          validationProblem("kind is required: voice or video.");
        }
        return idempotentCreate(
          store.chat,
          `${params.workspaceId}|${caller.userId}|chat.calls.start`,
          key,
          { spaceId: params.spaceId, kind },
          () =>
            startCall(
              store,
              params.workspaceId,
              params.spaceId,
              caller.userId,
              kind as MockCallKind,
            ),
        );
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
        const state = store.chat;
        liveSpace(state, params.workspaceId, params.spaceId);
        if (!state.activeMembershipOf(params.spaceId, caller.userId)) {
          notFoundProblem();
        }
        const call = liveCallOf(state, params.spaceId);
        return call ? ok(callView(call)) : noContent();
      });
    },
  }),

  callCommand("chat.calls.answer", "Chat.Calls.Answer", "answer", answerCall),
  callCommand(
    "chat.calls.decline",
    "Chat.Calls.Decline",
    "decline",
    declineCall,
  ),
  callCommand("chat.calls.cancel", "Chat.Calls.Cancel", "cancel", cancelCall),
  callCommand("chat.calls.end", "Chat.Calls.End", "end", endCall),
  callCommand("chat.calls.join", "Chat.Calls.Join", "join", joinCall),
  callCommand("chat.calls.leave", "Chat.Calls.Leave", "leave", leaveCall),

  defineMockOperation<
    CallParams,
    {
      audioOn?: boolean;
      videoOn?: boolean;
      sharingScreen?: boolean;
      expectedVersion?: number | null;
    }
  >({
    id: "chat.calls.updateMediaState",
    contract: { kind: "openapi", operationId: "Chat.Calls.UpdateMediaState" },
    method: "PUT",
    route: `${CALL_ROUTE}/participants/me`,
    async handle({ params, body, store }) {
      return guarded(async () => {
        const caller = callerIn(store, params.workspaceId);
        requireAction(caller, "viewWorkspace");
        const expected = parseVersion(body?.expectedVersion, "expectedVersion");
        const result = updateCallMedia(
          store,
          params.workspaceId,
          params.callId,
          caller.userId,
          expected,
          {
            audioOn: body?.audioOn ?? false,
            videoOn: body?.videoOn ?? false,
            sharingScreen: body?.sharingScreen ?? false,
          },
        );
        return { status: result.status, body: result.body };
      });
    },
  }),
];
