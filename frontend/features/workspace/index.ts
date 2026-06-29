// Component exports
export { WorkspaceSwitcher } from "./components/workspace-switcher"
export { PendingInvitationsMenu } from "./components/pending-invitations-menu"
export { WorkspaceManagementPanel } from "./components/workspace-management-panel"
export { WorkspaceSidebar } from "./components/workspace-sidebar"
export { WorkspaceCompactHeader } from "./components/workspace-compact-header"

// Hook exports
export { useWorkspace, useWorkspaceList } from "./hooks/queries/use-workspace"
export { useWorkspaceSnapshot } from "./hooks/queries/use-workspace-snapshot"
export { useWorkspaceMembers } from "./hooks/queries/use-workspace-members"
export { useWorkspaceViews } from "./hooks/queries/use-workspace-views"
export { useWorkspaceInvitations } from "./hooks/queries/use-workspace-invitations"
export { useInvitationByToken } from "./hooks/queries/use-invitation-by-token"
export { usePendingInvitations } from "./hooks/queries/use-pending-invitations"
export { useCreateWorkspace } from "./hooks/mutations/use-create-workspace"
export { useUpdateWorkspace } from "./hooks/mutations/use-update-workspace"
export { useCreateWorkspaceView } from "./hooks/mutations/use-create-workspace-view"
export { useUpdateWorkspaceView } from "./hooks/mutations/use-update-workspace-view"
export { useReorderWorkspaceViews } from "./hooks/mutations/use-reorder-workspace-views"
export { useUpdateMemberRole } from "./hooks/mutations/use-update-member-role"
export { useRemoveMember } from "./hooks/mutations/use-remove-member"
export { useCreateInvitation } from "./hooks/mutations/use-create-invitation"
export { useDeleteInvitation } from "./hooks/mutations/use-delete-invitation"
export { useAcceptInvitation } from "./hooks/mutations/use-accept-invitation"
export { useActiveWorkspaceView } from "./hooks/state/use-active-workspace-view"

// Type exports
export type {
  WorkspaceViewType,
  WorkspaceViewVisibility,
  WorkspaceMember,
  WorkspaceInvitation,
  WorkspaceSummary,
  WorkspaceViewTarget,
  WorkspaceViewConfig,
  WorkspaceView,
  WorkspaceFavorite,
  WorkspaceRecentItem,
  WorkspaceSnapshot,
  CreateWorkspaceViewInput,
  UpdateWorkspaceViewInput,
  CreateWorkspaceInput,
  UpdateWorkspaceInput,
  CreateWorkspaceInvitationInput,
} from "./types"

// Utils / routes exports
export {
  getViewHref,
  isWorkspaceViewType,
} from "./utils/workspace-view"

export {
  isBoardWorkspaceView,
  normalizeBoardWorkspaceViewType,
  getWorkspaceRootHref,
  getWorkspaceViewHref,
  getWorkspaceDashboardHref,
  getWorkspaceDocsHref,
  getWorkspaceDocHref,
  getWorkspaceBoardsHref,
  getWorkspaceBoardHref,
  getWorkspaceBoardBaseHref,
  getWorkspaceBoardViewHref,
  resolveWorkspaceTabbedRoute,
  resolveWorkspaceTabbedActiveView,
  getWorkspaceTabbedViews,
} from "./utils/workspace-routes"

export type { WorkspaceTabbedRoute } from "./utils/workspace-routes"

export { workspaceViewTemplates } from "./constants/view-templates"
