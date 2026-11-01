import type {
  PendingInvitation,
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

export function invitationsDefaultScenario(): PendingInvitation[] {
  return [
    {
      id: "invite-1",
      workspaceName: "Product",
      workspaceSlug: "product",
      inviterName: "Grace Hopper",
      role: "member",
      expiresAt: "2026-02-15T10:30:00.000Z",
    },
    {
      id: "invite-2",
      workspaceName: "Design",
      workspaceSlug: "design",
      inviterName: "Ada Lovelace",
      role: "guest",
      expiresAt: "2026-02-20T10:30:00.000Z",
    },
  ];
}

export function invitationsEmptyScenario(): PendingInvitation[] {
  return [];
}
