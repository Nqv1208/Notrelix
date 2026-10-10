export type WorkspaceViewType =
  | "table"
  | "doc"
  | "kanban"
  | "calendar"
  | "timeline"
  | "dashboard"
  | "form"
  | "gallery"
  | "chart"
  | "gantt";

export type WorkspaceViewVisibility = "private" | "workspace" | "public";

export type WorkspaceRole = "owner" | "admin" | "member" | "guest";

/**
 * Membership status as the producer reports it. `ListMembers` does not report
 * it yet (open item, WX-MEM-001), so the adapter leaves it `undefined`.
 */
export type WorkspaceMembershipStatus = "active" | "suspended";

export interface WorkspaceMember {
  /** The producer keys members by user id; `id` mirrors `userId`. */
  id: string;
  userId: string;
  name: string;
  role: WorkspaceRole;
  avatarUrl?: string;
  /** ISO date-time the member joined. */
  joinedAt?: string;
  membership?: WorkspaceMembershipStatus;
  /**
   * @deprecated Presence fields of the retired mock shape; read only by the
   * unmounted Phase 2 shell files (`apps/web/src/shell/sidebar/*`).
   */
  initials?: string;
  /** @deprecated See `initials`. */
  status?: "active" | "idle" | "offline" | "in-call";
  /** @deprecated See `initials`. */
  workload?: number;
  /** @deprecated See `initials`. */
  color?: string;
}

/**
 * Invitation lifecycle as far as the producer reports it. The list DTO only
 * carries `isAccepted` and `expiresAt`; revoked invitations are not
 * distinguishable yet (open item, WX-MEM-001).
 */
export type WorkspaceInvitationState = "pending" | "accepted" | "expired";

export interface WorkspaceInvitation {
  id: string;
  token?: string;
  email: string;
  role: WorkspaceRole;
  expiresAt: string;
  isAccepted: boolean;
  isExpired?: boolean;
  createdAt: string;
  workspaceId?: string;
  workspaceSlug?: string;
  workspaceName?: string;
  inviterName?: string;
}

export interface WorkspaceSummary {
  id: string;
  /**
   * Optimistic-concurrency version of the workspace, when the producer reports
   * it. `GetWorkspace` does not yet (open item, WX-SET-001); profile, settings
   * and archive writes require it.
   */
  version?: number;
  slug: string;
  name: string;
  description?: string;
  icon: string;
  plan: "free" | "pro" | "business" | "enterprise";
  memberCount: number;
  isPersonal: boolean;
  settings?: string;
}

export interface WorkspaceViewTarget {
  boardId?: string;
  pageId?: string;
  calendarId?: string;
  dashboardId?: string;
}

export interface WorkspaceViewConfig {
  groupBy?: string;
  hiddenFields?: string[];
  columnOrder?: string[];
  density?: "compact" | "default" | "comfortable";
  filters?: Array<{ fieldId: string; operator: string; value: unknown }>;
  sortBy?: Array<{ fieldId: string; direction: "asc" | "desc" }>;
}

export interface WorkspaceView {
  id: string;
  workspaceId: string;
  name: string;
  type: WorkspaceViewType;
  icon: string;
  description: string;
  target: WorkspaceViewTarget;
  config: WorkspaceViewConfig;
  visibility: WorkspaceViewVisibility;
  isDefault: boolean;
  position: number;
  createdAt: string;
  updatedAt?: string;
}

export interface WorkspaceFavorite {
  id: string;
  title: string;
  type: "view" | "doc" | "board" | "dashboard";
  icon: string;
  href: string;
}

export interface WorkspaceRecentItem {
  id: string;
  title: string;
  type: "view" | "doc" | "board" | "task" | "chat";
  icon: string;
  href: string;
  updatedAt: string;
}

export interface WorkspaceActivityItem {
  id: string;
  actor: string;
  action: string;
  target: string;
  createdAt: string;
}

export interface WorkspaceSnapshot {
  workspace: WorkspaceSummary;
  members: WorkspaceMember[];
  views: WorkspaceView[];
  favorites: WorkspaceFavorite[];
  recent: WorkspaceRecentItem[];
  activity: WorkspaceActivityItem[];
}

export interface CreateWorkspaceViewInput {
  workspaceId: string;
  name: string;
  type: WorkspaceViewType;
  target?: WorkspaceViewTarget;
}

export interface UpdateWorkspaceViewInput {
  name?: string;
  icon?: string;
  config?: Partial<WorkspaceViewConfig>;
  position?: number;
}

export interface CreateWorkspaceInput {
  name: string;
  slug: string;
  isPersonal: boolean;
}

export interface UpdateWorkspaceInput {
  name?: string;
  description?: string;
}

export interface UpdateWorkspaceProfileInput {
  readonly name: string;
  readonly description: string;
  readonly expectedVersion: number;
}

/** Workspace defaults (`Workspaces.Settings.*`). */
export interface WorkspaceSettings {
  readonly defaultMemberRole: Exclude<WorkspaceRole, "owner">;
  readonly invitationExpiryDays: number;
  readonly allowGuestInvites: boolean;
  readonly allowPublicSharing: boolean;
  readonly enforceMfa: boolean;
}

export interface CreateWorkspaceInvitationInput {
  email: string;
  role: Exclude<WorkspaceRole, "owner">;
}
