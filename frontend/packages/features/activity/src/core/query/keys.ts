/**
 * @notrelix/feature-activity — Activity query keys.
 *
 * Type C: Realtime invalidation.
 * Activity feed uses TanStack Query; realtime events invalidate the query.
 */

export const activityQueryKeys = {
  all: ['activity'] as const,
  workspace: (workspaceId: string) =>
    ['activity', 'workspace', workspaceId] as const,
} as const;
