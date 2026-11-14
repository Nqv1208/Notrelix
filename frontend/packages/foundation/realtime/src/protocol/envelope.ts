/**
 * Domain event frame delivered by the realtime gateway (`type: "event"`).
 *
 * Wire shape: backend realtime gateway protocol v1 (v1.1 adds the Chat scopes).
 * Written by hand here; the payload message types of the chat and per-user
 * streams are generated from `artifacts/contracts/realtime.v1.json`.
 *
 * Most frames carry NO payload: they only tell the client that a resource in a
 * subscribed scope changed, and state is reconciled by REST invalidation and
 * refetch (FE-RT-001, FE-RT-040). The chat channel stream (`chat.message.v1`) and
 * the per-user stream (`chat.unread.v1`) carry a compact `payload`; when it did
 * not fit the frame limit `payloadOmitted` is true and the client refetches.
 *
 * Mapping from the wire frame (`parseRealtimeMessage`):
 *   occurredAt -> timestamp
 *   resourceId -> resourceId and aggregateId (alias kept for adapters)
 *   tenantId   -> not sent; `workspaceId` is the tenant scope
 */
export interface RealtimeEnvelope {
  readonly schemaVersion: number;
  readonly eventId: string;
  readonly eventType: string;
  readonly changeKind: string;
  /** Wire subscription that delivered the frame (`workspace:<id>`, `board:<id>`). */
  readonly subscriptionId: string;
  /** Per-stream ordering domain, `{namespace}:{kind}:{id:N}`. */
  readonly streamKey: string;
  /** Per-streamKey gap-free counter allocated by the server (+1 per event). */
  readonly streamVersion: number;
  readonly resourceKind: string;
  readonly resourceId: string;
  /** Alias of `resourceId`. */
  readonly aggregateId: string;
  /** Informational only; never used for ordering or gap detection. */
  readonly resourceVersion?: number;
  readonly workspaceId: string;
  readonly correlationId: string;
  /** `occurredAt` from the wire frame. */
  readonly timestamp: string;
  readonly payloadContract?: string;
  /** Compact payload of the chat and per-user streams; absent on notify-only frames. */
  readonly payload?: Readonly<Record<string, unknown>>;
  /** True when the gateway dropped the payload to fit the frame limit; refetch over REST. */
  readonly payloadOmitted?: boolean;
}
