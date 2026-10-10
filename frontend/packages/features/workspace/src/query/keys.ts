import {
  accountQueryKey,
  globalQueryKey,
  workspaceQueryKey,
} from "@notrelix/query";

export const workspaceQueryKeys = {
  all: accountQueryKey("workspaces"),
  detail: (workspaceId: string) =>
    workspaceQueryKey(workspaceId, "workspace", "detail"),
  snapshot: (workspaceId: string) =>
    workspaceQueryKey(workspaceId, "workspace", "snapshot"),
  members: (workspaceId: string) =>
    workspaceQueryKey(workspaceId, "workspace", "members"),
  views: (workspaceId: string) =>
    workspaceQueryKey(workspaceId, "workspace", "views"),
  activeView: (workspaceId: string, view: string) =>
    workspaceQueryKey(workspaceId, "workspace", "views", "active", view),
  invitations: (workspaceId: string) =>
    workspaceQueryKey(workspaceId, "workspace", "invitations"),
  invitationByToken: (token: string) =>
    globalQueryKey("workspace-invitation", "token", token),
  pendingInvitations: accountQueryKey("workspace-invitations", "pending"),
  activity: (workspaceId: string) =>
    workspaceQueryKey(workspaceId, "workspace", "activity"),
  settings: (workspaceId: string) =>
    workspaceQueryKey(workspaceId, "workspace", "settings"),
  /** Per-member navigation state (WX-DEC-011); scoped by workspace. */
  navigation: (workspaceId: string) =>
    workspaceQueryKey(workspaceId, "workspace", "navigation"),
  recents: (workspaceId: string) =>
    workspaceQueryKey(workspaceId, "workspace", "navigation", "recents"),
  pins: (workspaceId: string) =>
    workspaceQueryKey(workspaceId, "workspace", "navigation", "pins"),
} as const;
