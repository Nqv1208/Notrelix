/**
 * Blocking a person for the caller (UC-CHT-E06). A block stops a new direct conversation between the two people from either side
 * (404); it does not hide channel messages.
 */

import { defineMockOperation } from "../../operations/types";
import { ok } from "../../transport/create-response";
import {
  callerIn,
  guarded,
  notFoundProblem,
  requireAction,
  ruleProblem,
  validationProblem,
} from "./chat-access";
import { CHAT_RULE } from "./chat-model";
import type { MockChatState } from "./chat-state";

/** True when both people hold an active membership in one live space of the Workspace (a Guest reaches people only so). */
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

export const blocksOperations = [
  // ─── GET /me/blocks ───────────────────────────────────────────────────────
  // The caller's own blocks, newest first. A Guest sees only the blocks of people they share a space with.
  defineMockOperation<{ workspaceId: string }>({
    id: "chat.blocks.list",
    contract: { kind: "openapi", operationId: "Chat.Blocks.List" },
    method: "GET",
    route: "/workspaces/:workspaceId/chat/me/blocks",
    async handle({ params, store }) {
      return guarded(async () => {
        const state = store.chat;
        const caller = callerIn(store, params.workspaceId);
        requireAction(caller, "viewWorkspace");
        const prefix = `${params.workspaceId}|${caller.userId}|`;
        const rows = [...state.blocks.entries()]
          .filter(([key]) => key.startsWith(prefix))
          .map(([key, blockedAt]) => ({
            userId: key.slice(prefix.length),
            blockedAt,
          }))
          .filter(
            (row) =>
              caller.role !== "Guest" ||
              sharesSpace(state, params.workspaceId, caller.userId, row.userId),
          )
          .sort(
            (a, b) =>
              Date.parse(b.blockedAt) - Date.parse(a.blockedAt) ||
              a.userId.localeCompare(b.userId),
          );
        return ok(rows);
      });
    },
  }),

  defineMockOperation<
    { workspaceId: string },
    { userId?: string | null; blocked?: boolean | null }
  >({
    id: "chat.blocks.set",
    contract: { kind: "openapi", operationId: "Chat.Blocks.Set" },
    method: "PUT",
    route: "/workspaces/:workspaceId/chat/me/blocks",
    async handle({ params, body, store }) {
      return guarded(async () => {
        const state = store.chat;
        const caller = callerIn(store, params.workspaceId);
        if (typeof body?.userId !== "string" || body.userId === "") {
          validationProblem("userId is required.");
        }
        if (typeof body.blocked !== "boolean") {
          validationProblem("blocked is required.");
        }
        requireAction(caller, "viewWorkspace");
        if (body.userId === caller.userId) {
          ruleProblem(
            CHAT_RULE.selfTarget,
            "A person cannot block themselves.",
          );
        }
        const isMember = store
          .getWorkspaceMembers(params.workspaceId)
          .some((row) => row.userId === body.userId);
        if (!isMember) notFoundProblem();
        const key = `${params.workspaceId}|${caller.userId}|${body.userId}`;
        if (body.blocked) {
          if (!state.blocks.has(key)) state.blocks.set(key, state.stampWrite());
        } else state.blocks.delete(key);
        return ok({ userId: body.userId, blocked: body.blocked });
      });
    },
  }),
];
