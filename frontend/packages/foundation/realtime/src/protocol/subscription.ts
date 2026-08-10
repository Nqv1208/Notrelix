export interface RealtimeSubscriptionFilter {
  readonly workspaceId: string;
  readonly eventTypes?: readonly string[];
  readonly subscriptionId?: string;
}
