import type {
  AccountStatus,
  WorkspaceMember,
  WorkspaceView,
} from "../types/workspace";

/** Closed reads exactly like Suspended (the stricter reading); a missing or unknown status is Active. */
export function isAccountReadOnly(
  workspace?: { readonly accountStatus?: AccountStatus } | null,
): boolean {
  return (
    workspace?.accountStatus === "Suspended" ||
    workspace?.accountStatus === "Closed"
  );
}

/** The workspace after the server refused a write because of the Account; Closed stays Closed. */
export function markAccountSuspended<
  T extends { readonly accountStatus?: AccountStatus },
>(workspace: T): T {
  return workspace.accountStatus === "Closed"
    ? workspace
    : { ...workspace, accountStatus: "Suspended" };
}

export function isOwner(member?: WorkspaceMember | null): boolean {
  return member?.role === "owner";
}

export function isAdmin(member?: WorkspaceMember | null): boolean {
  return member?.role === "owner" || member?.role === "admin";
}

export function getActiveView(
  views: WorkspaceView[],
  activeViewId?: string,
): WorkspaceView | undefined {
  if (!activeViewId) return views.find((v) => v.isDefault) || views[0];
  return views.find((v) => v.id === activeViewId);
}

export function sortViews(views: WorkspaceView[]): WorkspaceView[] {
  return [...views].sort((a, b) => a.position - b.position);
}
