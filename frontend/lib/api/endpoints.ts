export const endpoints = {
  auth: {
    login: "/auth/login",
    register: "/auth/register",
    refresh: "/auth/refresh",
    logout: "/auth/logout",
    forgotPassword: "/auth/forgot-password",
    resetPassword: "/auth/reset-password",
    profile: "/auth/me",
  },
  pages: {
    list: (workspaceId: string) => `/workspaces/${workspaceId}/pages`,
    tree: (workspaceId: string) => `/workspaces/${workspaceId}/pages/tree`,
    detail: (pageId: string) => `/pages/${pageId}`,
    breadcrumb: (pageId: string) => `/pages/${pageId}/breadcrumb`,
    blocks: (pageId: string) => `/pages/${pageId}/blocks`,
    comments: (pageId: string) => `/pages/${pageId}/comments`,
    history: (pageId: string) => `/pages/${pageId}/history`,
    search: (workspaceId: string) => `/workspaces/${workspaceId}/pages/search`,
    move: (pageId: string) => `/pages/${pageId}/move`,
  },
  blocks: {
    detail: (blockId: string) => `/blocks/${blockId}`,
    batch: (pageId: string) => `/pages/${pageId}/blocks/batch`,
  },
  boards: {
    list: (workspaceId: string) => `/workspaces/${workspaceId}/boards`,
    full: (boardId: string) => `/boards/${boardId}/full`,
    view: (boardId: string) => `/boards/${boardId}/view`,
  },
  cards: {
    detail: (cardId: string) => `/cards/${cardId}`,
    move: (cardId: string) => `/cards/${cardId}/move`,
    fieldValues: (cardId: string) => `/cards/${cardId}/field-values`,
  },
};
