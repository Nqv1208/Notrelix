export { createUseCreateWorkspace } from "./hooks/mutations/use-create-workspace";
export { createUseCreateWorkspaceView } from "./hooks/mutations/use-create-workspace-view";
export { createUseUpdateWorkspaceView } from "./hooks/mutations/use-update-workspace-view";
export { createUseReorderWorkspaceViews } from "./hooks/mutations/use-reorder-workspace-views";
export {
  createUseMemberMutations,
  type UpdateMemberRoleInput,
} from "./hooks/mutations/use-member-mutations";
export {
  createUseChangeInvitationRole,
  createUseCreateInvitation,
  createUseResendInvitation,
  createUseRevokeInvitation,
} from "./hooks/mutations/use-create-invitation";
export { createUseWorkspaceSettingsMutations } from "./hooks/mutations/use-workspace-settings-mutations";
export { createUseAcceptInvitation } from "./hooks/mutations/use-accept-invitation";
export { createUseWorkspaceList } from "./query/hooks/use-workspace-list";
export { createUseWorkspace } from "./query/hooks/use-workspace";
export {
  createUseMyWorkspaceMembership,
  createUseWorkspaceMembers,
} from "./query/hooks/use-workspace-members";
export { createUseWorkspaceInvitations } from "./query/hooks/use-workspace-invitations";
export { createUseWorkspaceActivity } from "./query/hooks/use-workspace-activity";
export { createUseWorkspaceSettings } from "./query/hooks/use-workspace-settings";
export { createUseInvitationByToken } from "./query/hooks/use-invitation-by-token";
export { createUsePendingInvitations } from "./query/hooks/use-pending-invitations";
export { createUseWorkspaceShellData } from "./query/hooks/use-workspace-shell-data";

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
export { workspaceCommandCatalog } from "./command/workspace-command.catalog";
export {
  workspaceCommandCopy,
  type WorkspaceCommandCopy,
} from "./command/workspace-command.copy";
export { useWorkspaceCommandCopy } from "./command/use-workspace-command-copy";
export * from "./command/command-models";
export {
  PALETTE_COMMAND_IDS,
  type PaletteCommandId,
} from "./command/command-build";
export {
  WorkspaceCommandPaletteSurface,
  type WorkspaceCommandPaletteSurfaceProps,
} from "./command/command-palette-surface";
export {
  WorkspaceShortcutSheetSurface,
  type WorkspaceShortcutSheetSurfaceProps,
} from "./shell/workspace-shortcut-sheet-surface";
export {
  WorkspaceScopeStateSurface,
  type WorkspaceScopeState,
  type WorkspaceScopeStateSurfaceProps,
} from "./shell/workspace-scope-state-surface";

// ── Inbox (Final-Inbox) ───────────────────────────────────────────
export { workspaceInboxCatalog } from "./inbox/workspace-inbox.catalog";
export {
  workspaceInboxCopy,
  type WorkspaceInboxCopy,
} from "./inbox/workspace-inbox.copy";
export { useWorkspaceInboxCopy } from "./inbox/use-workspace-inbox-copy";
export * from "./inbox/inbox-models";
export {
  WorkspaceInboxSurface,
  inboxDayLabel,
  inboxTimeLabel,
  type WorkspaceInboxSurfaceProps,
  type InboxInitialView,
} from "./inbox/workspace-inbox-surface";

// ── Notrelix system rail (Final-Space nav=hien-de / hien-day) ─────
export { notrelixSystemRailCatalog } from "./shell/notrelix-system-rail.catalog";
export {
  notrelixSystemRailCopy,
  type NotrelixSystemRailCopy,
} from "./shell/notrelix-system-rail.copy";
export { useNotrelixSystemRailCopy } from "./shell/use-notrelix-system-rail-copy";
export {
  NotrelixSystemRailSurface,
  type NotrelixSystemRailSurfaceProps,
  type SystemRailPresentation,
  type SystemRailWorkspace,
} from "./shell/notrelix-system-rail-surface";

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
  type HomeAgenda,
  type HomeAgendaEntry,
  type HomeReplyActions,
  type HomeReplyView,
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
  type CreateResourceTemplate,
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
  type SpaceBoardLink,
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
  type WorkspaceSpaceSurfaceProps,
} from "./spaces/workspace-space-surface";
export * from "./spaces/space-page-models";
export * from "./spaces/space-members-models";
export type { SpaceAddPreviewBase } from "./spaces/space-members-dialogs";
export type { MemberMenuState } from "./spaces/space-members-view";
export type {
  SpaceDetailsValues,
  SpaceSaveStatus,
} from "./spaces/space-settings-panel";

// ── Teams (WX-TEAM-001) ───────────────────────────────────────────
export {
  createUseTeam,
  createUseTeamMembers,
  createUseTeamsMembers,
  createUseTeams,
  createUseWorkspaceTeams,
} from "./query/hooks/use-teams";
export {
  createUseTeamMutations,
  type CreateTeamInput,
  type TeamMemberWrite,
  type TeamWrite,
} from "./hooks/mutations/use-team-mutations";
export { workspaceTeamsCatalog } from "./teams/workspace-teams.catalog";
export {
  workspaceTeamsCopy,
  type WorkspaceTeamsCopy,
} from "./teams/workspace-teams.copy";
export { useWorkspaceTeamsCopy } from "./teams/use-workspace-teams-copy";
export {
  ArchiveTeamDialog,
  CreateTeamDialog,
  TEAM_DESCRIPTION_MAX,
  TEAM_NAME_MAX,
  type ArchiveTeamDialogProps,
  type CreateTeamDialogProps,
  type CreateTeamValues,
  type TeamCandidate,
} from "./teams/create-team-dialog";
export {
  WorkspaceTeamsSurface,
  type TeamsAdminView,
  type TeamsFilter,
  type WorkspaceTeamsSurfaceProps,
} from "./teams/workspace-teams-surface";
export {
  WorkspaceTeamSurface,
  type AddTeamMemberInput,
  type WorkspaceTeamSurfaceProps,
} from "./teams/workspace-team-surface";
export {
  WorkspaceSecuritySurface,
  type WorkspaceSecurityView,
  type WorkspaceSecuritySurfaceProps,
} from "./teams/workspace-security-surface";
export {
  WorkspaceSetupFrame,
  type SetupFrameLink,
  type SetupFrameProps,
  type SetupSection,
} from "./teams/workspace-setup-frame";
export * from "./teams/workspace-teams-models";
export type {
  MyWorkActions,
  MyWorkCreateBoard,
  MyWorkWaitingView,
} from "./home/my-work-types";

// Onboarding (the Onboarding mockup)
export { workspaceOnboardingCatalog } from "./onboarding/onboarding.catalog";
export {
  onboardingCopy,
  type OnboardingCopy,
} from "./onboarding/onboarding.copy";
export { useOnboardingCopy } from "./onboarding/use-onboarding-copy";
export {
  OnboardingSurface,
  type OnboardingSurfaceProps,
} from "./onboarding/onboarding-surface";
export {
  OnboardingJoinSurface,
  type JoinSpace,
  type OnboardingJoinSurfaceProps,
} from "./onboarding/onboarding-join-surface";
export {
  GettingStartedCard,
  type GettingStartedCardProps,
  type GettingStartedItem,
} from "./onboarding/getting-started-card";
export {
  COMPANY_SIZES,
  DEFAULT_COLOR_CYCLE,
  DEPARTMENT_COLORS,
  companyInitials,
  departmentInitials,
  emptyDraft,
  shortCompanyName,
  slugFromName,
  type AddressCheck,
  type BoardTemplateId,
  type BoardTemplateInfo,
  type CompanySize,
  type DepartmentColor,
  type DepartmentDraft,
  type InviteRole,
  type InviteRowDraft,
  type OnboardingCatalog,
  type OnboardingDraftData,
  type OnboardingFormStep,
  type OnboardingStepId,
  type OnboardingSummary,
} from "./onboarding/onboarding-models";
export { newInviteRow } from "./onboarding/onboarding-steps";
export {
  onboardingSuggestions,
  type DepartmentSuggestion,
  type OnboardingSuggestions,
} from "./onboarding/onboarding-suggestions";

// Reports (the Reports mockup)
export { workspaceReportsCatalog } from "./reports/reports.catalog";
export { reportsCopy, type ReportsCopy } from "./reports/reports.copy";
export { useReportsCopy } from "./reports/use-reports-copy";
export {
  ReportDashboardSurface,
  type ReportDashboardSurfaceProps,
  type ReportPending,
} from "./reports/report-dashboard-surface";
export {
  ReportListSurface,
  type ReportListSurfaceProps,
} from "./reports/report-list-surface";
export type { ReportDrillState } from "./reports/report-drill-sheet";
export type {
  ReportSharingState,
  ReportWidgetOptionsState,
} from "./reports/report-dialogs";
export type { ReportLink, ReportLinks } from "./reports/report-widgets";
export type { ReportFormatters } from "./reports/reports-format";
export * from "./reports/reports-models";

// Billing (the Billing mockup)
export { workspaceBillingCatalog } from "./billing/billing.catalog";
export { billingCopy, type BillingCopy } from "./billing/billing.copy";
export { useBillingCopy } from "./billing/use-billing-copy";
export * from "./billing/billing-models";
export {
  BillingOverviewSurface,
  type BillingOverviewProps,
} from "./billing/billing-overview-surface";
export {
  BillingCompareSurface,
  type BillingCompareProps,
} from "./billing/billing-compare-surface";
export {
  BillingInvoicesSurface,
  type BillingInvoicesProps,
} from "./billing/billing-invoices-surface";
export {
  BillingDowngradeSurface,
  type BillingDowngradeProps,
} from "./billing/billing-downgrade-surface";
export {
  BillingBuyDialog,
  BillingSeatInviteDialog,
  SEAT_MAX,
  SEAT_MIN,
  type BillingBuyDialogProps,
  type BillingSeatInviteDialogProps,
} from "./billing/billing-dialogs";

// Integrations (the Integrations mockup)
export { workspaceIntegrationsCatalog } from "./integrations/integrations.catalog";
export {
  integrationsCopy,
  type IntegrationsCopy,
} from "./integrations/integrations.copy";
export { useIntegrationsCopy } from "./integrations/use-integrations-copy";
export * from "./integrations/integrations-models";
export {
  IntegrationConnectDialog,
  IntegrationDetailSurface,
  IntegrationDisconnectDialog,
  IntegrationSecretDialog,
  IntegrationUpgradeSheet,
  IntegrationsListSurface,
  type IntegrationConnectDialogProps,
  type IntegrationDetailProps,
  type IntegrationDisconnectDialogProps,
  type IntegrationSecretDialogProps,
  type IntegrationUpgradeSheetProps,
  type IntegrationsListProps,
} from "./integrations/integrations-surface";

// Pages whose backend is not available yet, and the billing capabilities page
export { workspaceFeatureAvailabilityCatalog } from "./unavailable/feature-availability.catalog";
export {
  featureAvailabilityCopy,
  type FeatureAvailabilityCopy,
} from "./unavailable/feature-availability.copy";
export { useFeatureAvailabilityCopy } from "./unavailable/use-feature-availability-copy";
export {
  FeatureUnavailableSurface,
  type FeatureUnavailableSurfaceProps,
  type UnavailableFeature,
} from "./unavailable/feature-unavailable-surface";
export {
  DEFAULT_REPORT_WEEKS,
  REPORT_WEEK_OPTIONS,
  WorkReportSurface,
  type WorkReportAssigneeRow,
  type WorkReportDto,
  type WorkReportScope,
  type WorkReportState,
  type WorkReportStatusRow,
  type WorkReportSurfaceProps,
  type WorkReportWeekRow,
} from "./reports/work-report-surface";
export {
  BillingCapabilitiesSurface,
  type BillingCapabilitiesState,
  type BillingCapabilitiesSurfaceProps,
  type BillingCapabilityDto,
  type BillingPlanCardState,
} from "./billing/billing-capabilities-surface";
