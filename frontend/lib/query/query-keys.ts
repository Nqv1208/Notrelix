// Query key factory — single source of truth for all TanStack Query keys.
// Import from here in both hooks and server prefetch functions.

export const queryKeys = {
  auth: {
    profile: ["auth", "profile"] as const,
  },
  pages: {
    all: ["pages"] as const,
    tree: (workspaceId: string) => ["pages", "tree", workspaceId] as const,
    list: (workspaceId: string) => ["pages", "list", workspaceId] as const,
    detail: (pageId: string) => ["pages", "detail", pageId] as const,
    breadcrumb: (pageId: string) => ["pages", "breadcrumb", pageId] as const,
    blocks: (pageId: string) => ["pages", "blocks", pageId] as const,
    comments: (pageId: string) => ["pages", "comments", pageId] as const,
    history: (pageId: string) => ["pages", "history", pageId] as const,
    search: (workspaceId: string, query: string) =>
      ["pages", "search", workspaceId, query] as const,
    favorites: (workspaceId: string) => ["pages", "favorites", workspaceId] as const,
  },
  workspaces: {
    all: ["workspaces"] as const,
    detail: (slug: string) => ["workspaces", "detail", slug] as const,
    snapshot: (slug: string) => ["workspaces", "snapshot", slug] as const,
    members: (workspaceId: string) => ["workspaces", "members", workspaceId] as const,
    views: (slug: string) => ["workspaces", "views", slug] as const,
    activeView: (slug: string, view: string) => ["workspaces", "views", slug, "active", view] as const,
  },
  boards: {
    all: ["boards"] as const,
    list: (workspaceId: string) => ["boards", "list", workspaceId] as const,
    fullBoard: (boardId: string) => ["boards", "full", boardId] as const,
    view: (boardId: string) => ["boards", "view", boardId] as const,
  },
  cards: {
    detail: (cardId: string) => ["cards", "detail", cardId] as const,
    comments: (cardId: string) => ["cards", "comments", cardId] as const,
    activity: (cardId: string) => ["cards", "activity", cardId] as const,
  },
} as const
