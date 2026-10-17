/**
 * The people of the Công nghệ department of the 2026-10 Space and Members
 * mockups: 24 in all, 2 admins, 19 members (16 added one by one, among them
 * Trần Minh Quân who is also in the team Nhóm Kỹ sư ERP, plus the 3 others of
 * that team) and 3 viewers (the team Ban giám đốc). The ERP board's people and
 * the base personas are among them; the rest are listed here, with the email,
 * job line and last activity the Members page shows (gap
 * `CTR-GAP-WS-MEMBERS-EXT`).
 *
 * Pure data: `MockStore.seedSpaces` inserts it.
 */
import { ERP_PEOPLE } from "./mock-erp-board";
import { mockIds } from "./mock-ids";

export interface SpacePerson {
  readonly id: string;
  readonly name: string;
  readonly email: string;
}

const person = (slug: string, name: string): SpacePerson => ({
  id: `mock-user-eng-${slug}`,
  name,
  email: `${slug}@minhan.vn`,
});

/** Added one by one as members, beside the ERP people and the member persona. */
export const ENGINEERING_MEMBERS: readonly SpacePerson[] = [
  person("hung", "Nguyễn Văn Hùng"),
  person("mai", "Trần Thị Mai"),
  person("huy", "Lê Quang Huy"),
  person("duc-anh", "Phạm Đức Anh"),
  person("lan", "Hoàng Thị Lan"),
  person("khoa", "Vũ Minh Khoa"),
  person("huong", "Đặng Thu Hương"),
  person("nam", "Bùi Văn Nam"),
  person("hai", "Ngô Thanh Hải"),
  person("han", "Lý Gia Hân"),
];

/** In the team Nhóm Kỹ sư ERP only, beside Trần Minh Quân and Phan Ngọc Anh. */
export const ERP_TEAM_ONLY: readonly SpacePerson[] = [
  person("quoc-bao", "Đặng Quốc Bảo"),
  person("gia-minh", "Lâm Gia Minh"),
];

/** The directors: the viewers of the department, through the team Ban giám đốc. */
export const EXECUTIVES: readonly SpacePerson[] = [
  person("quoc-hung", "Trịnh Quốc Hưng"),
  person("thi-lan", "Mai Thị Lan"),
  person("van-son", "Hoàng Văn Sơn"),
];

/** The team Nhóm Kiểm thử QA (not in the department; the add dialog offers it). */
export const QA_PEOPLE: readonly SpacePerson[] = [
  person("qa-nhat", "Tô Minh Nhật"),
  person("qa-oanh", "Nguyễn Thị Oanh"),
  person("qa-tu", "Trương Minh Tú"),
  person("qa-yen", "Lê Hải Yến"),
  person("qa-dat", "Võ Thành Đạt"),
];

/** The team Nhóm Kế toán tổng hợp (6): given the ERP Board by name, outside Công nghệ. */
export const ACCOUNTING_PEOPLE: readonly SpacePerson[] = [
  person("kt-hanh", "Võ Thị Hạnh"),
  person("kt-long", "Phạm Văn Long"),
  person("kt-ngan", "Trần Thị Ngân"),
  person("kt-phuong", "Lê Thu Phương"),
  person("kt-sinh", "Vũ Quang Sinh"),
  person("kt-yen", "Hoàng Hải Yến"),
];

export const ACCOUNTING_TEAM = {
  description: "Kế toán tổng hợp và công nợ",
  archived: false,
  createdAt: "2026-09-05T02:00:00.000Z",
  id: "mock-team-erp-accounting",
  name: "Nhóm Kế toán tổng hợp",
  members: ACCOUNTING_PEOPLE.map((entry, index) => ({
    userId: entry.id,
    role: index === 0 ? ("Lead" as const) : ("Member" as const),
  })),
};

/** The archived team Nhóm Trực hệ thống (3 people, leader Vũ Thanh), paused on Hạ tầng & bảo mật. */
export const ONCALL_TEAM = {
  id: "mock-team-oncall",
  name: "Nhóm Trực hệ thống",
  description: null as string | null,
  archived: true,
  createdAt: "2026-08-20T02:00:00.000Z",
  members: [
    { userId: ERP_PEOPLE.VT.id, role: "Lead" as const },
    { userId: ERP_PEOPLE.PH.id, role: "Member" as const },
    { userId: ERP_PEOPLE.LT.id, role: "Member" as const },
  ],
};

export const ERP_ENGINEERS_TEAM = {
  description: "Kỹ sư triển khai và dữ liệu ERP",
  archived: false,
  createdAt: "2026-09-01T02:00:00.000Z",
  id: "mock-team-erp-engineers",
  name: "Nhóm Kỹ sư ERP",
  members: [
    { userId: ERP_PEOPLE.MQ.id, role: "Lead" as const },
    { userId: ERP_PEOPLE.NA.id, role: "Member" as const },
    { userId: ERP_TEAM_ONLY[0]!.id, role: "Member" as const },
    { userId: ERP_TEAM_ONLY[1]!.id, role: "Member" as const },
  ],
};

export const EXECUTIVE_TEAM = {
  description: "Trịnh Quốc Hưng, Mai Thị Lan, Hoàng Văn Sơn",
  archived: false,
  createdAt: "2026-08-15T02:00:00.000Z",
  id: "mock-team-executive",
  name: "Ban giám đốc",
  members: [
    { userId: EXECUTIVES[0]!.id, role: "Lead" as const },
    { userId: EXECUTIVES[1]!.id, role: "Member" as const },
    { userId: EXECUTIVES[2]!.id, role: "Member" as const },
  ],
};

export const QA_TEAM = {
  description: "Kiểm thử phần mềm nội bộ",
  archived: false,
  createdAt: "2026-09-10T02:00:00.000Z",
  id: "mock-team-qa",
  name: "Nhóm Kiểm thử QA",
  members: QA_PEOPLE.map((entry, index) => ({
    userId: entry.id,
    role: index === 0 ? ("Lead" as const) : ("Member" as const),
  })),
};

/** What the Members page says about a person beyond the user record. */
export interface PersonProfile {
  readonly email: string;
  readonly title: string;
  /** UTC instant; absent = long ago. */
  readonly lastActiveAt: string;
  readonly activeNow?: boolean;
}

const ago = (iso: string) => iso;

export const PERSON_PROFILES: Readonly<Record<string, PersonProfile>> = {
  [ERP_PEOPLE.HP.id]: {
    email: "phuc.le@minhan.vn",
    title: "Trưởng phòng",
    lastActiveAt: ago("2026-10-12T03:30:00.000Z"),
  },
  [ERP_PEOPLE.HA.id]: {
    email: "thuha.nguyen@minhan.vn",
    title: "Quản trị workspace",
    lastActiveAt: ago("2026-10-12T03:40:00.000Z"),
    activeNow: true,
  },
  [ERP_PEOPLE.MQ.id]: {
    email: "quan.tran@minhan.vn",
    title: "Kỹ sư dữ liệu",
    lastActiveAt: ago("2026-10-12T01:40:00.000Z"),
  },
  [ERP_PEOPLE.VT.id]: {
    email: "thanh.vu@minhan.vn",
    title: "Quản trị hệ thống",
    lastActiveAt: ago("2026-10-11T08:00:00.000Z"),
  },
  [ERP_PEOPLE.LT.id]: {
    email: "trang.ly@minhan.vn",
    title: "Hỗ trợ kỹ thuật",
    lastActiveAt: ago("2026-10-12T03:30:00.000Z"),
  },
  [ERP_PEOPLE.TT.id]: {
    email: "tam.bui@minhan.vn",
    title: "Kỹ sư kiểm thử",
    lastActiveAt: ago("2026-10-10T08:00:00.000Z"),
  },
  [ERP_PEOPLE.PH.id]: {
    email: "hang.pham@minhan.vn",
    title: "Kỹ sư mạng",
    lastActiveAt: ago("2026-10-09T08:00:00.000Z"),
  },
  [mockIds.users.member]: {
    email: "member@minhan.vn",
    title: "Kỹ sư phần mềm",
    lastActiveAt: ago("2026-10-08T08:00:00.000Z"),
  },
  [EXECUTIVES[0]!.id]: {
    email: "hung.trinh@minhan.vn",
    title: "Tổng giám đốc",
    lastActiveAt: ago("2026-10-11T04:00:00.000Z"),
  },
  [EXECUTIVES[1]!.id]: {
    email: "lan.mai@minhan.vn",
    title: "Phó tổng giám đốc",
    lastActiveAt: ago("2026-10-10T04:00:00.000Z"),
  },
  [EXECUTIVES[2]!.id]: {
    email: "son.hoang@minhan.vn",
    title: "Giám đốc tài chính",
    lastActiveAt: ago("2026-10-09T04:00:00.000Z"),
  },
};

const TITLES = [
  "Kỹ sư phần mềm",
  "Kỹ sư kiểm thử",
  "Phân tích nghiệp vụ",
  "Kỹ sư DevOps",
  "Chuyên viên bảo mật",
];

export function profileOf(
  userId: string,
  fallbackEmail: string,
): PersonProfile {
  const listed = ENGINEERING_MEMBERS.findIndex((entry) => entry.id === userId);
  return (
    PERSON_PROFILES[userId] ??
    (listed >= 0
      ? {
          email: fallbackEmail,
          title: TITLES[listed % TITLES.length]!,
          lastActiveAt: "2026-10-05T04:00:00.000Z",
        }
      : { email: fallbackEmail, title: "", lastActiveAt: "" })
  );
}

/** A department's waiting workspace invitations (the sender's wording is the page's). */
export const PENDING_INVITES: Readonly<
  Record<
    string,
    readonly {
      id: string;
      email: string;
      name: string | null;
      sentAt: string;
      role: "Member";
    }[]
  >
> = {
  [mockIds.spaces.engineering]: [
    {
      id: "mock-invite-khoa",
      email: "khoa.ngo@minhan.vn",
      name: "Ngô Đăng Khoa",
      sentAt: "2026-10-10T03:00:00.000Z",
      role: "Member",
    },
  ],
};
