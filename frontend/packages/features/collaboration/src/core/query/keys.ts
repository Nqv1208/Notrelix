/**
 * @notrelix/feature-collaboration — Collaboration query keys.
 *
 * Type A: CRUD server state (comments, mentions, reactions, presence, attachments).
 */

export const collaborationQueryKeys = {
  comments: (resourceId: string) =>
    ['collaboration', 'comments', resourceId] as const,
  mentions: (workspaceId: string) =>
    ['collaboration', 'mentions', workspaceId] as const,
  reactions: (resourceId: string) =>
    ['collaboration', 'reactions', resourceId] as const,
  presence: (workspaceId: string) =>
    ['collaboration', 'presence', workspaceId] as const,
  attachments: (resourceId: string) =>
    ['collaboration', 'attachments', resourceId] as const,
} as const;
