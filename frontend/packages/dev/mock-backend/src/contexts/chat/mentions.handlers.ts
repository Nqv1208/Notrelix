/**
 * The mention picker of a space (UC-CHT-G01, G02, G03): the people a message can mention, whether the caller may broadcast, and the
 * member count above which a broadcast needs confirmation. Mentions themselves travel with a message (messages.handlers).
 */

import { defineMockOperation } from "../../operations/types";
import { ok } from "../../transport/create-response";
import {
  callerIn,
  isConversationKind,
  guarded,
  liveSpace,
  notFoundProblem,
  requireAction,
} from "./chat-access";
import { CHAT_LIMITS } from "./chat-model";
import { canBroadcast } from "./chat-mentions";

type SpaceParams = {
  workspaceId: string;
  spaceId: string;
};

export const mentionsOperations = [
  // ─── GET /spaces/:spaceId/mentionables ────────────────────────────────────

  defineMockOperation<SpaceParams>({
    id: "chat.mentions.listMentionables",
    contract: {
      kind: "openapi",
      operationId: "Chat.Mentions.ListMentionables",
    },
    method: "GET",
    route: "/workspaces/:workspaceId/chat/spaces/:spaceId/mentionables",
    async handle({ params, store }) {
      return guarded(async () => {
        const state = store.chat;
        const caller = callerIn(store, params.workspaceId);
        requireAction(caller, "viewWorkspace");
        const space = liveSpace(state, params.workspaceId, params.spaceId);
        // Only an active member of the space composes there; anyone else gets the NotFound of a missing space.
        if (!state.activeMembershipOf(space.id, caller.userId)) {
          notFoundProblem();
        }
        const inSpace = state
          .membershipsOfSpace(space.id)
          .filter((row) => row.leftAt === null)
          .map((row) => row.userId)
          .sort((a, b) => (a < b ? -1 : a > b ? 1 : 0));
        const people = inSpace.map((userId) => ({ userId, inSpace: true }));
        // A conversation lists its participants only: the Workspace directory is a channel composer feature (COL-030).
        if (caller.role !== "Guest" && !isConversationKind(space.kind)) {
          // The rest of the Workspace, not Guests (the backend lists the active non-Guest colleagues), marked as not in the space.
          const inSpaceSet = new Set(inSpace);
          const others = store
            .getWorkspaceMembers(params.workspaceId)
            .filter(
              (row) => row.role !== "guest" && !inSpaceSet.has(row.userId),
            )
            .map((row) => row.userId)
            .sort((a, b) => (a < b ? -1 : a > b ? 1 : 0));
          for (const userId of others) people.push({ userId, inSpace: false });
        }
        return ok({
          people: people.slice(0, CHAT_LIMITS.mentionablesMax),
          canBroadcast:
            canBroadcast(state, space, caller) &&
            inSpace.length > 1 &&
            space.kind !== "direct",
          broadcastConfirmAbove: CHAT_LIMITS.broadcastConfirmThreshold,
          memberCount: inSpace.length,
        });
      });
    },
  }),
];
