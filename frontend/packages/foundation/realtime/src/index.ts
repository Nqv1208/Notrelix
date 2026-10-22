/**
 * @notrelix/realtime - WebSocket client, protocol, and event contracts
 *
 * Provides the realtime transport for the backend realtime gateway protocol
 * v1: connection lifecycle, subscribe/ack, heartbeat, reconnect, per-stream
 * checkpoints and recovery signals. Frames carry no payload; consumers
 * invalidate/refetch authoritative state.
 *
 * USAGE: Instantiate RealtimeClient via the AppRuntime composition root,
 * not as a module-level singleton.
 */

export * from "./protocol";
export {
  ReconnectPolicy,
  type ReconnectPolicyConfig,
} from "./connection/reconnect-policy";
export {
  RealtimeClient,
  REALTIME_ENDPOINT_PATH,
  resolveRealtimeEndpointUrl,
  type RealtimeConnectionState,
  type RealtimeConnectContext,
  type RealtimeConnectionDescriptor,
  type RealtimeConnectionDescriptorProvider,
  type RealtimeRecoveryReason,
  type RealtimeRecoverySignal,
  type RealtimeEventListener,
  type RealtimeStateListener,
  type RealtimeRecoveryListener,
  type RealtimeTransport,
  type RealtimeClientOptions,
  type WebSocketFactory,
  type WebSocketLike,
} from "./transport/realtime-client";
export type { BoardEvent, BoardPatchEvent, BoardPresenceEvent } from "./events";
