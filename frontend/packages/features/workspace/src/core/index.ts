/**
 * @notrelix/feature-workspace — Workspace core types and API contracts.
 *
 * Framework-neutral: no React, no DOM, no Next.js.
 */

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
  WorkspaceActivityItem,
  WorkspaceSnapshot,
  CreateWorkspaceViewInput,
  UpdateWorkspaceViewInput,
  CreateWorkspaceInput,
  UpdateWorkspaceInput,
  CreateWorkspaceInvitationInput,
} from './types/workspace';

export { createWorkspaceService, type WorkspaceApiClient, type WorkspaceEndpoints } from './api/workspace.service';
export { createMembersService } from './api/members.service';
export { createInvitationsService, type InvitationsEndpoints } from './api/invitations.service';
export { createViewsService } from './api/views.service';
export { createActivityService } from './api/activity.service';

export * from './query';
export * from './model/selectors';
export * from './model/workspace-views';
export * from './rules/workspace-rules';

