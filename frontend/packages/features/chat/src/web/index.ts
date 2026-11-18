/**
 * @notrelix/features-chat — Chat web surface: the query hooks, the workspace
 * container and its copy catalog.
 */

export {
  createChatHooks,
  flattenPages,
  type ChatHookDeps,
  type ChatHooks,
} from "./query/create-chat-hooks";
export {
  appendToNewest,
  replaceMessage,
  tombstoneMessage,
  type ChatMessagesCache,
} from "./query/message-cache";
export {
  ChatWorkspace,
  type ChatViewerRole,
  type ChatWorkspaceProps,
  type ChatDraftStore,
} from "./components/chat-workspace";
export {
  DiscussButton,
  type DiscussButtonProps,
} from "./components/chat-discuss-button";
export { chatCatalog } from "./components/chat.catalog";
export {
  en as chatCopyEn,
  vi as chatCopyVi,
  type ChatCopy,
} from "./components/chat.copy";
export { useChatCopy, fillCopy, pluralCopy } from "./components/use-chat-copy";
export {
  createChatRealtimeAdapter,
  type ChatRealtimeAdapter,
  type ChatRealtimeAdapterOptions,
  type ChatRealtimeContext,
  type ChatRealtimeEnvelope,
  type ChatRecoverySignal,
} from "./realtime/chat-realtime-adapter";
export {
  applyMessageCreated,
  applyMessageDeleted,
  applyMessageUpdated,
  applyUnreadFrame,
  lastKnownSequence,
  mergeHistoryPage,
  messageFromCreatedFrame,
  windowIsLive,
  type MessageFrameOutcome,
  type UnreadFrameOutcome,
} from "./query/chat-realtime-reducers";
export {
  useChatCalls,
  type ChatCallsController,
  type ChatCurrentCall,
  type UseChatCallsOptions,
} from "./calls/use-chat-calls";
export { ChatCallDock } from "./calls/chat-call-dock";
export {
  connectChatCallMedia,
  type ChatCallMediaConnector,
  type ChatCallMediaSession,
} from "./calls/chat-call-media";
