/**
 * @notrelix/feature-workspace — Workspace query keys.
 *
 * Type A: CRUD server state.
 */

export const workspaceQueryKeys = {
  all: ['workspaces'] as const,
  detail: (workspaceId: string) =>
    ['workspaces', 'detail', workspaceId] as const,
  snapshot: (workspaceId: string) =>
    ['workspaces', 'snapshot', workspaceId] as const,
  members: (workspaceId: string) =>
    ['workspaces', 'members', workspaceId] as const,
  views: (workspaceId: string) =>
    ['workspaces', 'views', workspaceId] as const,
  activeView: (workspaceId: string, view: string) =>
    ['workspaces', 'views', workspaceId, 'active', view] as const,
  invitations: (workspaceId: string) =>
    ['workspaces', 'invitations', workspaceId] as const,
  invitationByToken: (token: string) =>
    ['workspaces', 'invitations', 'by-token', token] as const,
  pendingInvitations: ['workspaces', 'invitations', 'pending'] as const,
  activity: (workspaceId: string) =>
    ['workspaces', 'activity', workspaceId] as const,
} as const;
