/**
 * @notrelix/realtime — WebSocket client and event contracts
 * 
 * Provides realtime communication infrastructure with typed events.
 */

export {
  RealtimeClient,
  type RealtimeEvent,
  type RealtimeEnvelope,
  type RealtimeConnectionState,
  type ConnectionStateListener,
  type EventListener,
} from './transport/realtime-client';
export type { BoardEvent, BoardPatchEvent, BoardPresenceEvent } from './events';
