/**
 * Listener-side subscription filter.
 *
 * `workspaceId` is always required (tenant guard, FE-RT-028). `boardId`
 * narrows the server subscription to a board stream; without it the client
 * subscribes the workspace stream. The wire `subscriptionId` is derived by the
 * client (`workspace:<id>` / `board:<id>`) and shared by all listeners of the
 * same scope; it is not a component identity (FE-RT-029).
 */
export interface RealtimeSubscriptionFilter {
  readonly workspaceId: string;
  readonly boardId?: string;
  readonly eventTypes?: readonly string[];
}
