export { createUseCreateWorkspace } from "./hooks/mutations/use-create-workspace";
export { createUseCreateWorkspaceView } from "./hooks/mutations/use-create-workspace-view";
export { createUseUpdateWorkspaceView } from "./hooks/mutations/use-update-workspace-view";
export { createUseReorderWorkspaceViews } from "./hooks/mutations/use-reorder-workspace-views";
export {
  createUseMemberMutations,
  type UpdateMemberRoleInput,
} from "./hooks/mutations/use-member-mutations";
export {
  createUseCreateInvitation,
  createUseRevokeInvitation,
} from "./hooks/mutations/use-create-invitation";
export { createUseWorkspaceSettingsMutations } from "./hooks/mutations/use-workspace-settings-mutations";
export { createUseAcceptInvitation } from "./hooks/mutations/use-accept-invitation";
export { createUseWorkspaceList } from "./query/hooks/use-workspace-list";
export { createUseWorkspace } from "./query/hooks/use-workspace";
export { createUseWorkspaceMembers } from "./query/hooks/use-workspace-members";
export { createUseWorkspaceInvitations } from "./query/hooks/use-workspace-invitations";
export { createUseWorkspaceActivity } from "./query/hooks/use-workspace-activity";
export { createUseWorkspaceSettings } from "./query/hooks/use-workspace-settings";
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
export { ShellLink, IdentityMark, ResourceMark } from "./shell/shell-link";
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

// ── Home and My work (WX-HOME-001/002) ────────────────────────────
export { workspaceHomeCatalog } from "./home/workspace-home.catalog";
export {
  workspaceHomeCopy,
  type WorkspaceHomeCopy,
} from "./home/workspace-home.copy";
export { useWorkspaceHomeCopy } from "./home/use-workspace-home-copy";
export * from "./home/home-models";
export {
  WorkspaceHomeSurface,
  type WorkspaceHomeSurfaceProps,
} from "./home/workspace-home-surface";
export {
  WorkspaceMyWorkSurface,
  type WorkspaceMyWorkSurfaceProps,
} from "./home/workspace-my-work-surface";
export {
  WorkspaceOnboardingSurface,
  type WorkspaceOnboardingSurfaceProps,
} from "./home/workspace-onboarding-surface";
export {
  CreateResourceDialog,
  type CreateResourceDialogProps,
  type CreatableResource,
} from "./home/create-resource-dialog";

// ── Boards and Docs directory (WX-DIR-001) ────────────────────────
export { workspaceLibraryCatalog } from "./library/workspace-library.catalog";
export {
  workspaceLibraryCopy,
  type WorkspaceLibraryCopy,
} from "./library/workspace-library.copy";
export { useWorkspaceLibraryCopy } from "./library/use-workspace-library-copy";
export {
  WorkspaceLibrarySurface,
  type WorkspaceLibrarySurfaceProps,
} from "./library/workspace-library-surface";

// ── Members and invitations (WX-MEM-001) ──────────────────────────
export { workspaceMembersCatalog } from "./members/workspace-members.catalog";
export {
  workspaceMembersCopy,
  type WorkspaceMembersCopy,
} from "./members/workspace-members.copy";
export { useWorkspaceMembersCopy } from "./members/use-workspace-members-copy";
export {
  WorkspaceMembersSurface,
  type MembersTab,
  type WorkspaceMembersSurfaceProps,
} from "./members/workspace-members-surface";

// ── Settings (WX-SET-001) ─────────────────────────────────────────
export { workspaceSettingsCatalog } from "./settings/workspace-settings.catalog";
export {
  workspaceSettingsCopy,
  type WorkspaceSettingsCopy,
} from "./settings/workspace-settings.copy";
export { useWorkspaceSettingsCopy } from "./settings/use-workspace-settings-copy";
export {
  WorkspaceSettingsSurface,
  type SaveStatus,
  type WorkspaceProfileValues,
  type WorkspaceSettingsSurfaceProps,
} from "./settings/workspace-settings-surface";

// ── Space org tree (OT-5) ─────────────────────────────────────────
export { workspaceSpacesCatalog } from "./spaces/workspace-spaces.catalog";
export {
  workspaceSpacesCopy,
  type WorkspaceSpacesCopy,
} from "./spaces/workspace-spaces.copy";
export { useWorkspaceSpacesCopy } from "./spaces/use-workspace-spaces-copy";
export {
  createUseSpaceMembers,
  createUseSpaceTree,
  createUseWorkspaceTeams,
} from "./query/hooks/use-spaces";
export {
  createUseSpaceMutations,
  type SpaceMemberWrite,
} from "./hooks/mutations/use-space-mutations";
export {
  useSpaceTreeExpansion,
  spaceExpansionStorageKey,
  type ExpansionStorage,
} from "./spaces/use-space-tree-expansion";
export {
  SpaceTreeNav,
  SPACE_TREE_WINDOW_THRESHOLD,
  type SpaceTreeNavProps,
  type SpaceTreeState,
} from "./spaces/space-tree-nav";
export {
  CreateSpaceDialog,
  type CreateSpaceDialogProps,
  type CreateSpaceValues,
} from "./spaces/create-space-dialog";
export {
  MoveToSpaceDialog,
  type MoveToSpaceDialogProps,
  type MoveToSpaceResource,
  type MoveToSpaceSubject,
} from "./spaces/move-to-space-dialog";
export {
  WorkspaceSpaceSurface,
  type SpaceContent,
  type SpaceResource,
  type WorkspaceSpaceSurfaceProps,
} from "./spaces/workspace-space-surface";
export type {
  AddSpaceMemberInput,
  SpaceMemberCandidate,
} from "./spaces/space-members-panel";
export type {
  SpaceDetailsValues,
  SpaceSaveStatus,
} from "./spaces/space-settings-panel";

// ── Teams (WX-TEAM-001) ───────────────────────────────────────────
export {
  createUseTeam,
  createUseTeamMembers,
  createUseTeams,
} from "./query/hooks/use-teams";
export {
  createUseTeamMutations,
  type CreateTeamInput,
  type TeamMemberWrite,
  type TeamWrite,
} from "./hooks/mutations/use-team-mutations";
