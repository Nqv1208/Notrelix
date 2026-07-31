/**
 * @notrelix/realtime - WebSocket client, protocol, and event contracts
 *
 * Provides realtime communication infrastructure with typed events.
 *
 * USAGE: Instantiate RealtimeClient via the AppRuntime composition root,
 * not as a module-level singleton.
 */

export * from './protocol';
export { ReconnectPolicy, type ReconnectPolicyConfig } from './connection/reconnect-policy';
export {
  RealtimeClient,
  type RealtimeConnectionState,
  type RealtimeConnectContext,
  type RealtimeConnectionDescriptor,
  type RealtimeConnectionDescriptorProvider,
  type RealtimeSubscriptionFilter,
  type RealtimeSequenceGap,
  type RealtimeEventListener,
  type RealtimeStateListener,
  type RealtimeRecoveryListener,
  type RealtimeTransport,
  type RealtimeClientOptions,
  type WebSocketFactory,
  type WebSocketLike,
} from './transport/realtime-client';
export type { BoardEvent, BoardPatchEvent, BoardPresenceEvent } from './events';
