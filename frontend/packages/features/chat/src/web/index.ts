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
} from "./components/chat-workspace";
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
