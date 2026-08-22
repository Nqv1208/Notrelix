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
  users: {
    updateProfile: "/profile",
    preferences: "/account/preferences",
    security: "/users/security",
  },
  workspaces: {
    list: "/workspaces",
    detail: (workspaceId: string) => `/workspaces/${workspaceId}`,
    invitationByToken: (token: string) =>
      `/workspaces/invitations/by-token/${token}`,
    acceptInvitation: (token: string) =>
      `/workspaces/invitations/accept/${token}`,
    pendingInvitations: "/workspaces/invitations/pending",
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
    reorder: "/blocks/reorder",
    batch: (pageId: string) => `/pages/${pageId}/blocks/batch`,
  },
  boards: {
    listByWorkspaceId: (workspaceId: string) =>
      `/workspaces/${workspaceId}/boards`,
    detail: (boardId: string) => `/boards/${boardId}`,
    full: (boardId: string) => `/boards/${boardId}/full`,
    schema: (boardId: string) => `/boards/${boardId}/schema`,
    labels: (boardId: string) => `/boards/${boardId}/labels`,
    label: (boardId: string, labelId: string) =>
      `/boards/${boardId}/labels/${labelId}`,
  },

  checklists: {
    detail: (checklistId: string) => `/checklists/${checklistId}`,
    items: (checklistId: string) => `/checklists/${checklistId}/items`,
  },
  checklistItems: {
    detail: (itemId: string) => `/checklist-items/${itemId}`,
  },
  comments: {
    detail: (commentId: string) => `/comments/${commentId}`,
  },
  notifications: {
    list: "/notifications",
    read: (id: string) => `/notifications/${id}/read`,
    readAll: "/notifications/read-all",
  },

  boardGroups: {
    create: (boardId: string) => `/boards/${boardId}/groups`,
    detail: (groupId: string) => `/board-groups/${groupId}`,
    duplicate: (groupId: string) => `/board-groups/${groupId}/duplicate`,
    reorder: (boardId: string) => `/boards/${boardId}/groups/reorder`,
    archive: (groupId: string) => `/board-groups/${groupId}/archive`,
  },
  boardItems: {
    create: (boardId: string) => `/boards/${boardId}/items`,
    detail: (itemId: string) => `/board-items/${itemId}`,
    move: (itemId: string) => `/board-items/${itemId}/move`,
    duplicate: (itemId: string) => `/board-items/${itemId}/duplicate`,
    archive: (itemId: string) => `/board-items/${itemId}/archive`,
    fieldValues: (itemId: string) => `/board-items/${itemId}/field-values`,
    fieldValue: (itemId: string, fieldId: string) => `/board-items/${itemId}/field-values/${fieldId}`,
    comments: (itemId: string) => `/board-items/${itemId}/comments`,
    attachments: (itemId: string) => `/board-items/${itemId}/attachments`,
    activity: (itemId: string) => `/board-items/${itemId}/activity`,
    checklists: (itemId: string) => `/board-items/${itemId}/checklists`,
    labels: (itemId: string) => `/board-items/${itemId}/labels`,
    label: (itemId: string, labelId: string) => `/board-items/${itemId}/labels/${labelId}`,
  },
  boardFields: {
    create: (boardId: string) => `/boards/${boardId}/fields`,
    detail: (boardId: string, fieldId: string) => `/boards/${boardId}/fields/${fieldId}`,
    reorder: (boardId: string) => `/boards/${boardId}/fields/reorder`,
  },
  boardViews: {
    detail: (boardId: string) => `/boards/${boardId}/views`,
  },

};
