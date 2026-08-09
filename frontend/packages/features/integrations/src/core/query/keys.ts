/**
 * @notrelix/feature-integrations — Integrations query keys.
 *
 * Type A: CRUD server state.
 */

export const integrationsQueryKeys = {
  all: ["integrations"] as const,
  connections: (workspaceId: string) =>
    ["integrations", "connections", workspaceId] as const,
  webhooks: (workspaceId: string) =>
    ["integrations", "webhooks", workspaceId] as const,
} as const;
