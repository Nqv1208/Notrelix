/**
 * The people of the Công nghệ department of the 2026-10 Space mockup: 24 in
 * all, 2 admins, 19 members (15 added one by one and the 4 of the team Kỹ sư
 * ERP) and 3 viewers (the team Ban giám đốc). The ERP board's people and the
 * base personas are among them; the rest are listed here.
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

/** Added one by one as members, beside Lý Thu Trang, Phạm Thu Hằng and the member persona. */
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
  person("bao", "Dương Quốc Bảo"),
  person("han", "Lý Gia Hân"),
  person("thanh", "Đinh Công Thành"),
];

/** Board directors besides the admin persona: the viewers of the department. */
export const EXECUTIVE_VIEWERS: readonly SpacePerson[] = [
  person("hoai-nam", "Trương Hoài Nam"),
  person("bao-ngoc", "Châu Bảo Ngọc"),
];

export const ERP_ENGINEERS_TEAM = {
  id: "mock-team-erp-engineers",
  name: "Kỹ sư ERP",
  members: [
    { userId: ERP_PEOPLE.MQ.id, role: "Lead" as const },
    { userId: ERP_PEOPLE.NA.id, role: "Member" as const },
    { userId: ERP_PEOPLE.VT.id, role: "Member" as const },
    { userId: ERP_PEOPLE.TT.id, role: "Member" as const },
  ],
};

export const EXECUTIVE_TEAM = {
  id: "mock-team-executive",
  name: "Ban giám đốc",
  members: [
    { userId: EXECUTIVE_VIEWERS[0]!.id, role: "Lead" as const },
    { userId: mockIds.users.admin, role: "Member" as const },
    { userId: EXECUTIVE_VIEWERS[1]!.id, role: "Member" as const },
  ],
};
