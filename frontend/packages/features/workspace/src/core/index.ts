/**
 * @notrelix/feature-workspace — Workspace core types and API contracts.
 *
 * Framework-neutral: no React, no DOM, no Next.js.
 */

export type {
  WorkspaceViewType,
  WorkspaceViewVisibility,
  WorkspaceRole,
  WorkspaceMembershipStatus,
  WorkspaceMember,
  WorkspaceInvitationState,
  WorkspaceInvitation,
  WorkspaceSummary,
  WorkspaceViewTarget,
  WorkspaceViewConfig,
  WorkspaceView,
  WorkspaceFavorite,
  WorkspaceRecentItem,
  WorkspaceActivityItem,
  WorkspaceSnapshot,
  CreateWorkspaceViewInput,
  UpdateWorkspaceViewInput,
  CreateWorkspaceInput,
  UpdateWorkspaceInput,
  UpdateWorkspaceProfileInput,
  WorkspaceSettings,
  CreateWorkspaceInvitationInput,
} from "./types/workspace";

export {
  createWorkspaceService,
  createWorkspaceSettingsService,
  parseWorkspaceSettings,
  parseWorkspaceSummary,
  type WorkspaceApiClient,
  type WorkspaceEndpoints,
  type WorkspaceSettingsEndpoints,
  type WorkspaceSettingsService,
} from "./api/workspace.service";
export {
  createMembersService,
  parseMembers,
  type MembersService,
} from "./api/members.service";
export {
  createInvitationsService,
  createWorkspaceInvitationsService,
  parseInvitations,
  type InvitationsEndpoints,
  type WorkspaceInvitationsEndpoints,
  type WorkspaceInvitationsService,
} from "./api/invitations.service";
export { createViewsService } from "./api/views.service";
export {
  createActivityService,
  parseActivityPage,
  type ActivityEndpoints,
  type WorkspaceActivityPage,
} from "./api/activity.service";
export {
  createSpacesService,
  parseSpaceMembers,
  parseSpaceTree,
  parseTeams,
  type CreateSpaceInput,
  type MoveSpaceInput,
  type SpaceDetailsInput,
  type SpacesApiClient,
  type SpacesEndpoints,
  type SpacesService,
} from "./api/spaces.service";
export {
  createNavigationService,
  type NavigationApiClient,
  type NavigationEndpoints,
  type NavigationService,
} from "./api/navigation.service";

export * from "./model/selectors";
export * from "./model/workspace-views";
export * from "./rules/workspace-rules";
export * from "./model/navigation";
export * from "./model/breadcrumb";
export * from "./model/shortcuts";
export * from "./model/errors";
export * from "./model/members";
export * from "./model/my-work";
export * from "./model/library";
export * from "./model/spaces";
