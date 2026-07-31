/**
 * @notrelix/feature-search — Search query keys.
 *
 * Type A: CRUD server state.
 */

export const searchQueryKeys = {
  global: (workspaceId: string, query: string) =>
    ['search', 'global', workspaceId, query] as const,
  recent: ['search', 'recent'] as const,
} as const;
