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
  type ChatUploadOptions,
  type ChatUploadProgress,
  type ChatService,
} from "./api/chat.service";
export {
  chatFailureOf,
  isStaleFailure,
  type ChatFailure,
  type ChatFailureKind,
} from "./api/chat-failure";
export * from "./model/chat-model";
export * from "./realtime/chat-frames";
export * from "./model/conversation-model";
export * from "./model/mention-model";
export * from "./model/ephemeral-model";
export * from "./model/attachment-model";
export * from "./model/permission-model";
export * from "./model/emoji-data";
export * from "./model/emoji-model";
