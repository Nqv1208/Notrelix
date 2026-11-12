/**
 * Listener-side subscription filter.
 *
 * `workspaceId` is always required (tenant guard, FE-RT-028). The scope is
 * chosen by the first id present, in this order: `boardId` (board stream),
 * `channelId` (Chat channel stream, protocol v1.1), `userId` (the caller's own
 * per-user stream, protocol v1.1); otherwise the Workspace stream. The wire
 * `subscriptionId` is derived by the client (`workspace:<id>`, `board:<id>`,
 * `channel:<id>`, `user:<id>`) and shared by all listeners of the same scope;
 * it is not a component identity (FE-RT-029).
 */
export interface RealtimeSubscriptionFilter {
  readonly workspaceId: string;
  readonly boardId?: string;
  /** A Chat channel (space) the caller is a member of. Subscribe only the active one. */
  readonly channelId?: string;
  /** The caller's own per-user stream (unread and mention counters). */
  readonly userId?: string;
  readonly eventTypes?: readonly string[];
}
