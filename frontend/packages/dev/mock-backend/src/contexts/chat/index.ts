/**
 * Chat context — every operation the generated contract names under
 * `/api/v1/workspaces/{workspaceId}/chat` (the operations of
 * `frontend/packages/foundation/contracts/src/generated/rest/schema.ts`).
 *
 * Plan: docs/workstreams/chat-delivery-plan.md (S1.6), docs/workstreams/specs/chat-foundation.md §15
 */

import { channelsOperations } from "./channels.handlers";
import { channelGroupsOperations } from "./channel-groups.handlers";
import { membersOperations } from "./members.handlers";
import { messagesOperations } from "./messages.handlers";
import { readStateOperations } from "./read-state.handlers";

export {
  seedChatWorld,
  CHAT_SEED_IDS,
  GENERAL_MESSAGE_COUNT,
} from "./chat-world";
export { MockChatState, chatGuid, streamIdOf } from "./chat-state";

export const chatOperations = [
  ...channelGroupsOperations,
  ...channelsOperations,
  ...membersOperations,
  ...messagesOperations,
  ...readStateOperations,
];
