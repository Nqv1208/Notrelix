/**
 * Deterministic fixtures for the Teams pages inside "Thiết lập workspace"
 * (Final-Teams): the list, the create and archive dialogs, one Team and the
 * Sharing and security page. The data is the mockup's (Minh An Group's four
 * Teams and the archived Nhóm Trực hệ thống). No clocks, no randomness, no
 * network.
 */
import type { Team, TeamMember } from "../core/model/teams";
import type {
  CreateTeamDialogProps,
  TeamCandidate,
} from "../web/teams/create-team-dialog";
import type { WorkspaceSecuritySurfaceProps } from "../web/teams/workspace-security-surface";
import type { SetupFrameProps } from "../web/teams/workspace-setup-frame";
import type { WorkspaceTeamSurfaceProps } from "../web/teams/workspace-team-surface";
import type { WorkspaceTeamsSurfaceProps } from "../web/teams/workspace-teams-surface";
import {
  archiveLoss,
  buildTeamRows,
  type TeamExt,
  type TeamsExtData,
} from "../web/teams/workspace-teams-models";
import {
  workspaceTeamsCopy,
  type WorkspaceTeamsCopy,
} from "../web/teams/workspace-teams.copy";
import { shellNavigationFixture } from "./workspace-shell-fixtures";

export function teamsCopyFixture(
  locale: "en" | "vi" = "vi",
): WorkspaceTeamsCopy {
  return workspaceTeamsCopy(locale);
}

export const formatDayFixture = (iso: string): string => {
  const date = new Date(iso);
  const pad = (value: number) => String(value).padStart(2, "0");
  return `${pad(date.getUTCDate())}/${pad(date.getUTCMonth() + 1)}`;
};

const at = (day: string) => `2026-${day}T02:00:00.000Z`;

export function teamFixture(overrides: Partial<Team> = {}): Team {
  return {
    id: "t-erp",
    name: "Nhóm Kỹ sư ERP",
    description: "Kỹ sư triển khai và dữ liệu ERP",
    status: "Active",
    memberCount: 4,
    version: 3,
    createdAt: at("09-01"),
    ...overrides,
  };
}

export const TEAMS: Team[] = [
  teamFixture(),
  teamFixture({
    id: "t-bgd",
    name: "Ban giám đốc",
    description: "Trịnh Quốc Hưng, Mai Thị Lan, Hoàng Văn Sơn",
    memberCount: 3,
    createdAt: at("08-15"),
  }),
  teamFixture({
    id: "t-kt",
    name: "Nhóm Kế toán tổng hợp",
    description: "Kế toán tổng hợp và công nợ",
    memberCount: 6,
    createdAt: at("09-05"),
  }),
  teamFixture({
    id: "t-qa",
    name: "Nhóm Kiểm thử QA",
    description: "Kiểm thử phần mềm nội bộ",
    memberCount: 5,
    createdAt: at("09-10"),
  }),
  teamFixture({
    id: "t-oncall",
    name: "Nhóm Trực hệ thống",
    description: null,
    status: "Archived",
    memberCount: 3,
    createdAt: at("08-20"),
  }),
];

const person = (
  userId: string,
  name: string,
  role: "Lead" | "Member",
  extra: Partial<TeamExt["people"][number]> = {},
): TeamExt["people"][number] => ({
  userId,
  name,
  email: null,
  title: null,
  role,
  since: at("09-01"),
  ...extra,
});

const creator = { id: "u-ha", name: "Nguyễn Thu Hà" };

export const TEAMS_EXT: TeamsExtData = {
  teams: [
    {
      id: "t-erp",
      createdAt: at("09-01"),
      createdBy: creator,
      archivedAt: null,
      archivedBy: null,
      people: [
        person("u-quan", "Trần Minh Quân", "Lead", {
          email: "quan.tran@minhan.vn",
          title: "Kỹ sư dữ liệu",
        }),
        person("u-anh", "Phan Ngọc Anh", "Member", {
          email: "anh.phan@minhan.vn",
          title: "Kỹ sư triển khai",
        }),
        person("u-bao", "Đặng Quốc Bảo", "Member", {
          email: "bao.dang@minhan.vn",
          title: "Kỹ sư tích hợp",
        }),
        person("u-minh", "Lâm Gia Minh", "Member", {
          email: "minh.lam@minhan.vn",
          title: "Kỹ sư dữ liệu",
          since: at("09-15"),
        }),
      ],
      usage: [
        {
          kind: "space",
          id: "sp-cn",
          name: "Công nghệ",
          spaceType: "Department",
          grant: "Member",
          since: at("09-01"),
          direct: [{ userId: "u-quan", role: "Member" }],
        },
      ],
    },
    {
      id: "t-bgd",
      createdAt: at("08-15"),
      createdBy: creator,
      archivedAt: null,
      archivedBy: null,
      people: [
        person("u-hung", "Trịnh Quốc Hưng", "Lead", { title: "Tổng giám đốc" }),
        person("u-lan", "Mai Thị Lan", "Member", {
          title: "Phó tổng giám đốc",
        }),
        person("u-son", "Hoàng Văn Sơn", "Member", {
          title: "Giám đốc tài chính",
        }),
      ],
      usage: [
        {
          kind: "space",
          id: "sp-cn",
          name: "Công nghệ",
          spaceType: "Department",
          grant: "Viewer",
          since: at("08-15"),
          direct: [],
        },
        {
          kind: "space",
          id: "sp-kt",
          name: "Kế toán",
          spaceType: "Department",
          grant: "Viewer",
          since: at("08-15"),
          direct: [],
        },
      ],
    },
    {
      id: "t-kt",
      createdAt: at("09-05"),
      createdBy: creator,
      archivedAt: null,
      archivedBy: null,
      people: [
        person("u-hanh", "Võ Thị Hạnh", "Lead"),
        ...[
          "Nguyễn Thị Kim",
          "Phạm Văn Long",
          "Trần Thị Ngân",
          "Lê Thu Phương",
          "Vũ Quang Sinh",
        ].map((name, index) => person(`u-kt-${index}`, name, "Member")),
      ],
      usage: [
        {
          kind: "board",
          id: "b-erp",
          name: "Triển khai ERP Q4",
          spaceType: null,
          grant: "viewer",
          since: at("09-05"),
        },
      ],
    },
    {
      id: "t-qa",
      createdAt: at("09-10"),
      createdBy: creator,
      archivedAt: null,
      archivedBy: null,
      people: [
        person("u-nhat", "Tô Minh Nhật", "Lead"),
        ...[
          "Nguyễn Thị Oanh",
          "Trương Minh Tú",
          "Lê Hải Yến",
          "Võ Thành Đạt",
        ].map((name, index) => person(`u-qa-${index}`, name, "Member")),
      ],
      usage: [],
    },
    {
      id: "t-oncall",
      createdAt: at("08-20"),
      createdBy: creator,
      archivedAt: at("09-20"),
      archivedBy: creator,
      people: [
        person("u-thanh", "Vũ Thanh", "Lead"),
        person("u-hang", "Phạm Thu Hằng", "Member"),
        person("u-trang", "Lý Thu Trang", "Member"),
      ],
      usage: [
        {
          kind: "board",
          id: "b-infra",
          name: "Hạ tầng & bảo mật",
          spaceType: null,
          grant: "viewer",
          since: at("08-20"),
        },
      ],
    },
  ],
  admins: [
    {
      id: "u-hung",
      name: "Trịnh Quốc Hưng",
      email: "hung.tq@minhan.vn",
      role: "owner",
    },
    {
      id: "u-ha",
      name: "Nguyễn Thu Hà",
      email: "thuha.nguyen@minhan.vn",
      role: "admin",
    },
  ],
  directory: [
    {
      id: "u-bao",
      name: "Đặng Quốc Bảo",
      email: "bao.dang@minhan.vn",
      title: "Kỹ sư tích hợp",
    },
    {
      id: "u-minh",
      name: "Lâm Gia Minh",
      email: "minh.lam@minhan.vn",
      title: "Kỹ sư dữ liệu",
    },
    {
      id: "u-tai",
      name: "Khúc Văn Tài",
      email: "tai.khuc@minhan.vn",
      title: "Kho vận",
    },
  ],
};

export const FRAME: SetupFrameProps = {
  workspaceId: "ws-minhan",
  workspaceName: "Minh An Group",
  active: "teams",
  counts: { members: 86, teams: 4, departments: 5 },
  links: {
    general: { href: "/w/minhan/settings", onNavigate: () => undefined },
    members: { href: "/w/minhan/members", onNavigate: () => undefined },
    teams: { href: "/w/minhan/teams", onNavigate: () => undefined },
    departments: null,
    sharing: { href: "/w/minhan/security", onNavigate: () => undefined },
    integrations: null,
    billing: { href: "/w/minhan/billing", onNavigate: () => undefined },
  },
};

export const CANDIDATES: TeamCandidate[] = TEAMS_EXT.directory.map(
  (entry) => entry,
);

export function teamRowsFixture(copy = teamsCopyFixture()) {
  return buildTeamRows(copy, TEAMS, TEAMS_EXT.teams, formatDayFixture);
}

// ── Team list ─────────────────────────────────────────────────────

export type TeamsScenario = Omit<
  WorkspaceTeamsSurfaceProps,
  "navigation" | "onCreate" | "onArchive" | "onRestore" | "onRetry"
>;

export function teamsDefaultScenario(
  locale: "en" | "vi" = "vi",
): TeamsScenario {
  const copy = teamsCopyFixture(locale);
  return {
    copy,
    frame: FRAME,
    teams: { status: "ready", data: teamRowsFixture(copy) },
    canManage: true,
    admins: TEAMS_EXT.admins,
    candidates: CANDIDATES,
    creating: false,
    createError: null,
    pendingIds: new Set(),
    lossFor: (teamId) => {
      const info = TEAMS_EXT.teams.find((team) => team.id === teamId);
      return info ? archiveLoss(copy, info) : null;
    },
    formatDay: formatDayFixture,
  };
}

export function teamsLoadingScenario(): TeamsScenario {
  return { ...teamsDefaultScenario(), teams: { status: "loading" } };
}

export function teamsErrorScenario(): TeamsScenario {
  return { ...teamsDefaultScenario(), teams: { status: "error" } };
}

export function teamsEmptyScenario(): TeamsScenario {
  return { ...teamsDefaultScenario(), teams: { status: "ready", data: [] } };
}

/** A member who cannot manage Teams: the page that says whom to ask. */
export function teamsReadOnlyScenario(): TeamsScenario {
  return { ...teamsDefaultScenario(), canManage: false };
}

/** A search that matches the archived Team only. */
export function teamsArchivedScenario(): TeamsScenario {
  return { ...teamsDefaultScenario(), initialQuery: "trực hệ thống" };
}

/** `luu-tru`: the row menu of Nhóm Kỹ sư ERP open and its archive confirmation. */
export function teamsArchiveScenario(): TeamsScenario {
  return { ...teamsDefaultScenario(), initialArchiveTeamId: "t-erp" };
}

export function teamsMenuScenario(): TeamsScenario {
  return { ...teamsDefaultScenario(), initialMenuTeamId: "t-erp" };
}

/** Long names and descriptions, a Team of a thousand people. */
export function teamsEdgeDataScenario(): TeamsScenario {
  const copy = teamsCopyFixture("vi");
  const long = teamFixture({
    id: "t-edge",
    name: "Nhóm Vận hành Khu vực Đông Nam Á — Phòng Kế toán và Kiểm soát nội bộ",
    description:
      "Điều phối vận hành, đối soát công nợ và kiểm soát nội bộ cho toàn bộ các chi nhánh trong khu vực Đông Nam Á.",
    memberCount: 1280,
  });
  return {
    ...teamsDefaultScenario("vi"),
    teams: {
      status: "ready",
      data: buildTeamRows(
        copy,
        [long, TEAMS[3]!],
        TEAMS_EXT.teams,
        formatDayFixture,
      ),
    },
  };
}

export function teamsHighDensityScenario(): TeamsScenario {
  const copy = teamsCopyFixture("vi");
  const many = Array.from({ length: 60 }, (_, index) =>
    teamFixture({
      id: `t-bulk-${index}`,
      name: `Nhóm ${index + 1}`,
      description: index % 3 === 0 ? null : `Nhóm làm việc ${index + 1}`,
      memberCount: (index % 17) + 1,
    }),
  );
  return {
    ...teamsDefaultScenario("vi"),
    teams: {
      status: "ready",
      data: buildTeamRows(copy, many, TEAMS_EXT.teams, formatDayFixture),
    },
  };
}

// ── Create dialog ─────────────────────────────────────────────────

export type CreateScenario = Omit<
  CreateTeamDialogProps,
  "onOpenChange" | "onSubmit"
>;

export function createTeamDefaultScenario(
  locale: "en" | "vi" = "vi",
): CreateScenario {
  return {
    copy: teamsCopyFixture(locale),
    workspaceName: "Minh An Group",
    open: true,
    pending: false,
    error: null,
    candidates: CANDIDATES,
  };
}

export function createTeamPendingScenario(): CreateScenario {
  return { ...createTeamDefaultScenario(), pending: true };
}

export function createTeamErrorScenario(): CreateScenario {
  return {
    ...createTeamDefaultScenario(),
    error: "Đã có một nhóm tên này trong workspace.",
  };
}

export function createTeamEdgeDataScenario(): CreateScenario {
  return {
    ...createTeamDefaultScenario("vi"),
    error:
      "Máy chủ gặp sự cố khi tạo nhóm “Nhóm Vận hành Khu vực Đông Nam Á — Phòng Kế toán và Kiểm soát nội bộ”. Chưa có gì thay đổi; hãy thử lại.",
  };
}

// ── Team detail ───────────────────────────────────────────────────

export type TeamScenario = Omit<
  WorkspaceTeamSurfaceProps,
  | "onRename"
  | "onDescribe"
  | "onArchive"
  | "onUnarchive"
  | "onAddMember"
  | "onChangeRole"
  | "onRemove"
  | "onRetry"
  | "backNavigation"
  | "usageNavigation"
>;

export function teamMembersFixture(): TeamMember[] {
  return (TEAMS_EXT.teams[0]?.people ?? []).map((entry) => ({
    userId: entry.userId,
    displayName: entry.name,
    email: entry.email,
    role: entry.role,
  }));
}

/** `chi-tiet`: the owner's view of Nhóm Kỹ sư ERP, whose only lead is Quân. */
export function teamDefaultScenario(locale: "en" | "vi" = "vi"): TeamScenario {
  const copy = teamsCopyFixture(locale);
  return {
    copy,
    frame: FRAME,
    team: { status: "ready", data: TEAMS[0]! },
    members: { status: "ready", data: teamMembersFixture() },
    ext: TEAMS_EXT.teams[0]!,
    viewer: { userId: "u-ha", role: "admin" },
    candidates: CANDIDATES,
    pendingUserIds: new Set(),
    saving: false,
    lossFor: archiveLoss(copy, TEAMS_EXT.teams[0]!),
    formatDay: formatDayFixture,
  };
}

/** The role menu of the only lead, open (it cannot be lowered). */
export function teamRoleMenuScenario(): TeamScenario {
  return { ...teamDefaultScenario(), initialRoleMenuUserId: "u-quan" };
}

export function teamLoadingScenario(): TeamScenario {
  return {
    ...teamDefaultScenario(),
    team: { status: "loading" },
    members: { status: "loading" },
  };
}

export function teamErrorScenario(): TeamScenario {
  return {
    ...teamDefaultScenario(),
    team: { status: "error" },
    members: { status: "error" },
  };
}

export function teamEmptyScenario(): TeamScenario {
  return {
    ...teamDefaultScenario(),
    team: {
      status: "ready",
      data: teamFixture({ description: null, memberCount: 0 }),
    },
    members: { status: "ready", data: [] },
    ext: { ...TEAMS_EXT.teams[0]!, people: [], usage: [] },
  };
}

/** A member who is not an admin sees but cannot change the Team. */
export function teamReadOnlyScenario(): TeamScenario {
  return {
    ...teamDefaultScenario(),
    viewer: { userId: "u-anh", role: "member" },
  };
}

/** An archived Team: people frozen, the paused access shown, Restore offered. */
export function teamArchivedScenario(): TeamScenario {
  const copy = teamsCopyFixture("vi");
  return {
    ...teamDefaultScenario(),
    team: { status: "ready", data: TEAMS[4]! },
    members: {
      status: "ready",
      data: (TEAMS_EXT.teams[4]?.people ?? []).map((entry) => ({
        userId: entry.userId,
        displayName: entry.name,
        email: entry.email,
        role: entry.role,
      })),
    },
    ext: TEAMS_EXT.teams[4]!,
    lossFor: archiveLoss(copy, TEAMS_EXT.teams[4]!),
  };
}

export function teamEdgeDataScenario(): TeamScenario {
  return {
    ...teamDefaultScenario("vi"),
    team: {
      status: "ready",
      data: teamFixture({
        name: "Nhóm Vận hành Khu vực Đông Nam Á — Phòng Kế toán và Kiểm soát nội bộ",
        description:
          "Điều phối vận hành, đối soát công nợ và kiểm soát nội bộ cho toàn bộ các chi nhánh trong khu vực Đông Nam Á.",
      }),
    },
  };
}

export function teamHighDensityScenario(): TeamScenario {
  const people = Array.from({ length: 40 }, (_, index) => ({
    userId: `u-bulk-${index}`,
    displayName: `Người ${index + 1}`,
    email: `nguoi${index + 1}@minhan.vn`,
    role: index === 0 ? ("Lead" as const) : ("Member" as const),
  }));
  return {
    ...teamDefaultScenario(),
    team: { status: "ready", data: teamFixture({ memberCount: 40 }) },
    members: { status: "ready", data: people },
    ext: { ...TEAMS_EXT.teams[0]!, people: [] },
  };
}

// ── Sharing and security ──────────────────────────────────────────

export type SecurityScenario = Omit<
  WorkspaceSecuritySurfaceProps,
  "onToggleLinks" | "onRetry"
>;

export function securityDefaultScenario(): SecurityScenario {
  return {
    copy: teamsCopyFixture("vi"),
    frame: { ...FRAME, active: "sharing" },
    view: {
      status: "ready",
      data: {
        linksOn: false,
        offSince: "15/09",
        guestCount: 2,
        guestExpiryDays: 90,
        companyDomain: "minhan.vn",
      },
    },
    canManage: true,
    saving: false,
    error: null,
  };
}

export function securityLinksOnScenario(): SecurityScenario {
  return {
    ...securityDefaultScenario(),
    view: {
      status: "ready",
      data: {
        linksOn: true,
        offSince: null,
        guestCount: 1,
        guestExpiryDays: null,
        companyDomain: "minhan.vn",
      },
    },
  };
}

export function securityLoadingScenario(): SecurityScenario {
  return { ...securityDefaultScenario(), view: { status: "loading" } };
}

export function securityErrorScenario(): SecurityScenario {
  return { ...securityDefaultScenario(), view: { status: "error" } };
}

export function securityReadOnlyScenario(): SecurityScenario {
  return { ...securityDefaultScenario(), canManage: false };
}

export function teamsNavigationFixture() {
  return shellNavigationFixture();
}

export function teamBackNavigationFixture(
  onNavigate: () => void = () => undefined,
): WorkspaceTeamSurfaceProps["backNavigation"] {
  return { getHref: () => "/w/minhan/teams", onNavigate };
}

export function teamUsageNavigationFixture(): WorkspaceTeamSurfaceProps["usageNavigation"] {
  return {
    getHref: () => "/w/minhan/spaces/sp-cn",
    onNavigate: () => undefined,
  };
}
