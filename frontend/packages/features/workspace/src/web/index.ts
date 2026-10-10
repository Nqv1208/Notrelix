export { createUseCreateWorkspace } from "./hooks/mutations/use-create-workspace";
export { createUseUpdateWorkspace } from "./hooks/mutations/use-update-workspace";
export { createUseCreateWorkspaceView } from "./hooks/mutations/use-create-workspace-view";
export { createUseUpdateWorkspaceView } from "./hooks/mutations/use-update-workspace-view";
export { createUseReorderWorkspaceViews } from "./hooks/mutations/use-reorder-workspace-views";
export { createUseUpdateMemberRole } from "./hooks/mutations/use-update-member-role";
export { createUseRemoveMember } from "./hooks/mutations/use-remove-member";
export { createUseCreateInvitation } from "./hooks/mutations/use-create-invitation";
export { createUseDeleteInvitation } from "./hooks/mutations/use-delete-invitation";
export { createUseAcceptInvitation } from "./hooks/mutations/use-accept-invitation";
export { createUseWorkspaceList } from "./query/hooks/use-workspace-list";
export { createUseWorkspace } from "./query/hooks/use-workspace";
export { createUseWorkspaceMembers } from "./query/hooks/use-workspace-members";
export { createUseWorkspaceInvitations } from "./query/hooks/use-workspace-invitations";
export { createUseWorkspaceViews } from "./query/hooks/use-workspace-views";
export { createUseInvitationByToken } from "./query/hooks/use-invitation-by-token";
export { createUsePendingInvitations } from "./query/hooks/use-pending-invitations";
export { createUseWorkspaceShellData } from "./query/hooks/use-workspace-shell-data";

export { WorkspaceCompactHeader } from "./components/workspace-compact-header";
export { WorkspaceDirectory } from "./components/workspace-directory";
export { WorkspaceViewTabs } from "./components/workspace-view-tabs";
export { WorkspaceContextualToolbar } from "./components/workspace-contextual-toolbar";
export { WorkspaceAddViewMenu } from "./components/workspace-add-view-menu";
export { PendingInvitationsMenu } from "./components/pending-invitations-menu";

export { WorkspaceOverview } from "./components/dashboard/workspace-overview";
export { ActiveBoards } from "./components/dashboard/active-boards";
export { PinnedDocs } from "./components/dashboard/pinned-docs";
export { ActivityFeed } from "./components/dashboard/activity-feed";
export { UpcomingDeadlines } from "./components/dashboard/upcoming-deadlines";

// ── Workspace shell (WX-SHELL-001…005) ────────────────────────────
export {
  createUseWorkspaceRecents,
  createUseWorkspacePins,
} from "./query/hooks/use-workspace-navigation";
export {
  createUseRecordRecent,
  createUseTogglePin,
  type RecordRecentInput,
  type TogglePinInput,
} from "./hooks/mutations/use-workspace-navigation-mutations";
export { workspaceShellCatalog } from "./shell/workspace-shell.catalog";
export {
  workspaceShellCopy,
  type WorkspaceShellCopy,
} from "./shell/workspace-shell.copy";
export { useWorkspaceShellCopy } from "./shell/use-workspace-shell-copy";
export {
  useShellShortcuts,
  isTypingTarget,
  commandShortcutLabel,
  SHELL_SHORTCUTS,
  type ShellShortcutId,
} from "./shell/use-shell-shortcuts";
export * from "./shell/shell-models";
export { ShellLink, IdentityMark } from "./shell/shell-link";
export {
  WorkspaceSidebarSurface,
  type WorkspaceSidebarSurfaceProps,
} from "./shell/workspace-sidebar-surface";
export {
  WorkspaceTopbarSurface,
  type WorkspaceTopbarSurfaceProps,
} from "./shell/workspace-topbar-surface";
export {
  WorkspaceCommandPaletteSurface,
  type WorkspaceCommandPaletteSurfaceProps,
} from "./shell/workspace-command-palette-surface";
export {
  WorkspaceShortcutSheetSurface,
  type WorkspaceShortcutSheetSurfaceProps,
} from "./shell/workspace-shortcut-sheet-surface";
export {
  WorkspaceScopeStateSurface,
  type WorkspaceScopeState,
  type WorkspaceScopeStateSurfaceProps,
} from "./shell/workspace-scope-state-surface";
