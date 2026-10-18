/**
 * Deterministic fixtures and scenarios for the workspace shell surfaces
 * (WX-SHELL-001…005). No clocks, no randomness, no network.
 */
import {
  buildWorkspaceBreadcrumb,
  type WorkspaceBreadcrumbItem,
} from "../core/model/breadcrumb";
import {
  NAVIGATION_RESOURCE_KIND,
  markToneFor,
  type NavigationEntry,
  type WorkspaceDestination,
} from "../core/model/navigation";
import {
  workspaceBreadcrumbLabels,
  type NavigationListState,
  type ShellCurrentWorkspace,
  type ShellNavigation,
  type ShellUser,
  type ShellWorkspace,
} from "../web/shell/shell-models";
import {
  workspaceShellCopy,
  type WorkspaceShellCopy,
} from "../web/shell/workspace-shell.copy";
import type { WorkspaceSidebarSurfaceProps } from "../web/shell/workspace-sidebar-surface";

export const SHELL_FIXTURE_WORKSPACE_ID = "ws-product";

export function shellCopyFixture(
  locale: "en" | "vi" = "en",
): WorkspaceShellCopy {
  return workspaceShellCopy(locale);
}

/** Pure href builder mirroring the web host's routes. */
export function shellHrefFixture(
  destination: WorkspaceDestination,
  workspaceId = SHELL_FIXTURE_WORKSPACE_ID,
): string {
  const base = `/workspaces/${workspaceId}`;
  switch (destination.section) {
    case "home":
      return base;
    case "board":
      return `${base}/boards/${destination.boardId}`;
    case "doc":
      return `${base}/docs/${destination.docId}`;
    case "account":
      return `${base}/account/${destination.page ?? "profile"}`;
    case "space":
      return destination.tab
        ? `${base}/spaces/${destination.spaceId}?tab=${destination.tab}`
        : `${base}/spaces/${destination.spaceId}`;
    default:
      return `${base}/${destination.section}`;
  }
}

export function shellNavigationFixture(
  onNavigate: (destination: WorkspaceDestination) => void = () => undefined,
): ShellNavigation {
  return {
    getHref: (destination) => shellHrefFixture(destination),
    onNavigate,
  };
}

export function shellWorkspace(
  overrides: Partial<ShellCurrentWorkspace> &
    Pick<ShellCurrentWorkspace, "id" | "name">,
): ShellCurrentWorkspace {
  return {
    mark: markToneFor(overrides.id),
    role: "admin",
    ...overrides,
  };
}

export function shellWorkspaceList(): ShellWorkspace[] {
  return [
    { id: "ws-product", name: "Product", mark: "blue" },
    { id: "ws-design", name: "Design Studio", mark: "coral" },
    { id: "ws-ops", name: "Operations", mark: "sage" },
  ];
}

export function shellUser(overrides: Partial<ShellUser> = {}): ShellUser {
  return {
    id: "user-linh",
    name: "Linh Tran",
    email: "linh.tran@notrelix.test",
    avatarUrl: null,
    identity: "gold",
    ...overrides,
  };
}

type SidebarScenario = Omit<
  WorkspaceSidebarSurfaceProps,
  | "navigation"
  | "onOpenSystemRail"
  | "onOpenCommand"
  | "onOpenShortcuts"
  | "onSignOut"
  | "onTogglePin"
  | "onRetryNavigation"
>;

export function navigationEntry(
  overrides: Partial<NavigationEntry> &
    Pick<NavigationEntry, "resourceId" | "name">,
): NavigationEntry {
  return {
    kind: NAVIGATION_RESOURCE_KIND.board,
    at: "2026-10-09T08:00:00.000Z",
    pinned: true,
    ...overrides,
  };
}

function pinnedReady(entries: NavigationEntry[]): NavigationListState {
  return { status: "ready", entries };
}

/** English pins: a Board and a Doc. */
export function pinnedEntriesFixture(): NavigationEntry[] {
  return [
    navigationEntry({ resourceId: "board-roadmap", name: "Product Roadmap" }),
    navigationEntry({
      kind: NAVIGATION_RESOURCE_KIND.doc,
      resourceId: "doc-spec",
      name: "Q4 product specification",
    }),
  ];
}

/** Mockup pins (Minh An Group): a Board and a Doc of Công nghệ. */
export function mockupPinnedEntriesFixture(): NavigationEntry[] {
  return [
    navigationEntry({ resourceId: "board-cn-erp", name: "Triển khai ERP Q4" }),
    navigationEntry({
      kind: NAVIGATION_RESOURCE_KIND.doc,
      resourceId: "doc-cn-handbook",
      name: "Sổ tay vận hành CNTT",
    }),
  ];
}

/** English product world: four departments, My work 7, Inbox 3. */
export function sidebarDefaultScenario(): SidebarScenario {
  return {
    copy: shellCopyFixture(),
    workspace: shellWorkspace({
      id: "ws-product",
      name: "Product",
      mark: "blue",
      departmentCount: 4,
      memberCount: 32,
    }),
    location: { section: "board", boardId: "board-roadmap" },
    counts: { myWork: 7, inbox: 3 },
    pinned: pinnedReady(pinnedEntriesFixture()),
    user: shellUser(),
    commandShortcut: "⌘K",
  };
}

/** Workspace record and counts still loading (counts unavailable). */
export function sidebarLoadingScenario(): SidebarScenario {
  return {
    ...sidebarDefaultScenario(),
    workspace: null,
    location: { section: "home" },
    counts: { myWork: null, inbox: null },
    pinned: { status: "loading" },
  };
}

/**
 * Nothing to do yet: zero counts show no badge. A member, so setup offers
 * only Members and Teams.
 */
export function sidebarEmptyScenario(): SidebarScenario {
  const base = sidebarDefaultScenario();
  return {
    ...base,
    workspace: base.workspace ? { ...base.workspace, role: "member" } : null,
    location: { section: "home" },
    counts: { myWork: 0, inbox: 0 },
    pinned: pinnedReady([]),
  };
}

/** Counts unavailable after a failed load (no badge, never "0"). */
export function sidebarErrorScenario(): SidebarScenario {
  return {
    ...sidebarDefaultScenario(),
    location: { section: "home" },
    counts: { myWork: null, inbox: null },
    pinned: { status: "error" },
  };
}

/** The `Final-Space` mockup (state `tong-quan`): Vietnamese, Minh An Group. */
export function sidebarMockupScenario(
  locale: "vi" | "en" = "vi",
): SidebarScenario {
  return {
    copy: shellCopyFixture(locale),
    workspace: shellWorkspace({
      id: "ws-minh-an",
      name: "Minh An Group",
      mark: "blue",
      role: "admin",
      departmentCount: 5,
      memberCount: 86,
    }),
    location: { section: "space", spaceId: "sp-cn" },
    counts: { myWork: 7, inbox: 3 },
    pinned: pinnedReady(mockupPinnedEntriesFixture()),
    user: shellUser({
      id: "user-thu-ha",
      name: "Nguyễn Thu Hà",
      email: "thuha.nguyen@minhan.test",
      identity: "sage",
    }),
    commandShortcut: "⌘K",
  };
}

/** Long names, a guest, My work 0 (no badge) and Inbox 120 ("99+"). */
export function sidebarEdgeDataScenario(): SidebarScenario {
  return {
    ...sidebarMockupScenario(),
    workspace: shellWorkspace({
      id: "ws-global",
      name: "Không gian làm việc của nhóm Vận hành Toàn cầu",
      role: "guest",
      mark: "crimson",
      departmentCount: 12,
      memberCount: 1250,
    }),
    location: { section: "board", boardId: "board-edge-03" },
    counts: { myWork: 0, inbox: 120 },
    pinned: pinnedReady([
      navigationEntry({
        resourceId: "board-edge-01",
        name: "Kế hoạch đối soát công nợ toàn quốc — giai đoạn hai (bản nháp cuối cùng)",
      }),
      navigationEntry({
        kind: NAVIGATION_RESOURCE_KIND.doc,
        resourceId: "doc-edge-policy",
        name: "Quy trình phê duyệt chi phí 📄",
      }),
      navigationEntry({ resourceId: "board-edge-03", name: "Bảng số 03" }),
    ]),
    user: shellUser({
      name: "Nguyễn Thị Phương Thảo",
      email: "nguyen.thi.phuong.thao.operations@notrelix.test",
      identity: "sage",
    }),
    commandShortcut: "Ctrl K",
  };
}

const shellBoardsFixture: readonly { id: string; name: string }[] = [
  { id: "board-roadmap", name: "Product Roadmap" },
  { id: "board-launch", name: "Launch checklist" },
  { id: "board-bugs", name: "Bug triage" },
];

const shellDocsFixture: readonly { id: string; name: string }[] = [
  { id: "doc-spec", name: "Q4 product specification" },
  { id: "doc-retro", name: "Sprint 42 retro" },
];

export function breadcrumbFixture(
  location: WorkspaceDestination | null,
  options: {
    loadingNames?: boolean;
    workspaceName?: string | null;
    copy?: WorkspaceShellCopy;
  } = {},
): WorkspaceBreadcrumbItem[] {
  const copy = options.copy ?? shellCopyFixture();
  return buildWorkspaceBreadcrumb({
    workspaceName:
      options.workspaceName === undefined ? "Product" : options.workspaceName,
    location,
    boards: options.loadingNames ? null : shellBoardsFixture,
    docs: options.loadingNames ? null : shellDocsFixture,
    labels: workspaceBreadcrumbLabels(copy),
  });
}

/** The document tree of the Docs row on a document page (Final-Docs `binh-thuong`). */
export function sidebarDocsTreeFixture(
  expand: boolean = true,
): import("../web/shell/shell-models").SidebarDocsTree {
  return {
    departments: [
      {
        id: "space-kt",
        name: "Kế toán",
        markTone: "gold",
        pages: [
          {
            id: "page-process",
            title: "Quy trình",
            children: [
              {
                id: "page-invoice",
                title: "Nhập hoá đơn đầu vào",
                children: [],
              },
            ],
          },
        ],
      },
    ],
    activeDocId: "page-invoice",
    expandedIds: new Set(expand ? ["space-kt", "page-process"] : []),
    onExpandedChange: () => undefined,
  };
}
