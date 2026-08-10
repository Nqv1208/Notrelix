export interface WorkspaceRecoveryPolicyOptions {
  readonly workspaceId: string;
  readonly invalidateQueries: (
    keys: readonly unknown[][],
  ) => Promise<void> | void;
}

export async function handleWorkspaceRecovery({
  workspaceId,
  invalidateQueries,
}: WorkspaceRecoveryPolicyOptions): Promise<void> {
  await invalidateQueries([
    ["workspaces", workspaceId],
    ["workspaces", workspaceId, "members"],
    ["workspaces", workspaceId, "abilities"],
    ["notifications", "unread-count"],
  ]);
}
