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
  type RealtimeEvent,
  type RealtimeEnvelope,
  type RealtimeConnectionState,
  type ConnectionStateListener,
  type EventListener,
} from './transport/realtime-client';
export type { BoardEvent, BoardPatchEvent, BoardPresenceEvent } from './events';
