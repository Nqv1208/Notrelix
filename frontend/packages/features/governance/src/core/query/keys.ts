/**
 * @notrelix/feature-governance — Governance query keys.
 *
 * Type A: CRUD server state.
 */

export const governanceQueryKeys = {
  all: ["governance"] as const,
  roles: (workspaceId: string) => ["governance", "roles", workspaceId] as const,
  permissions: (workspaceId: string) =>
    ["governance", "permissions", workspaceId] as const,
  auditLogs: (workspaceId: string) =>
    ["governance", "audit-logs", workspaceId] as const,
} as const;
