import type {
  WorkspaceActivityItem,
  WorkspaceInvitation,
} from "../core/types/workspace";

export function workspaceInvitation(
  overrides: Partial<WorkspaceInvitation> &
    Pick<WorkspaceInvitation, "id" | "email">,
): WorkspaceInvitation {
  return {
    role: overrides.role ?? "member",
    expiresAt: overrides.expiresAt ?? "2026-02-15T10:30:00.000Z",
    isAccepted: overrides.isAccepted ?? false,
    createdAt: overrides.createdAt ?? "2026-01-15T10:30:00.000Z",
    workspaceName: overrides.workspaceName ?? "Acme Workspace",
    inviterName: overrides.inviterName ?? "Ada Lovelace",
    ...overrides,
  };
}

export function workspaceActivityItem(
  overrides: Partial<WorkspaceActivityItem> & Pick<WorkspaceActivityItem, "id">,
): WorkspaceActivityItem {
  return {
    actor: overrides.actor ?? "Ada Lovelace",
    action: overrides.action ?? "edited",
    target: overrides.target ?? "Operating plan",
    createdAt: overrides.createdAt ?? "2026-01-15T10:30:00.000Z",
    ...overrides,
  };
}

export function invitationsDefaultScenario(): WorkspaceInvitation[] {
  return [
    workspaceInvitation({
      id: "invite-1",
      token: "tok-1",
      email: "ada@notrelix.dev",
      role: "admin",
      workspaceName: "Product",
      inviterName: "Grace Hopper",
    }),
    workspaceInvitation({
      id: "invite-2",
      token: "tok-2",
      email: "alan@notrelix.dev",
      role: "member",
      workspaceName: "Design",
      inviterName: "Ada Lovelace",
    }),
  ];
}

export function invitationsEmptyScenario(): WorkspaceInvitation[] {
  return [];
}
