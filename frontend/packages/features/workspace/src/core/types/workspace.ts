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
 * Membership status as `ListMembers` reports it (`Active` or `Suspended`; removed
 * members are not listed). `undefined` only when a row carries no known status.
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
  /** The person's title in this workspace, when they or an admin set one. */
  jobTitle?: string;
  membership?: WorkspaceMembershipStatus;
  /** The member's version; every role, suspend, activate and remove write quotes it. */
  version?: number;
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
 * Invitation lifecycle as shown. `ListWorkspaceInvitations` reports the status
 * (`Pending`, `Accepted`, `Revoked`, `Expired`, `Declined`); revoked and declined
 * invitations are dropped by the adapter, and a pending one past `expiresAt` is
 * shown as expired.
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
  /** The invitation's version; changing its role quotes it. */
  version?: number;
  createdAt: string;
  workspaceId?: string;
  workspaceSlug?: string;
  workspaceName?: string;
  inviterName?: string;
}

/**
 * An invitation waiting for the signed-in person (`GetUserPendingInvitations`): addressed to their own
 * confirmed address, so it holds no email and no token; answering it quotes only its id.
 */
export interface PendingInvitation {
  id: string;
  workspaceName: string;
  workspaceSlug: string;
  /** The inviter's display name; empty when the producer has none to show. */
  inviterName: string;
  role: WorkspaceRole;
  expiresAt: string;
}

export interface WorkspaceSummary {
  id: string;
  /**
   * Optimistic-concurrency version of the workspace, as `GetWorkspace` and the
   * workspace lists report it; profile, settings and archive writes send it
   * back as `expectedVersion`.
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
  /** The address the caller chose; when absent the producer derives a free one from the name. */
  slug?: string;
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
  /** IANA time zone the workspace's boards inherit. */
  readonly timeZone: string;
  /** First day of the week the workspace's boards inherit. */
  readonly weekStart: "monday" | "sunday";
}

/** The time zones the settings list names in words; any IANA id is accepted. */
export const WORKSPACE_TIME_ZONES = [
  "Asia/Ho_Chi_Minh",
  "Asia/Singapore",
  "Asia/Tokyo",
] as const;
export type WorkspaceTimeZone = (typeof WORKSPACE_TIME_ZONES)[number];

/** Every IANA time zone id the browser or runtime lists, the named ones and UTC included. */
export function allTimeZones(): readonly string[] {
  const listed =
    typeof Intl.supportedValuesOf === "function"
      ? Intl.supportedValuesOf("timeZone")
      : [];
  return [...new Set([...WORKSPACE_TIME_ZONES, ...listed, "UTC"])];
}

/** The spelling of an IANA time zone id the list uses, or null when the runtime does not know it. */
export function resolveTimeZone(value: string): string | null {
  const wanted = value.trim();
  if (!wanted) return null;
  const listed = allTimeZones().find(
    (zone) => zone.toLowerCase() === wanted.toLowerCase(),
  );
  if (listed) return listed;
  if (!wanted.includes("/")) return null;
  try {
    new Intl.DateTimeFormat("en", { timeZone: wanted });
    return wanted;
  } catch {
    return null;
  }
}

export interface CreateWorkspaceInvitationInput {
  email: string;
  /** Member or Guest only (WSP-026); Admin is granted after joining. */
  role: Extract<WorkspaceRole, "member" | "guest">;
}
