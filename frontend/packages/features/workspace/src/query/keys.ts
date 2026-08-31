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
  invitationPreview: (token: string) =>
    globalQueryKey("workspace-invitation", "preview", token),
  pendingInvitations: accountQueryKey("workspace-invitations", "pending"),
  activity: (workspaceId: string) =>
    workspaceQueryKey(workspaceId, "workspace", "activity"),
} as const;
