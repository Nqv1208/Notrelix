import type {
  WorkspaceActivityItem,
  WorkspaceInvitation,
  WorkspaceMember,
  WorkspaceSummary,
  WorkspaceView,
} from "../core/types/workspace";

export function workspaceSummary(
  overrides: Partial<WorkspaceSummary> & Pick<WorkspaceSummary, "id" | "name">,
): WorkspaceSummary {
  return {
    slug: overrides.slug ?? overrides.id,
    description: overrides.description,
    icon: overrides.icon ?? "N",
    plan: overrides.plan ?? "pro",
    memberCount: overrides.memberCount ?? 12,
    isPersonal: overrides.isPersonal ?? false,
    ...overrides,
  };
}

export function workspaceMember(
  overrides: Partial<WorkspaceMember> & Pick<WorkspaceMember, "id">,
): WorkspaceMember {
  return {
    userId: overrides.userId ?? overrides.id,
    name: overrides.name ?? `Member ${overrides.id}`,
    initials: overrides.initials ?? "M",
    role: overrides.role ?? "member",
    status: overrides.status ?? "active",
    workload: overrides.workload ?? 0,
    color: overrides.color ?? "#6161ff",
    ...overrides,
  };
}

export function workspaceView(
  overrides: Partial<WorkspaceView> & Pick<WorkspaceView, "id" | "name">,
): WorkspaceView {
  return {
    workspaceId: overrides.workspaceId ?? "ws-main",
    type: overrides.type ?? "table",
    icon: overrides.icon ?? "▦",
    description: overrides.description ?? "Workspace view",
    target: overrides.target ?? {},
    config: overrides.config ?? {},
    visibility: overrides.visibility ?? "workspace",
    isDefault: overrides.isDefault ?? false,
    position: overrides.position ?? 0,
    createdAt: overrides.createdAt ?? "2026-01-15T10:30:00.000Z",
    ...overrides,
  };
}

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

export function workspaceDirectoryDefaultScenario(): WorkspaceSummary[] {
  return [
    workspaceSummary({ id: "ws-product", name: "Product", plan: "pro" }),
    workspaceSummary({ id: "ws-design", name: "Design", plan: "free" }),
    workspaceSummary({
      id: "ws-enterprise",
      name: "Enterprise Rollout",
      plan: "enterprise",
      memberCount: 84,
    }),
  ];
}

export function workspaceDirectoryEmptyScenario(): WorkspaceSummary[] {
  return [];
}

export function workspaceDirectoryEdgeDataScenario(): WorkspaceSummary[] {
  return [
    workspaceSummary({
      id: "ws-global-enterprise-program",
      name: "Global Enterprise Program",
      plan: "enterprise",
      memberCount: 128,
      description: "Cross-region rollout governance",
    }),
    workspaceSummary({ id: "ws-personal", name: "Personal", isPersonal: true }),
  ];
}

export function workspaceHeaderDefaultScenario() {
  return {
    workspace: workspaceSummary({ id: "ws-product", name: "Product" }),
    members: [
      workspaceMember({ id: "m-ada", name: "Ada Lovelace", initials: "AL" }),
      workspaceMember({ id: "m-alan", name: "Alan Turing", initials: "AT" }),
      workspaceMember({ id: "m-grace", name: "Grace Hopper", initials: "GH" }),
    ],
  };
}

export function workspaceTabsDefaultScenario(): WorkspaceView[] {
  return [
    workspaceView({
      id: "view-board",
      name: "Board",
      type: "kanban",
      isDefault: true,
    }),
    workspaceView({ id: "view-table", name: "Table", type: "table" }),
    workspaceView({ id: "view-calendar", name: "Calendar", type: "calendar" }),
  ];
}

export function workspaceTabsEmptyScenario(): WorkspaceView[] {
  return [];
}

export function workspaceTabsEdgeDataScenario(): WorkspaceView[] {
  return [
    workspaceView({
      id: "view-enterprise-tracker",
      name: "Enterprise Rollout Tracker",
      type: "table",
    }),
    workspaceView({
      id: "view-enterprise-calendar",
      name: "Regional Go-Live Calendar",
      type: "calendar",
    }),
    workspaceView({
      id: "view-enterprise-dashboard",
      name: "Program Health Dashboard",
      type: "dashboard",
    }),
    workspaceView({
      id: "view-enterprise-docs",
      name: "Decision Log",
      type: "doc",
    }),
  ];
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

export function dashboardDefaultScenario() {
  return {
    workspaceName: "Product",
    pageCount: 24,
    boardCount: 7,
    memberCount: 12,
    status: "idle" as const,
    activities: [
      workspaceActivityItem({
        id: "act-1",
        actor: "Ada Lovelace",
        action: "edited",
        target: "Operating plan",
      }),
      workspaceActivityItem({
        id: "act-2",
        actor: "Alan Turing",
        action: "commented",
        target: "Migration risks",
      }),
      workspaceActivityItem({
        id: "act-3",
        actor: "Grace Hopper",
        action: "created",
        target: "Release notes",
      }),
    ],
  };
}

export function dashboardEmptyScenario() {
  return {
    ...dashboardDefaultScenario(),
    pageCount: 0,
    boardCount: 0,
    memberCount: 0,
    activities: [],
  };
}

export function dashboardLoadingScenario() {
  return {
    ...dashboardDefaultScenario(),
    status: "loading" as const,
  };
}
