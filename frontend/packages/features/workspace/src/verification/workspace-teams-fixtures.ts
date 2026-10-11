/**
 * Deterministic fixtures for the Teams surfaces (WX-TEAM-001): the Team
 * list, the create dialog and the Team detail. No clocks, no randomness,
 * no network.
 */
import type { Team, TeamMember } from "../core/model/teams";
import type { WorkspaceMember } from "../core/types/workspace";
import type { CreateTeamDialogProps } from "../web/teams/create-team-dialog";
import type { WorkspaceTeamSurfaceProps } from "../web/teams/workspace-team-surface";
import type { WorkspaceTeamsSurfaceProps } from "../web/teams/workspace-teams-surface";
import {
  workspaceTeamsCopy,
  type WorkspaceTeamsCopy,
} from "../web/teams/workspace-teams.copy";
import { HOME_FIXTURE_NOW } from "./workspace-home-fixtures";
import { memberFixture, membersFixture } from "./workspace-members-fixtures";

const DAY = 24 * 60 * 60 * 1000;
const at = (offset: number) =>
  new Date(HOME_FIXTURE_NOW + offset).toISOString();

export function teamsCopyFixture(
  locale: "en" | "vi" = "en",
): WorkspaceTeamsCopy {
  return workspaceTeamsCopy(locale);
}

export function teamFixture(overrides: Partial<Team> = {}): Team {
  return {
    id: "team-platform",
    name: "Platform engineering",
    description: "Owns the shared runtime, CI and developer tooling.",
    status: "Active",
    memberCount: 3,
    version: 3,
    createdAt: at(-90 * DAY),
    ...overrides,
  };
}

export function teamMemberFixture(
  overrides: Partial<TeamMember> &
    Pick<TeamMember, "userId" | "displayName"> = {
    userId: "u-ada",
    displayName: "Ada Lovelace",
  },
): TeamMember {
  return {
    email: null,
    role: "Member",
    ...overrides,
  };
}

/** The Team's members: Ada is the only Lead. */
export function teamMembersFixture(): TeamMember[] {
  return [
    teamMemberFixture({
      userId: "u-ada",
      displayName: "Ada Lovelace",
      role: "Lead",
    }),
    teamMemberFixture({
      userId: "u-alan",
      displayName: "Alan Turing",
      email: "alan.turing@example.com",
    }),
    teamMemberFixture({ userId: "u-grace", displayName: "Grace Hopper" }),
  ];
}

/**
 * Workspace members the Team could take: the workspace fixture (one
 * suspended guest, never addable) plus an active guest, who may join.
 */
export function teamCandidatesFixture(): WorkspaceMember[] {
  return [
    ...membersFixture(),
    memberFixture({ userId: "u-priya", name: "Priya Raman", role: "guest" }),
  ];
}

export function teamsNavigationFixture(
  onNavigate: (teamId: string) => void = () => undefined,
): WorkspaceTeamsSurfaceProps["navigation"] {
  return {
    getHref: (teamId) => `/w/product/teams/${teamId}`,
    onNavigate,
  };
}

export function teamBackNavigationFixture(
  onNavigate: () => void = () => undefined,
): WorkspaceTeamSurfaceProps["backNavigation"] {
  return { getHref: () => "/w/product/teams", onNavigate };
}

// ── Team list ─────────────────────────────────────────────────────

type TeamsScenario = Omit<
  WorkspaceTeamsSurfaceProps,
  "navigation" | "onFilterChange" | "onCreate" | "onRetry"
>;

export function teamsListFixture(): Team[] {
  return [
    teamFixture(),
    teamFixture({
      id: "team-design",
      name: "Design systems",
      description: "Tokens, primitives and the component library.",
      memberCount: 1,
    }),
    teamFixture({
      id: "team-support",
      name: "Customer support",
      description: null,
      memberCount: 12,
    }),
    teamFixture({
      id: "team-legacy",
      name: "Legacy billing",
      description: "Wound down in the last quarter.",
      status: "Archived",
      memberCount: 2,
    }),
    teamFixture({
      id: "team-pilot",
      name: "Pilot programme",
      description: null,
      status: "Archived",
      memberCount: 4,
    }),
  ];
}

export function teamsDefaultScenario(
  locale: "en" | "vi" = "en",
): TeamsScenario {
  return {
    copy: teamsCopyFixture(locale),
    teams: { status: "ready", data: teamsListFixture() },
    filter: "active",
    canManage: true,
    readOnlyReason: null,
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

/** A member who is not an admin: the list is visible, nothing is offered. */
export function teamsReadOnlyScenario(): TeamsScenario {
  const copy = teamsCopyFixture();
  return {
    ...teamsDefaultScenario(),
    canManage: false,
    readOnlyReason: copy.readOnly,
  };
}

export function teamsArchivedScenario(): TeamsScenario {
  return { ...teamsDefaultScenario(), filter: "archived" };
}

/** Vietnamese copy, long names and descriptions, large member counts. */
export function teamsEdgeDataScenario(): TeamsScenario {
  return {
    ...teamsDefaultScenario("vi"),
    teams: {
      status: "ready",
      data: [
        teamFixture({
          id: "team-edge-1",
          name: "Nhóm Vận hành Khu vực Đông Nam Á — Phòng Kế toán và Kiểm soát nội bộ",
          description:
            "Điều phối vận hành, đối soát công nợ và kiểm soát nội bộ cho toàn bộ các chi nhánh trong khu vực Đông Nam Á.",
          memberCount: 1280,
        }),
        teamFixture({
          id: "team-edge-2",
          name: "Nguyễn Thùy Linh & cộng sự",
          description: null,
          memberCount: 1,
        }),
        teamFixture({
          id: "team-edge-3",
          name: "Supercalifragilisticexpialidocious-platform-reliability-guild",
          memberCount: 0,
        }),
      ],
    },
  };
}

export function teamsHighDensityScenario(): TeamsScenario {
  return {
    ...teamsDefaultScenario(),
    teams: {
      status: "ready",
      data: Array.from({ length: 120 }, (_, index) =>
        teamFixture({
          id: `team-bulk-${index}`,
          name: `Team ${index + 1}`,
          description: index % 3 === 0 ? null : `Working group ${index + 1}`,
          memberCount: (index % 17) + 1,
        }),
      ),
    },
  };
}

// ── Create dialog ─────────────────────────────────────────────────

type CreateScenario = Omit<CreateTeamDialogProps, "onOpenChange" | "onSubmit">;

export function createTeamDefaultScenario(
  locale: "en" | "vi" = "en",
): CreateScenario {
  return {
    copy: teamsCopyFixture(locale),
    open: true,
    pending: false,
    error: null,
  };
}

export function createTeamPendingScenario(): CreateScenario {
  return { ...createTeamDefaultScenario(), pending: true };
}

export function createTeamErrorScenario(): CreateScenario {
  return {
    ...createTeamDefaultScenario(),
    error: "A Team with this name already exists in the workspace.",
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

type TeamScenario = Omit<
  WorkspaceTeamSurfaceProps,
  | "onRename"
  | "onDescribe"
  | "onArchive"
  | "onUnarchive"
  | "onAddMember"
  | "onChangeRole"
  | "onRemove"
  | "onRetry"
>;

/** The owner's view of an active Team whose only Lead is Ada. */
export function teamDefaultScenario(locale: "en" | "vi" = "en"): TeamScenario {
  return {
    copy: teamsCopyFixture(locale),
    team: { status: "ready", data: teamFixture() },
    members: { status: "ready", data: teamMembersFixture() },
    viewer: { userId: "u-ada", role: "owner" },
    candidates: teamCandidatesFixture(),
    pendingUserIds: new Set(),
    saving: false,
    backNavigation: teamBackNavigationFixture(),
  };
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

/** The Team loaded but its members did not. */
export function teamMembersErrorScenario(): TeamScenario {
  return { ...teamDefaultScenario(), members: { status: "error" } };
}

export function teamEmptyScenario(): TeamScenario {
  return {
    ...teamDefaultScenario(),
    team: {
      status: "ready",
      data: teamFixture({ description: null, memberCount: 0 }),
    },
    members: { status: "ready", data: [] },
  };
}

/** A workspace member who is not an admin sees but cannot change the Team. */
export function teamReadOnlyScenario(): TeamScenario {
  return {
    ...teamDefaultScenario(),
    viewer: { userId: "u-grace", role: "member" },
  };
}

export function teamArchivedScenario(): TeamScenario {
  return {
    ...teamDefaultScenario(),
    team: { status: "ready", data: teamFixture({ status: "Archived" }) },
  };
}

/** A role change and a removal in flight for two members. */
export function teamPendingScenario(): TeamScenario {
  return {
    ...teamDefaultScenario(),
    pendingUserIds: new Set(["u-alan"]),
  };
}

/** Vietnamese copy, 40-plus character names and a long e-mail address. */
export function teamEdgeDataScenario(): TeamScenario {
  return {
    ...teamDefaultScenario("vi"),
    team: {
      status: "ready",
      data: teamFixture({
        name: "Nhóm Vận hành Khu vực Đông Nam Á — Phòng Kế toán và Kiểm soát nội bộ",
        description:
          "Điều phối vận hành, đối soát công nợ và kiểm soát nội bộ cho toàn bộ các chi nhánh trong khu vực Đông Nam Á, gồm cả quy trình khóa sổ cuối tháng.",
        memberCount: 4,
      }),
    },
    members: {
      status: "ready",
      data: [
        teamMemberFixture({
          userId: "u-ada",
          displayName: "Đồng sở hữu: Trần Nguyễn Hoàng Bảo Ngọc Anh",
          email:
            "tran.nguyen.hoang.bao.ngoc.anh.phong.van.hanh.khu.vuc@example-company-international.com",
          role: "Lead",
        }),
        teamMemberFixture({
          userId: "u-linh",
          displayName: "Nguyễn Thùy Linh",
          email: "linh.nguyen@example.com",
        }),
        teamMemberFixture({
          userId: "u-long",
          displayName:
            "Supercalifragilisticexpialidocious-Longfellow-Wolfeschlegelstein",
        }),
        teamMemberFixture({ userId: "u-grace", displayName: "Grace Hopper" }),
      ],
    },
  };
}

/** 200 members: a long table with many Leads and many role selects. */
export function teamHighDensityScenario(): TeamScenario {
  const members = Array.from({ length: 200 }, (_, index) =>
    teamMemberFixture({
      userId: `u-bulk-${index}`,
      displayName: `Thành viên số ${index + 1}`,
      role: index % 25 === 0 ? "Lead" : "Member",
    }),
  );
  return {
    ...teamDefaultScenario(),
    team: {
      status: "ready",
      data: teamFixture({ memberCount: members.length }),
    },
    members: { status: "ready", data: members },
  };
}
