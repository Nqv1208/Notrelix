/**
 * @notrelix/realtime — WebSocket client and event contracts
 * 
 * Provides realtime communication infrastructure with typed events.
 */

export { RealtimeClient, type RealtimeEvent } from './transport/realtime-client'
export type { BoardEvent, BoardPatchEvent, BoardPresenceEvent } from './events'
