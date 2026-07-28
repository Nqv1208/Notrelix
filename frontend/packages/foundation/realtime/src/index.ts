/**
 * @notrelix/realtime - WebSocket client, protocol, and event contracts
 *
 * Provides realtime communication infrastructure with typed events.
 *
 * USAGE: Instantiate RealtimeClient via the AppRuntime composition root,
 * not as a module-level singleton.
 */

export * from './protocol';
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
} from './transport/realtime-client';
export type { BoardEvent, BoardPatchEvent, BoardPresenceEvent } from './events';
