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
    bootstrap: "/auth/bootstrap",
    changePassword: "/auth/change-password",
    // Identity.Security.* / Identity.Sessions.* / Identity.Mfa.*
    securitySummary: "/auth/security/summary",
    securityActivity: "/auth/security/activity",
    stepUpRequirement: "/auth/security/step-up/requirement",
    stepUpCompleteMfa: "/auth/security/step-up/complete-mfa",
    stepUpCompletePassword: "/auth/security/step-up/complete-password",
    sessions: "/auth/sessions",
    sessionRevoke: (sessionId: string) =>
      `/auth/sessions/${encodeURIComponent(sessionId)}/revoke`,
    sessionsRevokeOthers: "/auth/sessions/revoke-others",
    mfaEnrollmentStart: "/auth/mfa/enrollment/start",
    mfaEnrollmentVerify: "/auth/mfa/enrollment/verify",
    mfaDisable: "/auth/mfa/disable",
    mfaRecoveryCodes: "/auth/mfa/recovery-codes/regenerate",
  },
  users: {
    updateProfile: "/profile",
    // Identity.Profile.GetPreferences / UpdatePreferences.
    preferences: "/profile/preferences",
    // Identity.Profile.UploadAvatar (PUT, multipart part `file`) / RemoveAvatar (DELETE).
    avatar: "/profile/avatar",
  },
  account: {
    // Workspaces.Members.ListMyWorkspaces / SetDefaultWorkspace / LeaveWorkspace.
    workspaces: "/account/workspaces",
    defaultWorkspace: "/account/default-workspace",
    leaveWorkspace: (workspaceId: string) =>
      `/account/workspaces/${encodeURIComponent(workspaceId)}`,
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
    // The signed-in member's own job title (Workspaces.Members.UpdateMyJobTitle).
    myJobTitle: (workspaceId: string) =>
      `/workspaces/${workspaceId}/members/me/job-title`,
    myMembership: (workspaceId: string) =>
      `/workspaces/${workspaceId}/members/me`,
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
    invitationRole: (workspaceId: string, invitationId: string) =>
      `/workspaces/${workspaceId}/invitations/${invitationId}/role`,
    resendInvitation: (workspaceId: string, invitationId: string) =>
      `/workspaces/${workspaceId}/invitations/${invitationId}/resend`,
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
    archive: (pageId: string) => `/pages/${pageId}/archive`,
    restore: (pageId: string) => `/pages/${pageId}/restore`,
    trash: (workspaceId: string) => `/workspaces/${workspaceId}/pages/trash`,
    backlinks: (pageId: string) => `/pages/${pageId}/backlinks`,
    // Documents.Versions.*: the page's history entries.
    versions: (pageId: string) => `/pages/${pageId}/versions`,
    version: (pageId: string, versionId: string) =>
      `/pages/${pageId}/versions/${versionId}`,
    restoreVersion: (pageId: string, versionId: string) =>
      `/pages/${pageId}/versions/${versionId}/restore`,
  },
  // Documents.Templates.*: workspace page templates.
  pageTemplates: {
    list: (workspaceId: string) => `/workspaces/${workspaceId}/page-templates`,
    createPage: (workspaceId: string, templateId: string) =>
      `/workspaces/${workspaceId}/page-templates/${templateId}/pages`,
  },
  // WorkManagement.Templates.*: workspace board templates.
  boardTemplates: {
    list: (workspaceId: string) => `/workspaces/${workspaceId}/templates`,
    saveBoard: (boardId: string) => `/boards/${boardId}/templates`,
    detail: (templateId: string) => `/templates/${templateId}`,
    publish: (templateId: string) => `/templates/${templateId}/publish`,
    archive: (templateId: string) => `/templates/${templateId}/archive`,
    createBoard: (templateId: string) =>
      `/templates/${templateId}/create-board`,
  },
  pageAttachments: {
    list: (pageId: string) => `/pages/${pageId}/attachments`,
    files: (pageId: string) => `/pages/${pageId}/attachments/files`,
    content: (pageId: string, attachmentId: string) =>
      `/pages/${pageId}/attachments/${attachmentId}/content`,
    detail: (pageId: string, attachmentId: string) =>
      `/pages/${pageId}/attachments/${attachmentId}`,
  },
  blocks: {
    detail: (pageId: string, blockId: string) =>
      `/pages/${pageId}/blocks/${blockId}`,
    reorder: "/blocks/reorder",
    batch: (pageId: string) => `/pages/${pageId}/blocks/batch`,
  },
  // WorkManagement.Labels.Update / Delete address the label by id alone.
  labels: {
    detail: (labelId: string) => `/labels/${labelId}`,
  },
  boards: {
    listByWorkspaceId: (workspaceId: string) =>
      `/workspaces/${workspaceId}/boards`,
    // WorkManagement.Boards.ListArchived / ListDeleted: boards that left the working set.
    archivedByWorkspaceId: (workspaceId: string) =>
      `/workspaces/${workspaceId}/boards/archived`,
    deletedByWorkspaceId: (workspaceId: string) =>
      `/workspaces/${workspaceId}/boards/deleted`,
    detail: (boardId: string) => `/boards/${boardId}`,
    // WorkManagement.Boards.Archive / Unarchive / Restore carry the board version the person saw.
    archive: (boardId: string) => `/boards/${boardId}/archive`,
    unarchive: (boardId: string) => `/boards/${boardId}/unarchive`,
    restore: (boardId: string) => `/boards/${boardId}/restore`,
    lifecycleImpact: (boardId: string) => `/boards/${boardId}/lifecycle-impact`,
    full: (boardId: string) => `/boards/${boardId}/full`,
    // WorkManagement.BoardItems.ListLane: the next page of a Status lane ("none" is the lane without a status).
    lane: (boardId: string, lane: string) =>
      `/boards/${boardId}/items/lanes/${lane}`,
    access: (boardId: string) => `/boards/${boardId}/access`,
    schema: (boardId: string) => `/boards/${boardId}/schema`,
    labels: (boardId: string) => `/boards/${boardId}/labels`,
    moveToSpace: (boardId: string) => `/boards/${boardId}/move-to-space`,
    // WorkManagement.Boards.GetMoveImpact: who gains and loses the board in another space (`?targetSpaceId=`).
    moveImpact: (boardId: string) => `/boards/${boardId}/move-impact`,
    // WorkManagement.Boards.GetSharing / MakePrivate and the Board member writes.
    sharing: (boardId: string) => `/boards/${boardId}/sharing`,
    sharingPrivate: (boardId: string) => `/boards/${boardId}/sharing/private`,
    member: (boardId: string, userId: string) =>
      `/boards/${boardId}/members/${userId}`,
    memberRole: (boardId: string, userId: string) =>
      `/boards/${boardId}/members/${userId}/role`,
    // WorkManagement.Boards.Leave: ends the caller's own explicit membership.
    leave: (boardId: string) => `/boards/${boardId}/members/leave`,
  },
  // Workspaces.Onboarding.* (draft and getting-started card).
  // Workspaces.Workspaces.* routes under an Account.
  accounts: {
    workspaces: (accountId: string) => `/accounts/${accountId}/workspaces`,
    workspaceSlugCheck: (accountId: string) =>
      `/accounts/${accountId}/workspaces/slug-check`,
  },
  onboarding: {
    draft: "/workspaces/onboarding-draft",
    gettingStarted: (workspaceId: string) =>
      `/workspaces/${workspaceId}/getting-started`,
    dismissGettingStarted: (workspaceId: string) =>
      `/workspaces/${workspaceId}/getting-started/dismiss`,
  },
  // Governance.ResourcePermissions.*: explicit grants on a resource kind.
  resourcePermissions: {
    list: (resourceType: string, resourceId: string) =>
      `/resources/${resourceType}/${resourceId}/permissions`,
    detail: (resourceType: string, resourceId: string, permissionId: string) =>
      `/resources/${resourceType}/${resourceId}/permissions/${permissionId}`,
  },

  checklists: {
    detail: (checklistId: string) => `/checklists/${checklistId}`,
    items: (checklistId: string) => `/checklists/${checklistId}/items`,
    // WorkManagement.Checklists.Move
    move: (checklistId: string) => `/checklists/${checklistId}/move`,
  },
  checklistItems: {
    detail: (itemId: string) => `/checklist-items/${itemId}`,
    // WorkManagement.Checklists.ToggleItem
    toggle: (itemId: string) => `/checklist-items/${itemId}/toggle`,
    // WorkManagement.Checklists.MoveItem
    move: (itemId: string) => `/checklist-items/${itemId}/move`,
    // WorkManagement.Checklists.SetItemAssignee
    assignee: (itemId: string) => `/checklist-items/${itemId}/assignee`,
    // WorkManagement.Checklists.SetItemDue
    due: (itemId: string) => `/checklist-items/${itemId}/due`,
  },
  comments: {
    detail: (commentId: string) => `/comments/${commentId}`,
    // Collaboration.Comments.Resolve
    resolve: (commentId: string) => `/comments/${commentId}/resolve`,
    // Collaboration.Comments.Reopen
    reopen: (commentId: string) => `/comments/${commentId}/reopen`,
  },
  notifications: {
    list: "/notifications",
    read: (id: string) => `/notifications/${id}/read`,
    readAll: "/notifications/read-all",
    // Collaboration.Preferences.*: one member's choices in one workspace.
    preferences: (workspaceId: string) =>
      `/workspaces/${workspaceId}/notification-preferences`,
  },

  boardGroups: {
    create: (boardId: string) => `/boards/${boardId}/groups`,
    detail: (groupId: string) => `/board-groups/${groupId}`,
    duplicate: (groupId: string) => `/board-groups/${groupId}/duplicate`,
    move: (groupId: string) => `/board-groups/${groupId}/move`,
    archive: (groupId: string) => `/board-groups/${groupId}/archive`,
    unarchive: (groupId: string) => `/board-groups/${groupId}/unarchive`,
  },
  boardItems: {
    create: (boardId: string) => `/boards/${boardId}/items`,
    bulk: (boardId: string) => `/boards/${boardId}/items/bulk`,
    detail: (itemId: string) => `/board-items/${itemId}`,
    move: (itemId: string) => `/board-items/${itemId}/move`,
    linkPage: (itemId: string) => `/board-items/${itemId}/link-page`,
    duplicate: (itemId: string) => `/board-items/${itemId}/duplicate`,
    archive: (itemId: string) => `/board-items/${itemId}/archive`,
    complete: (itemId: string) => `/board-items/${itemId}/complete`,
    status: (itemId: string) => `/board-items/${itemId}/status`,
    fieldValues: (itemId: string) => `/board-items/${itemId}/field-values`,
    fieldValue: (itemId: string, fieldId: string) =>
      `/board-items/${itemId}/values/${fieldId}`,
    comments: (itemId: string) => `/board-items/${itemId}/comments`,
    commentsPage: (itemId: string) => `/board-items/${itemId}/comments/page`,
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
    assignees: (itemId: string) => `/board-items/${itemId}/assignees`,
    assignee: (itemId: string, userId: string) =>
      `/board-items/${itemId}/assignees/${userId}`,
  },
  boardFields: {
    create: (boardId: string) => `/boards/${boardId}/fields`,
    detail: (boardId: string, fieldId: string) =>
      `/boards/${boardId}/fields/${fieldId}`,
    move: (boardId: string, fieldId: string) =>
      `/boards/${boardId}/fields/${fieldId}/move`,
    // WorkManagement.BoardFields.Deleted / .Restore: a deleted field is kept 30 days.
    deleted: (boardId: string) => `/boards/${boardId}/fields/deleted`,
    restore: (boardId: string, fieldId: string) =>
      `/boards/${boardId}/fields/${fieldId}/restore`,
    options: (boardId: string, fieldId: string) =>
      `/boards/${boardId}/fields/${fieldId}/options`,
    option: (boardId: string, fieldId: string, optionId: string) =>
      `/boards/${boardId}/fields/${fieldId}/options/${optionId}`,
  },
  // WorkManagement.SavedFilters.*: listed and created per board, changed by filter id.
  savedFilters: {
    forBoard: (boardId: string) => `/boards/${boardId}/saved-filters`,
    detail: (filterId: string) => `/saved-filters/${filterId}`,
    rename: (filterId: string) => `/saved-filters/${filterId}/rename`,
    visibility: (filterId: string) => `/saved-filters/${filterId}/visibility`,
    restore: (filterId: string) => `/saved-filters/${filterId}/restore`,
  },
  // WorkManagement.Forms.*: listed and created per board, changed by form id; a response is changed by its own id.
  forms: {
    forBoard: (boardId: string) => `/boards/${boardId}/forms`,
    detail: (formId: string) => `/forms/${formId}`,
    publish: (formId: string) => `/forms/${formId}/publish`,
    close: (formId: string) => `/forms/${formId}/close`,
    restore: (formId: string) => `/forms/${formId}/restore`,
    questions: (formId: string) => `/forms/${formId}/questions`,
    question: (formId: string, questionId: string) =>
      `/forms/${formId}/questions/${questionId}`,
    submissions: (formId: string) => `/forms/${formId}/submissions`,
    submission: (submissionId: string) => `/form-submissions/${submissionId}`,
    process: (submissionId: string) =>
      `/form-submissions/${submissionId}/process`,
    reject: (submissionId: string) =>
      `/form-submissions/${submissionId}/reject`,
    spam: (submissionId: string) => `/form-submissions/${submissionId}/spam`,
  },
  // WorkManagement.BoardPreferences.*: a member's personal state of one view of a board.
  boardPreferences: {
    forView: (boardId: string, viewId: string) =>
      `/boards/${boardId}/preferences/${viewId}`,
  },
  boardViews: {
    detail: (boardId: string) => `/boards/${boardId}/views`,
    saved: (boardId: string) => `/boards/${boardId}/views/saved`,
    view: (boardId: string, viewId: string) =>
      `/boards/${boardId}/views/${viewId}`,
  },
};
