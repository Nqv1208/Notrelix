export interface RealtimeEnvelope<TPayload = unknown> {
  readonly schemaVersion: number;
  readonly eventId: string;
  readonly eventType: string;
  readonly workspaceId: string;
  readonly correlationId: string;
  readonly timestamp: string;
  readonly aggregateVersion?: number;
  readonly sequence?: number;
  readonly subscriptionId?: string;
  readonly tenantId?: string;
  readonly aggregateId?: string;
  readonly causationId?: string;
  readonly payload: TPayload;
}
