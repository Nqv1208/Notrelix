/**
 * Deterministic fixtures for members and invitations (WX-MEM-001).
 */
import type {
  WorkspaceInvitation,
  WorkspaceMember,
} from "../core/types/workspace";
import {
  workspaceMembersCopy,
  type WorkspaceMembersCopy,
} from "../web/members/workspace-members.copy";
import type { WorkspaceMembersSurfaceProps } from "../web/members/workspace-members-surface";
import {
  HOME_FIXTURE_NOW,
  homeFormattersFixture,
} from "./workspace-home-fixtures";

const DAY = 24 * 60 * 60 * 1000;
const at = (offset: number) =>
  new Date(HOME_FIXTURE_NOW + offset).toISOString();

export function membersCopyFixture(
  locale: "en" | "vi" = "en",
): WorkspaceMembersCopy {
  return workspaceMembersCopy(locale);
}

export function memberFixture(
  overrides: Partial<WorkspaceMember> &
    Pick<WorkspaceMember, "userId" | "name" | "role">,
): WorkspaceMember {
  return {
    id: overrides.userId,
    joinedAt: at(-120 * DAY),
    ...overrides,
  };
}

export function membersFixture(): WorkspaceMember[] {
  return [
    memberFixture({ userId: "u-ada", name: "Ada Lovelace", role: "owner" }),
    memberFixture({
      userId: "u-alan",
      name: "Alan Turing",
      role: "admin",
      joinedAt: at(-60 * DAY),
    }),
    memberFixture({
      userId: "u-grace",
      name: "Grace Hopper",
      role: "member",
      joinedAt: at(-20 * DAY),
    }),
    memberFixture({
      userId: "u-linh",
      name: "Nguyễn Thùy Linh",
      role: "member",
      joinedAt: at(-3 * DAY),
    }),
    memberFixture({
      userId: "u-sam",
      name: "Sam Okafor",
      role: "guest",
      joinedAt: at(-DAY),
      membership: "suspended",
    }),
  ];
}

export function invitationFixture(
  overrides: Partial<WorkspaceInvitation> &
    Pick<WorkspaceInvitation, "id" | "email">,
): WorkspaceInvitation {
  return {
    role: "member",
    createdAt: at(-2 * DAY),
    expiresAt: at(5 * DAY),
    isAccepted: false,
    ...overrides,
  };
}

export function invitationsFixture(): WorkspaceInvitation[] {
  return [
    invitationFixture({ id: "inv-1", email: "minh.tran@example.com" }),
    invitationFixture({
      id: "inv-2",
      email: "priya.raman@example.com",
      role: "admin",
      createdAt: at(-DAY),
      expiresAt: at(6 * DAY),
    }),
    invitationFixture({
      id: "inv-3",
      email: "old.contact@example.com",
      createdAt: at(-10 * DAY),
      expiresAt: at(-3 * DAY),
    }),
    invitationFixture({
      id: "inv-4",
      email: "joined@example.com",
      isAccepted: true,
    }),
  ];
}

type MembersScenario = Omit<
  WorkspaceMembersSurfaceProps,
  | "onTabChange"
  | "onChangeRole"
  | "onRemove"
  | "onSuspend"
  | "onActivate"
  | "onInvite"
  | "onRevoke"
  | "onRetryMembers"
  | "onRetryInvitations"
>;

/** The owner's view (Ada is the only owner). */
export function membersDefaultScenario(
  locale: "en" | "vi" = "en",
): MembersScenario {
  return {
    copy: membersCopyFixture(locale),
    formatters: homeFormattersFixture(locale),
    now: HOME_FIXTURE_NOW,
    workspaceName: "Product",
    viewer: { userId: "u-ada", role: "owner" },
    members: { status: "ready", data: membersFixture() },
    invitations: { status: "ready", data: invitationsFixture() },
    tab: "members",
    pendingMemberIds: new Set(),
    pendingInvitationIds: new Set(),
    inviting: false,
    inviteError: null,
    inviteResetKey: 0,
  };
}

/** An admin's view: the sole owner cannot be changed by anyone. */
export function membersAdminScenario(): MembersScenario {
  return {
    ...membersDefaultScenario(),
    viewer: { userId: "u-alan", role: "admin" },
  };
}

/** A member's view: read-only, no invitations tab. */
export function membersReadOnlyScenario(): MembersScenario {
  return {
    ...membersDefaultScenario(),
    viewer: { userId: "u-grace", role: "member" },
    invitations: null,
  };
}

export function membersLoadingScenario(): MembersScenario {
  return {
    ...membersDefaultScenario(),
    workspaceName: null,
    members: { status: "loading" },
    invitations: { status: "loading" },
  };
}

export function membersErrorScenario(): MembersScenario {
  return {
    ...membersDefaultScenario(),
    members: { status: "error" },
    invitations: { status: "error" },
  };
}

export function membersInvitationsScenario(): MembersScenario {
  return {
    ...membersDefaultScenario(),
    tab: "invitations",
    pendingInvitationIds: new Set(["inv-2"]),
  };
}

export function membersInvitationsEmptyScenario(): MembersScenario {
  return {
    ...membersDefaultScenario(),
    tab: "invitations",
    invitations: { status: "ready", data: [] },
    inviteError: "Đã có một lời mời đang chờ xử lý dành cho email này.",
  };
}

/** Long and Unicode names, a pending role change, many members. */
export function membersEdgeDataScenario(): MembersScenario {
  const many = Array.from({ length: 24 }, (_, index) =>
    memberFixture({
      userId: `u-bulk-${index}`,
      name: `Thành viên số ${index + 1} — Phòng Vận hành Khu vực Đông Nam Á`,
      role: index % 5 === 0 ? "admin" : index % 7 === 0 ? "guest" : "member",
      joinedAt: at(-index * DAY),
    }),
  );
  return {
    ...membersDefaultScenario("vi"),
    members: {
      status: "ready",
      data: [
        ...membersFixture(),
        memberFixture({
          userId: "u-owner-2",
          name: "Đồng sở hữu: Trần Nguyễn Hoàng Bảo Ngọc Anh",
          role: "owner",
        }),
        ...many,
      ],
    },
    pendingMemberIds: new Set(["u-grace"]),
  };
}
