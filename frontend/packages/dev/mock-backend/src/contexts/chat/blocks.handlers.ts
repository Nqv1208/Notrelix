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

export const blocksOperations = [
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
        if (body.blocked) state.blocks.add(key);
        else state.blocks.delete(key);
        return ok({ userId: body.userId, blocked: body.blocked });
      });
    },
  }),
];
