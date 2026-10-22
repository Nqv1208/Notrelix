/**
 * Domain event frame delivered by the realtime gateway (`type: "event"`).
 *
 * Wire shape: backend realtime gateway protocol v1, written by hand here. The
 * generated `artifacts/contracts/realtime.v1.json` stub is NOT the producer of
 * this type (the contract-owner decision is still open).
 *
 * The gateway sends NO payload. A frame only tells the client that a resource
 * in a subscribed scope changed; state is reconciled by REST invalidation and
 * refetch (FE-RT-001, FE-RT-040).
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
}
