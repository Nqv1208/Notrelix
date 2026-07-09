/**
 * @notrelix/feature-billing — Billing query keys.
 *
 * Type A: CRUD server state.
 */

export const billingQueryKeys = {
  all: ['billing'] as const,
  subscription: (workspaceId: string) =>
    ['billing', 'subscription', workspaceId] as const,
  invoices: (workspaceId: string) =>
    ['billing', 'invoices', workspaceId] as const,
  entitlements: (workspaceId: string) =>
    ['billing', 'entitlements', workspaceId] as const,
  usage: (workspaceId: string) =>
    ['billing', 'usage', workspaceId] as const,
} as const;
