export const endpoints = {
  auth: {
    login: "/auth/login",
    register: "/auth/register",
    refresh: "/auth/refresh",
    csrf: "/auth/csrf",
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
    // Per-member navigation state (WX-BE-001/002): references only.
    recents: (workspaceId: string) => `/workspaces/${workspaceId}/recents`,
    pins: (workspaceId: string) => `/workspaces/${workspaceId}/pins`,
    pin: (workspaceId: string, kind: string, resourceId: string) =>
      `/workspaces/${workspaceId}/pins/${encodeURIComponent(kind)}/${resourceId}`,
    // Workspace profile, settings and lifecycle (producer: Workspaces.Workspaces.*).
    profile: (workspaceId: string) => `/workspaces/${workspaceId}/profile`,
    settings: (workspaceId: string) => `/workspaces/${workspaceId}/settings`,
    archive: (workspaceId: string) => `/workspaces/${workspaceId}/archive`,
    activity: (workspaceId: string) => `/workspaces/${workspaceId}/activity`,
    // Members (Workspaces.Members.*). Inviting is POST on the members route.
    members: (workspaceId: string) => `/workspaces/${workspaceId}/members`,
    member: (workspaceId: string, userId: string) =>
      `/workspaces/${workspaceId}/members/${userId}`,
    suspendMember: (workspaceId: string, userId: string) =>
      `/workspaces/${workspaceId}/members/${userId}/suspend`,
    activateMember: (workspaceId: string, userId: string) =>
      `/workspaces/${workspaceId}/members/${userId}/activate`,
    // Invitations (Workspaces.Invitations.*).
    invitations: (workspaceId: string) =>
      `/workspaces/${workspaceId}/invitations`,
    invitation: (workspaceId: string, invitationId: string) =>
      `/workspaces/${workspaceId}/invitations/${invitationId}`,
    // My Work read (WorkManagement.BoardItems.ListMyWork, WX-BE-005).
    myWorkItems: (workspaceId: string) =>
      `/workspaces/${workspaceId}/my-work/items`,
    // Space org tree (Workspaces.Spaces.*, WSP-021..025, OT-4).
    spaces: (workspaceId: string) => `/workspaces/${workspaceId}/spaces`,
    spaceTree: (workspaceId: string) =>
      `/workspaces/${workspaceId}/spaces/tree`,
    space: (workspaceId: string, spaceId: string) =>
      `/workspaces/${workspaceId}/spaces/${spaceId}`,
    spaceDescription: (workspaceId: string, spaceId: string) =>
      `/workspaces/${workspaceId}/spaces/${spaceId}/description`,
    spaceVisibility: (workspaceId: string, spaceId: string) =>
      `/workspaces/${workspaceId}/spaces/${spaceId}/visibility`,
    spaceType: (workspaceId: string, spaceId: string) =>
      `/workspaces/${workspaceId}/spaces/${spaceId}/type`,
    spaceArchive: (workspaceId: string, spaceId: string) =>
      `/workspaces/${workspaceId}/spaces/${spaceId}/archive`,
    spaceUnarchive: (workspaceId: string, spaceId: string) =>
      `/workspaces/${workspaceId}/spaces/${spaceId}/unarchive`,
    spaceMove: (workspaceId: string, spaceId: string) =>
      `/workspaces/${workspaceId}/spaces/${spaceId}/move`,
    spaceMembers: (workspaceId: string, spaceId: string) =>
      `/workspaces/${workspaceId}/spaces/${spaceId}/members`,
    spaceMember: (
      workspaceId: string,
      spaceId: string,
      subjectType: string,
      subjectId: string,
    ) =>
      `/workspaces/${workspaceId}/spaces/${spaceId}/members/${subjectType}/${subjectId}`,
    // Teams (Workspaces.Teams.*), read as Space member subjects.
    teams: (workspaceId: string) => `/workspaces/${workspaceId}/teams`,
    team: (workspaceId: string, teamId: string) =>
      `/workspaces/${workspaceId}/teams/${teamId}`,
    teamDescription: (workspaceId: string, teamId: string) =>
      `/workspaces/${workspaceId}/teams/${teamId}/description`,
    teamArchive: (workspaceId: string, teamId: string) =>
      `/workspaces/${workspaceId}/teams/${teamId}/archive`,
    teamUnarchive: (workspaceId: string, teamId: string) =>
      `/workspaces/${workspaceId}/teams/${teamId}/unarchive`,
    teamMembers: (workspaceId: string, teamId: string) =>
      `/workspaces/${workspaceId}/teams/${teamId}/members`,
    teamMember: (workspaceId: string, teamId: string, userId: string) =>
      `/workspaces/${workspaceId}/teams/${teamId}/members/${userId}`,
    teamMemberRole: (workspaceId: string, teamId: string, userId: string) =>
      `/workspaces/${workspaceId}/teams/${teamId}/members/${userId}/role`,
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
    moveToSpace: (pageId: string) => `/pages/${pageId}/move-to-space`,
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
    access: (boardId: string) => `/boards/${boardId}/access`,
    schema: (boardId: string) => `/boards/${boardId}/schema`,
    labels: (boardId: string) => `/boards/${boardId}/labels`,
    label: (boardId: string, labelId: string) =>
      `/boards/${boardId}/labels/${labelId}`,
    moveToSpace: (boardId: string) => `/boards/${boardId}/move-to-space`,
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
    move: (groupId: string) => `/board-groups/${groupId}/move`,
    archive: (groupId: string) => `/board-groups/${groupId}/archive`,
  },
  boardItems: {
    create: (boardId: string) => `/boards/${boardId}/items`,
    detail: (itemId: string) => `/board-items/${itemId}`,
    move: (itemId: string) => `/board-items/${itemId}/move`,
    duplicate: (itemId: string) => `/board-items/${itemId}/duplicate`,
    archive: (itemId: string) => `/board-items/${itemId}/archive`,
    complete: (itemId: string) => `/board-items/${itemId}/complete`,
    status: (itemId: string) => `/board-items/${itemId}/status`,
    fieldValues: (itemId: string) => `/board-items/${itemId}/field-values`,
    fieldValue: (itemId: string, fieldId: string) =>
      `/board-items/${itemId}/field-values/${fieldId}`,
    comments: (itemId: string) => `/board-items/${itemId}/comments`,
    attachments: (itemId: string) => `/board-items/${itemId}/attachments`,
    attachmentFiles: (itemId: string) =>
      `/board-items/${itemId}/attachments/files`,
    attachmentContent: (itemId: string, attachmentId: string) =>
      `/board-items/${itemId}/attachments/${attachmentId}/content`,
    activity: (itemId: string) => `/board-items/${itemId}/activity`,
    checklists: (itemId: string) => `/board-items/${itemId}/checklists`,
    labels: (itemId: string) => `/board-items/${itemId}/labels`,
    label: (itemId: string, labelId: string) =>
      `/board-items/${itemId}/labels/${labelId}`,
  },
  boardFields: {
    create: (boardId: string) => `/boards/${boardId}/fields`,
    detail: (boardId: string, fieldId: string) =>
      `/boards/${boardId}/fields/${fieldId}`,
    move: (boardId: string, fieldId: string) =>
      `/boards/${boardId}/fields/${fieldId}/move`,
    options: (boardId: string, fieldId: string) =>
      `/boards/${boardId}/fields/${fieldId}/options`,
    option: (boardId: string, fieldId: string, optionId: string) =>
      `/boards/${boardId}/fields/${fieldId}/options/${optionId}`,
  },
  boardViews: {
    detail: (boardId: string) => `/boards/${boardId}/views`,
  },
};
