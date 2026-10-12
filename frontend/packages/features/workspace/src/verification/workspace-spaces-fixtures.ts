/**
 * Deterministic fixtures for the Space org tree surfaces (OT-5).
 * No clocks, no randomness, no network.
 */
import type {
  MoveTargets,
  SpaceMemberEntry,
  SpaceNode,
  SpaceTeam,
} from "../core/model/spaces";
import {
  buildSpaceIndex,
  childrenOf,
  moveTargets,
  spacePath,
} from "../core/model/spaces";
import type { CreateSpaceDialogProps } from "../web/spaces/create-space-dialog";
import type { MoveToSpaceDialogProps } from "../web/spaces/move-to-space-dialog";
import type { SpaceMemberCandidate } from "../web/spaces/space-members-panel";
import type {
  SpaceBoardLink,
  SpaceTreeNavProps,
} from "../web/spaces/space-tree-nav";
import type { WorkspaceSpaceSurfaceProps } from "../web/spaces/workspace-space-surface";
import {
  workspaceSpacesCopy,
  type WorkspaceSpacesCopy,
} from "../web/spaces/workspace-spaces.copy";
import { shellNavigationFixture } from "./workspace-shell-fixtures";
import { homeFormattersFixture } from "./workspace-home-fixtures";

export function spacesCopyFixture(
  locale: "en" | "vi" = "en",
): WorkspaceSpacesCopy {
  return workspaceSpacesCopy(locale);
}

export function spaceNode(
  overrides: Partial<SpaceNode> & Pick<SpaceNode, "id" | "name">,
): SpaceNode {
  return {
    parentId: null,
    description: null,
    type: "Department",
    visibility: "Private",
    isArchived: false,
    position: "a0",
    version: 3,
    createdAt: "2026-09-01T08:00:00.000Z",
    access: "Manager",
    ...overrides,
  };
}

/** Departments with nested teams; the caller administers the workspace. */
export function spaceTreeFixture(): SpaceNode[] {
  return [
    spaceNode({ id: "sp-it", name: "IT", position: "a0" }),
    spaceNode({
      id: "sp-it-platform",
      name: "Platform",
      parentId: "sp-it",
      type: "Folder",
      visibility: "Workspace",
      position: "a0",
    }),
    spaceNode({
      id: "sp-it-infra",
      name: "Infrastructure",
      parentId: "sp-it-platform",
      type: "ProjectArea",
      visibility: "Workspace",
      position: "a0",
    }),
    spaceNode({
      id: "sp-it-security",
      name: "Security",
      parentId: "sp-it",
      type: "Folder",
      position: "a1",
    }),
    spaceNode({
      id: "sp-accounting",
      name: "Accounting",
      position: "a1",
      access: "Editor",
    }),
    spaceNode({
      id: "sp-accounting-payroll",
      name: "Payroll",
      parentId: "sp-accounting",
      type: "Folder",
      position: "a0",
      access: "Editor",
    }),
    spaceNode({
      id: "sp-marketing",
      name: "Marketing",
      position: "a2",
      visibility: "Workspace",
      access: "Viewer",
    }),
    spaceNode({
      id: "sp-archive",
      name: "2025 campaigns",
      position: "a3",
      type: "Portfolio",
      visibility: "Workspace",
      isArchived: true,
    }),
  ];
}

/** Long and Vietnamese names, deep nesting (WSP-021: depth is unlimited). */
export function spaceTreeEdgeFixture(): SpaceNode[] {
  const nodes: SpaceNode[] = [
    spaceNode({
      id: "sp-edge-root",
      name: "Phòng Kế toán – Tài chính doanh nghiệp và Kiểm soát nội bộ khu vực miền Nam",
      position: "a0",
    }),
  ];
  let parent = "sp-edge-root";
  for (let depth = 1; depth <= 7; depth += 1) {
    const id = `sp-edge-${depth}`;
    nodes.push(
      spaceNode({
        id,
        name: `Cấp ${depth} — Nhóm đối soát ngân hàng`,
        parentId: parent,
        type: depth % 2 ? "Folder" : "ProjectArea",
        visibility: depth % 3 ? "Workspace" : "Private",
        position: "a0",
      }),
    );
    parent = id;
  }
  nodes.push(
    spaceNode({
      id: "sp-edge-sibling",
      name: "Đội IT",
      position: "a1",
      visibility: "Workspace",
    }),
  );
  return nodes;
}

/** 25 departments × 20 teams: 525 Spaces, for windowing. */
export function spaceTreeHighDensityFixture(): SpaceNode[] {
  const nodes: SpaceNode[] = [];
  for (let d = 0; d < 25; d += 1) {
    const departmentId = `sp-dept-${String(d).padStart(2, "0")}`;
    nodes.push(
      spaceNode({
        id: departmentId,
        name: `Department ${String(d + 1).padStart(2, "0")}`,
        position: `a${String(d).padStart(2, "0")}`,
      }),
    );
    for (let t = 0; t < 20; t += 1) {
      nodes.push(
        spaceNode({
          id: `${departmentId}-team-${String(t).padStart(2, "0")}`,
          name: `Team ${String(d + 1).padStart(2, "0")}.${String(t + 1).padStart(2, "0")}`,
          parentId: departmentId,
          type: "Folder",
          visibility: "Workspace",
          position: `a${String(t).padStart(2, "0")}`,
        }),
      );
    }
  }
  return nodes;
}

export const ALL_DEPARTMENTS_EXPANDED = new Set(
  Array.from({ length: 25 }, (_, d) => `sp-dept-${String(d).padStart(2, "0")}`),
);

export function spaceMembersFixture(): SpaceMemberEntry[] {
  return [
    {
      subjectType: "User",
      subjectId: "user-linh",
      role: "Admin",
      displayName: "Linh Tran",
      avatarUrl: null,
      addedAt: "2026-09-01T08:00:00.000Z",
    },
    {
      subjectType: "Team",
      subjectId: "team-helpdesk",
      role: "Member",
      displayName: "Helpdesk",
      avatarUrl: null,
      addedAt: "2026-09-02T08:00:00.000Z",
    },
    {
      subjectType: "User",
      subjectId: "user-minh",
      role: "Viewer",
      displayName: "Minh Pham",
      avatarUrl: null,
      addedAt: "2026-09-03T08:00:00.000Z",
    },
  ];
}

export function spaceTeamsFixture(): SpaceTeam[] {
  return [
    { id: "team-helpdesk", name: "Helpdesk", memberCount: 6 },
    { id: "team-sre", name: "SRE", memberCount: 4 },
    { id: "team-audit", name: "Audit", memberCount: 1 },
  ];
}

export function spaceCandidatesFixture(): SpaceMemberCandidate[] {
  return [
    { id: "user-linh", name: "Linh Tran", avatarUrl: null },
    { id: "user-minh", name: "Minh Pham", avatarUrl: null },
    { id: "user-an", name: "An Nguyen", avatarUrl: null },
    { id: "user-bao", name: "Bao Le", avatarUrl: null },
  ];
}

// ── Sidebar tree ──────────────────────────────────────────────────

type TreeScenario = Omit<
  SpaceTreeNavProps,
  "onExpandedChange" | "onCreateSpace" | "onRetry" | "onNavigated"
>;

export function spaceTreeDefaultScenario(): TreeScenario {
  return {
    copy: spacesCopyFixture(),
    navigation: shellNavigationFixture(),
    state: { status: "ready", nodes: spaceTreeFixture() },
    expandedIds: new Set(["sp-it", "sp-it-platform"]),
    activeSpaceId: "sp-it-infra",
    canCreateRoot: true,
  };
}

export function spaceTreeLoadingScenario(): TreeScenario {
  return { ...spaceTreeDefaultScenario(), state: { status: "loading" } };
}

export function spaceTreeErrorScenario(): TreeScenario {
  return { ...spaceTreeDefaultScenario(), state: { status: "error" } };
}

export function spaceTreeEmptyScenario(): TreeScenario {
  return {
    ...spaceTreeDefaultScenario(),
    state: { status: "ready", nodes: [] },
    activeSpaceId: null,
  };
}

export function spaceTreeReadOnlyScenario(): TreeScenario {
  return {
    ...spaceTreeDefaultScenario(),
    canCreateRoot: false,
    activeSpaceId: null,
    expandedIds: new Set(["sp-accounting"]),
  };
}

export function spaceTreeEdgeDataScenario(): TreeScenario {
  const nodes = spaceTreeEdgeFixture();
  return {
    ...spaceTreeDefaultScenario(),
    copy: spacesCopyFixture("vi"),
    state: { status: "ready", nodes },
    expandedIds: new Set(nodes.map((node) => node.id)),
    activeSpaceId: "sp-edge-5",
  };
}

export function spaceTreeHighDensityScenario(): TreeScenario {
  return {
    ...spaceTreeDefaultScenario(),
    state: { status: "ready", nodes: spaceTreeHighDensityFixture() },
    expandedIds: ALL_DEPARTMENTS_EXPANDED,
    activeSpaceId: null,
  };
}

type SidebarSpaces = Omit<SpaceTreeNavProps, "navigation" | "onNavigated">;

function sidebarSpaces(scenario: TreeScenario): SidebarSpaces {
  const { navigation: _navigation, ...rest } = scenario;
  return {
    ...rest,
    onExpandedChange: () => undefined,
    onCreateSpace: () => undefined,
    onRetry: () => undefined,
  };
}

/** The sidebar's `spaces` prop (Space org tree in the workspace sidebar). */
export function sidebarSpacesFixture(): SidebarSpaces {
  return sidebarSpaces(spaceTreeDefaultScenario());
}

function board(id: string, name: string, isPrivate = false): SpaceBoardLink {
  return { id, name, isPrivate };
}

/** Boards of the default tree: IT has two (one private), Marketing one. */
export function spaceBoardsFixture(): ReadonlyMap<
  string,
  readonly SpaceBoardLink[]
> {
  return new Map([
    [
      "sp-it",
      [
        board("board-it-erp", "ERP rollout"),
        board("board-it-budget", "IT budget 2027", true),
      ],
    ],
    ["sp-marketing", [board("board-mk-calendar", "Campaign calendar")]],
  ]);
}

/** The default tree with Boards; IT is open, nothing is current. */
export function spaceTreeBoardsScenario(): TreeScenario {
  return {
    ...spaceTreeDefaultScenario(),
    boardsBySpace: spaceBoardsFixture(),
    expandedIds: new Set(["sp-it"]),
    activeSpaceId: null,
  };
}

/** Departments of the `Final-Space` mockup (Minh An Group). */
export function departmentTreeFixture(): SpaceNode[] {
  return [
    spaceNode({ id: "sp-cn", name: "Công nghệ", position: "a0" }),
    spaceNode({
      id: "sp-cn-ht",
      name: "Nhóm Hạ tầng",
      parentId: "sp-cn",
      visibility: "Workspace",
      position: "a0",
    }),
    spaceNode({
      id: "sp-cn-ud",
      name: "Nhóm Ứng dụng",
      parentId: "sp-cn",
      position: "a1",
    }),
    spaceNode({ id: "sp-kt", name: "Kế toán", position: "a1" }),
    spaceNode({
      id: "sp-mk",
      name: "Marketing",
      position: "a2",
      visibility: "Workspace",
    }),
    spaceNode({ id: "sp-ns", name: "Nhân sự", position: "a3" }),
    spaceNode({ id: "sp-gd", name: "Ban giám đốc", position: "a4" }),
  ];
}

export function departmentBoardsFixture(): ReadonlyMap<
  string,
  readonly SpaceBoardLink[]
> {
  return new Map([
    [
      "sp-cn",
      [
        board("board-cn-erp", "Triển khai ERP Q4"),
        board("board-cn-infra", "Hạ tầng & bảo mật"),
        board("board-cn-support", "Hỗ trợ nội bộ"),
        board("board-cn-budget", "Ngân sách CNTT 2027", true),
      ],
    ],
    ["sp-kt", [board("board-kt-close", "Khóa sổ tháng 10")]],
    ["sp-mk", [board("board-mk-campaign", "Chiến dịch Tết 2027")]],
  ]);
}

/** The mockup sidebar tree: Công nghệ open and current. */
export function sidebarDepartmentsFixture(): SidebarSpaces {
  return sidebarSpaces({
    copy: spacesCopyFixture("vi"),
    navigation: shellNavigationFixture(),
    state: { status: "ready", nodes: departmentTreeFixture() },
    boardsBySpace: departmentBoardsFixture(),
    expandedIds: new Set(["sp-cn"]),
    activeSpaceId: "sp-cn",
    canCreateRoot: true,
  });
}

/**
 * Edge data: a long department name with 12 Boards (one long, two private),
 * a nested sub-department two levels deep, and a department with 0 Boards.
 */
export function sidebarDepartmentsEdgeFixture(): SidebarSpaces {
  const root = "sp-edge-root";
  const nodes = [
    spaceNode({
      id: root,
      name: "Phòng Kế toán – Tài chính doanh nghiệp và Kiểm soát nội bộ khu vực miền Nam",
      position: "a0",
    }),
    spaceNode({
      id: "sp-edge-1",
      name: "Nhóm đối soát ngân hàng và công nợ phải thu",
      parentId: root,
      visibility: "Workspace",
      position: "a0",
    }),
    spaceNode({
      id: "sp-edge-2",
      name: "Tổ đối soát chi nhánh Cần Thơ",
      parentId: "sp-edge-1",
      position: "a0",
    }),
    spaceNode({
      id: "sp-edge-empty",
      name: "Đội IT",
      position: "a1",
      visibility: "Workspace",
    }),
  ];
  const boards = Array.from({ length: 12 }, (_, index) => {
    const number = String(index + 1).padStart(2, "0");
    return board(
      `board-edge-${number}`,
      index === 0
        ? "Kế hoạch đối soát công nợ toàn quốc — giai đoạn hai (bản nháp cuối cùng)"
        : `Bảng số ${number}`,
      index === 3 || index === 7,
    );
  });
  return sidebarSpaces({
    copy: spacesCopyFixture("vi"),
    navigation: shellNavigationFixture(),
    state: { status: "ready", nodes },
    boardsBySpace: new Map([
      [root, boards],
      ["sp-edge-2", [board("board-edge-nested", "Đối soát tháng 10", true)]],
    ]),
    expandedIds: new Set([root, "sp-edge-1", "sp-edge-2"]),
    activeSpaceId: null,
    canCreateRoot: false,
  });
}

/** Sidebar tree states for the sidebar stories. */
export function sidebarDepartmentsStateFixture(
  status: "loading" | "error" | "empty",
): SidebarSpaces {
  return {
    ...sidebarDepartmentsFixture(),
    state:
      status === "empty"
        ? { status: "ready", nodes: [] }
        : { status },
    activeSpaceId: null,
  };
}

// ── Space page ────────────────────────────────────────────────────

type PageScenario = Omit<
  WorkspaceSpaceSurfaceProps,
  | "onTabChange"
  | "onCreateChild"
  | "onCreateBoard"
  | "onCreateDoc"
  | "onMoveSpace"
  | "onMoveBoard"
  | "onMoveDoc"
  | "onUnarchive"
  | "onSave"
  | "onArchive"
  | "onAddMember"
  | "onChangeMemberRole"
  | "onRemoveMember"
  | "onRetry"
  | "onRetryContent"
  | "onRetryMembers"
>;

function pageFor(
  nodes: readonly SpaceNode[],
  spaceId: string,
  tab: PageScenario["tab"] = "overview",
): PageScenario {
  const index = buildSpaceIndex(nodes);
  return {
    copy: spacesCopyFixture(),
    navigation: shellNavigationFixture(),
    formatters: homeFormattersFixture(),
    space: { status: "ready", data: index.byId.get(spaceId) ?? null },
    path: spacePath(index, spaceId),
    childSpaces: childrenOf(index, spaceId),
    content: {
      status: "ready",
      data: {
        boards: [
          {
            id: "board-it-roadmap",
            name: "IT roadmap",
            updatedAt: "2026-10-08T09:00:00.000Z",
          },
          { id: "board-it-tickets", name: "Service desk", updatedAt: null },
        ],
        docs: [
          {
            id: "doc-it-onboarding",
            name: "Onboarding checklist",
            updatedAt: "2026-10-05T09:00:00.000Z",
          },
        ],
      },
    },
    members: { status: "ready", data: spaceMembersFixture() },
    candidateUsers: { status: "ready", data: spaceCandidatesFixture() },
    candidateTeams: { status: "ready", data: spaceTeamsFixture() },
    tab,
    canMoveBoards: true,
    canMoveDocs: true,
    pendingMemberKeys: new Set(),
    addingMember: false,
    addMemberError: null,
    addMemberResetKey: 0,
    saveStatus: "idle",
    archiving: false,
    archiveRefused: false,
    unarchiving: false,
  };
}

export function spacePageDefaultScenario(): PageScenario {
  return pageFor(spaceTreeFixture(), "sp-it");
}

export function spacePageMembersScenario(): PageScenario {
  return pageFor(spaceTreeFixture(), "sp-it", "members");
}

export function spacePageSettingsScenario(): PageScenario {
  return pageFor(spaceTreeFixture(), "sp-it", "settings");
}

/** An empty leaf Space: archive is offered. */
export function spacePageEmptyScenario(): PageScenario {
  return {
    ...pageFor(spaceTreeFixture(), "sp-it-security"),
    content: { status: "ready", data: { boards: [], docs: [] } },
  };
}

export function spacePageLoadingScenario(): PageScenario {
  return { ...spacePageDefaultScenario(), space: { status: "loading" } };
}

export function spacePageErrorScenario(): PageScenario {
  return { ...spacePageDefaultScenario(), space: { status: "error" } };
}

export function spacePageNotFoundScenario(): PageScenario {
  return {
    ...spacePageDefaultScenario(),
    space: { status: "ready", data: null },
    path: [],
    childSpaces: [],
  };
}

/** Viewer access: no create, move, member or settings actions. */
export function spacePageReadOnlyScenario(): PageScenario {
  return {
    ...pageFor(spaceTreeFixture(), "sp-marketing"),
    canMoveBoards: false,
    canMoveDocs: false,
  };
}

/** Editor access: may add Boards and Docs, may not manage the Space. */
export function spacePagePermissionLimitedScenario(): PageScenario {
  return pageFor(spaceTreeFixture(), "sp-accounting");
}

export function spacePageArchivedScenario(): PageScenario {
  return {
    ...pageFor(spaceTreeFixture(), "sp-archive"),
    content: { status: "ready", data: { boards: [], docs: [] } },
  };
}

export function spacePageEdgeDataScenario(): PageScenario {
  const nodes = spaceTreeEdgeFixture();
  return {
    ...pageFor(nodes, "sp-edge-6"),
    copy: spacesCopyFixture("vi"),
    formatters: homeFormattersFixture("vi"),
    content: {
      status: "ready",
      data: {
        boards: [
          {
            id: "board-edge",
            name: "Bảng theo dõi đối soát công nợ phải thu và phải trả của toàn bộ chi nhánh",
            updatedAt: "2026-10-08T09:00:00.000Z",
          },
        ],
        docs: [],
      },
    },
  };
}

// ── Dialogs ───────────────────────────────────────────────────────

type CreateScenario = Omit<CreateSpaceDialogProps, "onOpenChange" | "onSubmit">;

export function createSpaceDefaultScenario(): CreateScenario {
  return {
    copy: spacesCopyFixture(),
    open: true,
    parent: null,
    workspaceName: "Product",
    pending: false,
    error: null,
  };
}

export function createSpaceChildScenario(): CreateScenario {
  return {
    ...createSpaceDefaultScenario(),
    parent: { id: "sp-it", name: "IT" },
  };
}

export function createSpacePendingScenario(): CreateScenario {
  return { ...createSpaceDefaultScenario(), pending: true };
}

export function createSpaceErrorScenario(): CreateScenario {
  return {
    ...createSpaceDefaultScenario(),
    error: "A space with this name already exists under the same parent.",
  };
}

type MoveScenario = Omit<MoveToSpaceDialogProps, "onOpenChange" | "onSubmit">;

function targetsFor(
  nodes: readonly SpaceNode[],
  subject: Parameters<typeof moveTargets>[1],
): MoveTargets {
  return moveTargets(buildSpaceIndex(nodes), subject, "admin");
}

export function moveSpaceScenario(): MoveScenario {
  const nodes = spaceTreeFixture();
  const space = nodes.find((node) => node.id === "sp-it-platform")!;
  return {
    copy: spacesCopyFixture(),
    subject: { kind: "space", space },
    workspaceName: "Product",
    nodes,
    targets: targetsFor(nodes, { kind: "space", spaceId: space.id }),
    pending: false,
    error: null,
  };
}

export function moveBoardScenario(): MoveScenario {
  const nodes = spaceTreeFixture();
  return {
    ...moveSpaceScenario(),
    subject: {
      kind: "board",
      id: "board-it-roadmap",
      name: "IT roadmap",
      spaceId: "sp-it",
    },
    targets: targetsFor(nodes, { kind: "board" }),
  };
}

/** A root Doc placed in IT; a nested page is never offered the dialog. */
export function movePageScenario(): MoveScenario {
  const nodes = spaceTreeFixture();
  return {
    ...moveSpaceScenario(),
    subject: {
      kind: "page",
      id: "doc-it-onboarding",
      name: "Onboarding checklist",
      spaceId: "sp-it",
    },
    targets: targetsFor(nodes, { kind: "page" }),
  };
}

/** A Board moved across the 525-Space tree; the picker is windowed. */
export function moveHighDensityScenario(): MoveScenario {
  const nodes = spaceTreeHighDensityFixture();
  return {
    ...moveSpaceScenario(),
    subject: {
      kind: "board",
      id: "board-high-density",
      name: "Quarterly plan",
      spaceId: "sp-dept-00-team-00",
    },
    workspaceName: "Product",
    nodes,
    targets: targetsFor(nodes, { kind: "board" }),
  };
}

export function moveErrorScenario(): MoveScenario {
  return {
    ...moveSpaceScenario(),
    error:
      "Someone changed this Space at the same time. The latest version is loaded; try again.",
  };
}

export function movePendingScenario(): MoveScenario {
  return { ...moveSpaceScenario(), pending: true };
}
