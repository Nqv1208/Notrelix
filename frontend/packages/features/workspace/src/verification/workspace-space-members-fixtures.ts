/**
 * Deterministic fixtures of the department members tab: the data of
 * Final-Members at its clock (12 Oct 2026, 10:40) for every state it
 * declares. No clocks, no randomness, no network.
 */
import type { IdentityTone } from "../core/model/navigation";
import type { SpaceMemberEntry, SpaceTeam } from "../core/model/spaces";
import type { SectionState } from "../web/home/home-models";
import type { SpaceAddPreviewBase } from "../web/spaces/space-members-dialogs";
import type {
  SpaceDirectoryPerson,
  SpaceMemberCandidate,
  SpacePendingInviteView,
  TeamPersonView,
  TypedToken,
} from "../web/spaces/space-members-models";

const user = (
  id: string,
  displayName: string,
  role: SpaceMemberEntry["role"],
  addedAt = "2026-09-20T09:00:00+07:00",
): SpaceMemberEntry => ({
  subjectType: "User",
  subjectId: id,
  role,
  displayName,
  avatarUrl: null,
  addedAt,
});

const team = (
  id: string,
  displayName: string,
  role: SpaceMemberEntry["role"],
  addedAt: string,
): SpaceMemberEntry => ({
  subjectType: "Team",
  subjectId: id,
  role,
  displayName,
  avatarUrl: null,
  addedAt,
});

const FILLER = [
  "Bùi Thị Hoa",
  "Cao Minh Đức",
  "Dương Thu Hằng",
  "Đinh Quốc Việt",
  "Hồ Ngọc Lan",
  "Khúc Văn Nam",
  "Lương Thị Mai",
  "Ngô Đăng Khoa",
  "Ông Gia Huy",
  "Phạm Thanh Tùng",
  "Quách Bảo Ngọc",
  "Tạ Hoài Phương",
  "Uông Minh Châu",
];

/** 20 entries: 2 teams and 18 people, so seven rows show and 13 are left. */
export function membersFixture(): SpaceMemberEntry[] {
  return [
    team("t-erp", "Nhóm Kỹ sư ERP", "Member", "2026-09-01T09:00:00+07:00"),
    team("t-bgd", "Ban giám đốc", "Viewer", "2026-08-15T09:00:00+07:00"),
    user("u-phuc", "Lê Hoàng Phúc", "Admin", "2026-10-12T10:30:00+07:00"),
    user("u-ha", "Nguyễn Thu Hà", "Admin", "2026-10-12T10:40:00+07:00"),
    user("u-quan", "Trần Minh Quân", "Member", "2026-10-12T08:40:00+07:00"),
    user("u-thanh", "Vũ Thanh", "Member", "2026-10-11T09:00:00+07:00"),
    user("u-trang", "Lý Thu Trang", "Member", "2026-10-12T10:30:00+07:00"),
    ...FILLER.map((name, index) =>
      user(`u-filler-${index}`, name, index < 11 ? "Member" : "Viewer"),
    ),
  ];
}

/** Hà demoted: Phúc is the only admin (Final-Members `quan-tri-cuoi`). */
export function membersLastAdminFixture(): SpaceMemberEntry[] {
  return membersFixture().map((entry) =>
    entry.subjectId === "u-ha" ? { ...entry, role: "Member" } : entry,
  );
}

export const MEMBERS_TEAMS: SpaceTeam[] = [
  { id: "t-erp", name: "Nhóm Kỹ sư ERP", memberCount: 4 },
  { id: "t-bgd", name: "Ban giám đốc", memberCount: 3 },
  { id: "t-qa", name: "Nhóm Kiểm thử QA", memberCount: 5 },
];

export const MEMBERS_CANDIDATES: SpaceMemberCandidate[] = [
  {
    id: "u-hung",
    name: "Đỗ Văn Hùng",
    avatarUrl: null,
    email: "hung.do@minhan.vn",
  },
  { id: "u-tuan", name: "Đặng Minh Tuấn", avatarUrl: null, email: null },
];

export const MEMBERS_DIRECTORY: ReadonlyMap<string, SpaceDirectoryPerson> =
  new Map([
    [
      "u-phuc",
      {
        email: "phuc.le@minhan.vn",
        workspaceRole: "member",
        title: "Trưởng phòng",
        shortName: "Phúc",
      },
    ],
    [
      "u-ha",
      {
        email: "thuha.nguyen@minhan.vn",
        workspaceRole: "admin",
        title: null,
        sourceNote: "người tạo phòng",
      },
    ],
    [
      "u-quan",
      {
        email: "quan.tran@minhan.vn",
        workspaceRole: "member",
        title: "Kỹ sư dữ liệu",
        shortName: "Quân",
      },
    ],
    [
      "u-thanh",
      {
        email: "thanh.vu@minhan.vn",
        workspaceRole: "member",
        title: "Quản trị hệ thống",
      },
    ],
    [
      "u-trang",
      {
        email: "trang.ly@minhan.vn",
        workspaceRole: "member",
        title: "Hỗ trợ kỹ thuật",
      },
    ],
  ]);

const person = (
  id: string,
  name: string,
  tone: IdentityTone,
  note: string | null = null,
): TeamPersonView => ({ id, name, tone, note });

export const MEMBERS_TEAM_PEOPLE: ReadonlyMap<
  string,
  SectionState<readonly TeamPersonView[]>
> = new Map([
  [
    "t-erp",
    {
      status: "ready",
      data: [
        person("u-quan", "Trần Minh Quân", "blue"),
        person("u-anh", "Phan Ngọc Anh", "coral"),
        person("u-bao", "Đặng Quốc Bảo", "sage"),
        person("u-minh", "Lâm Gia Minh", "gold"),
      ],
    },
  ],
  [
    "t-bgd",
    {
      status: "ready",
      data: [
        person("u-hung2", "Trịnh Quốc Hưng", "ember", "Tổng giám đốc"),
        person("u-lan", "Mai Thị Lan", "coral", "Phó tổng giám đốc"),
        person("u-son", "Hoàng Văn Sơn", "sage", "Giám đốc tài chính"),
      ],
    },
  ],
]);

export const MEMBERS_INVITES: SpacePendingInviteView[] = [
  {
    id: "inv-khoa",
    email: "khoa.ngo@minhan.vn",
    name: "Ngô Đăng Khoa",
    sent: "mời 2 ngày trước",
    role: "Member",
  },
];

export const MEMBERS_ALSO: ReadonlyMap<
  string,
  { team: string; role: "Admin" | "Member" | "Viewer" }
> = new Map([["u-quan", { team: "Nhóm Kỹ sư ERP", role: "Member" }]]);

export const MEMBERS_PREVIEW: SpaceAddPreviewBase = {
  openBoards: 5,
  docs: 3,
  openChildren: ["Nhóm Hạ tầng"],
  hidden: ["Nhóm Ứng dụng", "board Ngân sách CNTT 2027"],
};

export const MEMBERS_INVITE_TOKENS: TypedToken[] = [
  {
    kind: "person",
    candidate: MEMBERS_CANDIDATES[0] as SpaceMemberCandidate,
  },
  { kind: "team", team: MEMBERS_TEAMS[2] as SpaceTeam },
  { kind: "outsider", email: "tuan@doitac-erp.vn" },
];
