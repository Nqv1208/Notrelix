/**
 * Deterministic fixtures of the department (Space) page: the data of
 * Final-Space at its clock (12 Oct 2026, 10:40) for every state it declares,
 * plus the loading, error, not-found and read-only states. No clocks, no
 * randomness, no network.
 */
import type { SpaceNode } from "../core/model/spaces";
import type {
  SpaceAccessView,
  SpaceAttentionRowView,
  SpaceBoardRowView,
  SpaceChildView,
  SpaceDocRowView,
  SpaceHeadPeople,
  SpaceTemplateView,
  SpaceWorkloadRowView,
} from "../web/spaces/space-page-models";
import type { WorkspaceSpaceSurfaceProps } from "../web/spaces/workspace-space-surface";
import {
  spaceCandidatesFixture,
  spaceMembersFixture,
  spaceTeamsFixture,
  spacesCopyFixture,
} from "./workspace-spaces-fixtures";
import { shellNavigationFixture } from "./workspace-shell-fixtures";

/** The mockup's clock: 12 Oct 2026, 10:40 (UTC+7). */
export const SPACE_PAGE_NOW = Date.parse("2026-10-12T10:40:00+07:00");

export type SpacePageScenario = Omit<
  WorkspaceSpaceSurfaceProps,
  | "onCreateChild"
  | "onCreateBoard"
  | "onCreateDoc"
  | "onMoveSpace"
  | "onUnarchive"
  | "onSave"
  | "onArchive"
  | "onDelete"
  | "onAddMember"
  | "onChangeMemberRole"
  | "onRemoveMember"
  | "onRetry"
  | "onRetryContent"
  | "onRetryMembers"
>;

const WORKSPACE = "Minh An Group";

function node(
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

export const SPACE_TECH = node({
  id: "sp-cong-nghe",
  name: "Phòng Công nghệ",
  description:
    "Vận hành hạ tầng, phần mềm nội bộ và triển khai ERP cho toàn tập đoàn.",
});

export const SPACE_LEGAL = node({
  id: "sp-phap-che",
  name: "Phòng Pháp chế",
  description: "Vừa tạo. Hiện chỉ bạn và quản trị workspace thấy phòng này.",
});

const person = (
  id: string,
  name: string,
  tone: "blue" | "coral" | "gold" | "sage" | "ember",
) => ({ id, name, tone });

const HP = person("u-phuc", "Lê Hoàng Phúc", "ember");
const HA = person("u-ha", "Nguyễn Thu Hà", "sage");
const MQ = person("u-quan", "Trần Minh Quân", "blue");
const NA = person("u-anh", "Phan Ngọc Anh", "coral");
const VT = person("u-thanh", "Vũ Thanh", "gold");
const TT = person("u-trang", "Lý Thu Trang", "coral");

const PEOPLE_24: SpaceHeadPeople = {
  avatars: [HP, HA, MQ, NA],
  more: 20,
  label: "24 thành viên",
};

function board(
  overrides: Partial<SpaceBoardRowView> &
    Pick<SpaceBoardRowView, "id" | "name">,
): SpaceBoardRowView {
  return {
    icon: "table",
    meta: null,
    isPrivate: false,
    addedCount: null,
    progress: null,
    attention: null,
    people: null,
    updatedAt: null,
    ...overrides,
  };
}

const BOARD_ERP = board({
  id: "b-erp",
  name: "Triển khai ERP Q4",
  icon: "kanban",
  meta: "Kanban · Go-live 01/11 · có 1 khách",
  progress: { kind: "bar", done: 5, total: 16 },
  attention: { tone: "over", text: "1 quá hạn" },
  people: [MQ, HP, NA],
  updatedAt: "2026-10-12T08:40:00+07:00",
});
const BOARD_INFRA = board({
  id: "b-infra",
  name: "Hạ tầng & bảo mật",
  icon: "table",
  meta: "Bảng · nhóm Hạ tầng",
  progress: { kind: "bar", done: 41, total: 57 },
  attention: { tone: "warn", text: "1 chờ duyệt" },
  people: [VT, HP],
  updatedAt: "2026-10-11T16:00:00+07:00",
});
const BOARD_SUPPORT = board({
  id: "b-support",
  name: "Hỗ trợ nội bộ",
  icon: "inbox",
  meta: "Bảng · nhận yêu cầu từ mọi phòng ban",
  progress: { kind: "text", text: "23 yêu cầu mở" },
  attention: { tone: "warn", text: "6 chưa ai nhận" },
  people: [TT, VT],
  updatedAt: "2026-10-12T10:30:00+07:00",
});
const BOARD_BUDGET = board({
  id: "b-budget",
  name: "Ngân sách CNTT 2027",
  icon: "table",
  isPrivate: true,
  addedCount: 4,
  progress: { kind: "bar", done: 3, total: 12 },
  attention: { tone: "ok", text: "Không có" },
  people: [HP, HA],
  updatedAt: "2026-10-07T09:00:00+07:00",
});

const DOCS: SpaceDocRowView[] = [
  {
    id: "d-migration",
    name: "Kế hoạch chuyển đổi dữ liệu",
    meta: "Gắn với 6 việc trong Triển khai ERP Q4",
    editor: "Trần Minh Quân sửa",
    updatedAt: "2026-10-11T15:00:00+07:00",
  },
  {
    id: "d-oncall",
    name: "Sổ tay trực hệ thống",
    meta: "Lịch trực, quy trình xử lý sự cố",
    editor: "Vũ Thanh sửa",
    updatedAt: "2026-10-03T09:00:00+07:00",
  },
];

const CHILD_INFRA: SpaceChildView = {
  id: "sp-ha-tang",
  name: "Nhóm Hạ tầng",
  markTone: "blue",
  isPrivate: false,
  boards: 2,
  people: 6,
};
const CHILD_APPS: SpaceChildView = {
  id: "sp-ung-dung",
  name: "Nhóm Ứng dụng",
  markTone: "blue",
  isPrivate: true,
  boards: 3,
  people: 9,
};

const ATTENTION: SpaceAttentionRowView[] = [
  { id: "overdue", tone: "over", count: 3, label: "Việc quá hạn", href: "#" },
  {
    id: "unassigned",
    tone: "warn",
    count: 6,
    label: "Yêu cầu hỗ trợ chưa ai nhận",
    href: "#",
  },
  {
    id: "review",
    tone: "warn",
    count: 5,
    label: "Việc đang chờ duyệt",
    href: "#",
  },
  {
    id: "stale",
    tone: "mute",
    count: 1,
    label: "Board 14 ngày chưa cập nhật",
    href: "#",
  },
];

const WORKLOAD: SpaceWorkloadRowView[] = [
  {
    id: "w-quan",
    person: MQ,
    active: 7,
    overdue: 2,
    activePct: 62,
    overduePct: 25,
  },
  {
    id: "w-anh",
    person: NA,
    active: 5,
    overdue: 0,
    activePct: 62,
    overduePct: 0,
  },
  {
    id: "w-thanh",
    person: VT,
    active: 4,
    overdue: 1,
    activePct: 37,
    overduePct: 13,
  },
  {
    id: "w-phuc",
    person: HP,
    active: 3,
    overdue: 0,
    activePct: 37,
    overduePct: 0,
  },
  {
    id: "w-trang",
    person: TT,
    active: 2,
    overdue: 0,
    activePct: 25,
    overduePct: 0,
  },
];

const ACCESS_TECH: SpaceAccessView = {
  visibility: "Private",
  summary: "24 người: 2 quản trị · 19 thành viên · 3 người xem",
  rows: [
    {
      key: "admins",
      label: "Quản trị",
      value: "Lê Hoàng Phúc, Nguyễn Thu Hà",
      note: null,
      href: null,
    },
    {
      key: "Member",
      label: "Thành viên",
      value: "19 người",
      note: "gồm nhóm Kỹ sư ERP (4 người)",
      href: null,
    },
    {
      key: "Viewer",
      label: "Người xem",
      value: "3 người",
      note: "nhóm Ban giám đốc, chỉ xem",
      href: null,
    },
    {
      key: "guests",
      label: "Khách ở board",
      value: "Đặng Minh Tuấn",
      note: "chỉ board Triển khai ERP Q4, không thuộc phòng",
      href: "#",
    },
  ],
};

const TEMPLATES: SpaceTemplateView[] = [
  {
    id: "contract",
    title: "Theo dõi hợp đồng",
    hint: "Soạn thảo, rà soát, ký, lưu hồ sơ · trường Đối tác, Giá trị, Ngày hết hạn",
    icon: "contract",
  },
  {
    id: "intake",
    title: "Tiếp nhận yêu cầu pháp lý",
    hint: "Các phòng ban gửi yêu cầu qua biểu mẫu, Pháp chế nhận và trả lời",
    icon: "intake",
  },
  {
    id: "calendar",
    title: "Lịch hạn giấy phép và văn bản",
    hint: "Lịch nhắc trước 30 ngày khi giấy phép sắp hết hạn",
    icon: "calendar",
  },
];

function base(): SpacePageScenario {
  return {
    copy: spacesCopyFixture("vi"),
    navigation: shellNavigationFixture(),
    now: SPACE_PAGE_NOW,
    space: { status: "ready", data: SPACE_TECH },
    workspaceName: WORKSPACE,
    ancestors: [],
    tab: "overview",
    people: PEOPLE_24,
    peopleTotal: 24,
    contentCount: 9,
    membersCount: 24,
    content: {
      status: "ready",
      data: {
        boards: [BOARD_ERP, BOARD_INFRA, BOARD_SUPPORT, BOARD_BUDGET],
        boardTotal: 6,
        docs: DOCS,
        docTotal: 3,
      },
    },
    childSpaces: [CHILD_INFRA, CHILD_APPS],
    access: ACCESS_TECH,
    attention: ATTENTION,
    workload: WORKLOAD,
    templates: [],
    firstSteps: false,
    viewAs: null,
    members: { status: "ready", data: spaceMembersFixture() },
    candidateUsers: { status: "ready", data: spaceCandidatesFixture() },
    candidateTeams: { status: "ready", data: spaceTeamsFixture() },
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

/** Final-Space `tong-quan`. */
export function spacePageOverviewScenario(): SpacePageScenario {
  return base();
}

/** Final-Space `tao-moi`: the Create menu open. */
export function spacePageCreateMenuScenario(): SpacePageScenario {
  return { ...base(), initialMenuOpen: true };
}

/** Final-Space `phong-moi`: a new department with its three first steps. */
export function spacePageNewScenario(): SpacePageScenario {
  return {
    ...base(),
    space: { status: "ready", data: SPACE_LEGAL },
    people: {
      avatars: [HA],
      more: 0,
      label: "1 thành viên",
    },
    peopleTotal: 1,
    contentCount: 0,
    membersCount: 1,
    content: {
      status: "ready",
      data: { boards: [], boardTotal: 0, docs: [], docTotal: 0 },
    },
    childSpaces: [],
    firstSteps: true,
    attention: null,
    workload: null,
    templates: TEMPLATES,
    access: {
      visibility: "Private",
      summary: null,
      rows: [
        {
          key: "admins",
          label: "Quản trị",
          value: "Nguyễn Thu Hà",
          note: "bạn · người tạo",
          href: null,
        },
        {
          key: "Member",
          label: "Thành viên",
          value: "Chưa có ai",
          note: null,
          href: null,
          empty: true,
        },
      ],
    },
  };
}

/** Final-Space `thiet-lap`: settings with the delete confirmation open. */
export function spacePageSettingsScenario(): SpacePageScenario {
  // The mockup lets Archive through with sub-departments inside; the page
  // blocks it (WSP-022) once it knows the content, so this state is drawn
  // before the content is known.
  return {
    ...base(),
    tab: "settings",
    initialDeleteOpen: true,
    content: { status: "loading" },
  };
}

/** Final-Space `chi-xem`: previewing what a department member sees. */
export function spacePageViewAsScenario(): SpacePageScenario {
  const scenario = base();
  return {
    ...scenario,
    viewAs: { onExit: () => undefined },
    people: { ...PEOPLE_24, more: 21, label: "24 thành viên" },
    contentCount: 8,
    content: {
      status: "ready",
      data: {
        boards: [BOARD_ERP, BOARD_INFRA, BOARD_SUPPORT],
        boardTotal: 5,
        docs: DOCS,
        docTotal: 3,
      },
    },
    childSpaces: [CHILD_INFRA],
  };
}

export function spacePageLoadingScenario(): SpacePageScenario {
  return { ...base(), space: { status: "loading" } };
}

export function spacePageErrorScenario(): SpacePageScenario {
  return { ...base(), space: { status: "error" } };
}

export function spacePageNotFoundScenario(): SpacePageScenario {
  return { ...base(), space: { status: "ready", data: null } };
}

/** The Boards and Docs are still loading under a ready head. */
export function spacePageContentLoadingScenario(): SpacePageScenario {
  return { ...base(), content: { status: "loading" } };
}

export function spacePageContentErrorScenario(): SpacePageScenario {
  return { ...base(), content: { status: "error" } };
}

/** Viewer access: no Add members, Create or Settings. */
export function spacePageReadOnlyScenario(): SpacePageScenario {
  return {
    ...base(),
    space: { status: "ready", data: { ...SPACE_TECH, access: "Viewer" } },
    attention: null,
    workload: null,
  };
}

/** Editor access: may create, may not manage the department. */
export function spacePageEditorScenario(): SpacePageScenario {
  return {
    ...base(),
    space: { status: "ready", data: { ...SPACE_TECH, access: "Editor" } },
    attention: null,
    workload: null,
  };
}

export function spacePageArchivedScenario(): SpacePageScenario {
  return {
    ...base(),
    space: { status: "ready", data: { ...SPACE_TECH, isArchived: true } },
  };
}

/** The columns and blocks the producers can fill today (no progress, people). */
export function spacePageProducedOnlyScenario(): SpacePageScenario {
  const plain = (row: SpaceBoardRowView): SpaceBoardRowView => ({
    ...row,
    meta: null,
    progress: null,
    attention: null,
    people: null,
  });
  return {
    ...base(),
    people: { avatars: [HP, HA, MQ], more: 1, label: "4 người và nhóm" },
    contentCount: 7,
    membersCount: 4,
    content: {
      status: "ready",
      data: {
        boards: [BOARD_ERP, BOARD_INFRA, BOARD_BUDGET].map(plain),
        boardTotal: 3,
        docs: DOCS.map((doc) => ({ ...doc, meta: null, editor: null })),
        docTotal: 2,
      },
    },
    childSpaces: [
      { ...CHILD_INFRA, people: null },
      { ...CHILD_APPS, people: null },
    ],
    attention: null,
    workload: null,
    access: {
      ...ACCESS_TECH,
      summary: "3 người và 1 nhóm: 1 quản trị · 2 thành viên · 1 người xem",
      rows: ACCESS_TECH.rows.slice(0, 3),
    },
  };
}

/** The settings of a department the member may only read. */
export function spacePageSettingsReadOnlyScenario(): SpacePageScenario {
  return {
    ...spacePageReadOnlyScenario(),
    tab: "settings",
  };
}

/** The Members tab (the members panel, until its own design lands). */
export function spacePageMembersScenario(): SpacePageScenario {
  return { ...base(), tab: "members" };
}

/** Settings of a department that still has content: Archive is blocked. */
export function spacePageSettingsBlockedScenario(): SpacePageScenario {
  return { ...base(), tab: "settings" };
}

/** Settings of an empty leaf department: Archive is offered. */
export function spacePageSettingsEmptyScenario(): SpacePageScenario {
  return {
    ...base(),
    tab: "settings",
    childSpaces: [],
    content: {
      status: "ready",
      data: { boards: [], boardTotal: 0, docs: [], docTotal: 0 },
    },
  };
}
