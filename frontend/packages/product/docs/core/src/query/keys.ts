/**
 * @notrelix/docs-core — Docs query keys.
 *
 * Type A: CRUD server state for pages, blocks, comments.
 */

export const docsQueryKeys = {
  all: ['pages'] as const,
  tree: (workspaceId: string) => ['pages', 'tree', workspaceId] as const,
  list: (workspaceId: string) => ['pages', 'list', workspaceId] as const,
  detail: (pageId: string) => ['pages', 'detail', pageId] as const,
  breadcrumb: (pageId: string) => ['pages', 'breadcrumb', pageId] as const,
  blocks: (pageId: string) => ['pages', 'blocks', pageId] as const,
  comments: (pageId: string) => ['pages', 'comments', pageId] as const,
  history: (pageId: string) => ['pages', 'history', pageId] as const,
  search: (workspaceId: string, query: string) =>
    ['pages', 'search', workspaceId, query] as const,
  favorites: (workspaceId: string) =>
    ['pages', 'favorites', workspaceId] as const,
} as const;
