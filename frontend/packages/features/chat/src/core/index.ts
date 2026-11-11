/**
 * @notrelix/features-chat — Chat core: wire types, the REST service, failure
 * classification and the framework-neutral view rules. No React, no DOM.
 */

export * from "./types/chat";
export {
  createChatService,
  type ChatApiClient,
  type ChatEndpoints,
  type ChatRequestOptions,
  type ChatService,
} from "./api/chat.service";
export {
  chatFailureOf,
  isStaleFailure,
  type ChatFailure,
  type ChatFailureKind,
} from "./api/chat-failure";
export * from "./model/chat-model";
