// Query key factory — single source of truth for all TanStack Query keys.
// Import from here in both hooks and server prefetch functions.

export const queryKeys = {
  auth: {
    all: ["auth"] as const,
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
    detail: (workspaceId: string) => ["workspaces", "detail", workspaceId] as const,
    snapshot: (workspaceId: string) => ["workspaces", "snapshot", workspaceId] as const,
    members: (workspaceId: string) => ["workspaces", "members", workspaceId] as const,
    views: (workspaceId: string) => ["workspaces", "views", workspaceId] as const,
    activeView: (workspaceId: string, view: string) => ["workspaces", "views", workspaceId, "active", view] as const,
    invitations: (workspaceId: string) => ["workspaces", "invitations", workspaceId] as const,
    invitationByToken: (token: string) => ["workspaces", "invitations", "by-token", token] as const,
    pendingInvitations: ["workspaces", "invitations", "pending"] as const,
    activity: (workspaceId: string) => ["workspaces", "activity", workspaceId] as const,
  },
  boards: {
    all: ["boards"] as const,
    list: (workspaceId: string) => ["boards", "list", workspaceId] as const,
    workspaceList: (workspaceId: string) => ["boards", "workspace", workspaceId] as const,
    fullBoard: (boardId: string, workspaceId?: string) => ["boards", "full", workspaceId ?? "workspace", boardId] as const,
    view: (workspaceId: string, boardId: string) => ["boards", "view", workspaceId, boardId] as const,
    groups: (workspaceId: string, boardId: string) => ["boards", "groups", workspaceId, boardId] as const,
    columns: (workspaceId: string, boardId: string) => ["boards", "columns", workspaceId, boardId] as const,
  },
  cards: {
    detail: (cardId: string) => ["cards", "detail", cardId] as const,
    updates: (cardId: string) => ["cards", "updates", cardId] as const,
    files: (cardId: string) => ["cards", "files", cardId] as const,
    comments: (cardId: string) => ["cards", "comments", cardId] as const,
    activity: (cardId: string) => ["cards", "activity", cardId] as const,
  },
  notifications: {
    all: ["notifications"] as const,
  },
} as const
