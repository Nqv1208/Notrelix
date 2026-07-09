/**
 * @notrelix/wm-core — Work Management query keys.
 *
 * Type B data keys for boards, cards, and work management state.
 * These will eventually move to wm-state once the state package matures.
 */

export const wmQueryKeys = {
  all: ['boards'] as const,
  list: (workspaceId: string) => ['boards', 'list', workspaceId] as const,
  workspaceList: (workspaceId: string) =>
    ['boards', 'workspace', workspaceId] as const,
  fullBoard: (boardId: string, workspaceId?: string) =>
    ['boards', 'full', workspaceId ?? 'workspace', boardId] as const,
  view: (workspaceId: string, boardId: string) =>
    ['boards', 'view', workspaceId, boardId] as const,
  groups: (workspaceId: string, boardId: string) =>
    ['boards', 'groups', workspaceId, boardId] as const,
  columns: (workspaceId: string, boardId: string) =>
    ['boards', 'columns', workspaceId, boardId] as const,
  cardDetail: (cardId: string) => ['cards', 'detail', cardId] as const,
  cardUpdates: (cardId: string) => ['cards', 'updates', cardId] as const,
  cardFiles: (cardId: string) => ['cards', 'files', cardId] as const,
  cardComments: (cardId: string) => ['cards', 'comments', cardId] as const,
  cardActivity: (cardId: string) => ['cards', 'activity', cardId] as const,
} as const;
