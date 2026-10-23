import { notificationsQueryKeys } from "@notrelix/features-notifications";

export interface WorkspaceRecoveryPolicyOptions {
  readonly workspaceId: string;
  /** The signed-in principal's account scope (see `accountQueryKey`). */
  readonly accountId: string;
  readonly invalidateQueries: (
    keys: readonly unknown[][],
  ) => Promise<void> | void;
}

/**
 * Re-establishes authoritative state for one Workspace over REST after a
 * realtime recovery signal (subscription ack, stream gap/reset, permission
 * loss).
 *
 * The gateway sends no payload, so the whole Workspace scope is revalidated:
 * the `["workspace", workspaceId]` prefix covers every Workspace-scoped query
 * of every product owner (FE-RT-027). A revoked permission surfaces here as a
 * forbidden REST response, which the existing query error pathway renders.
 */
export async function handleWorkspaceRecovery({
  workspaceId,
  accountId,
  invalidateQueries,
}: WorkspaceRecoveryPolicyOptions): Promise<void> {
  await invalidateQueries([
    // Root of `workspaceQueryKey(workspaceId, ...)` from @notrelix/query.
    ["workspace", workspaceId],
    [...notificationsQueryKeys.unreadCount(accountId)],
  ]);
}
