import { describe, expect, it } from "vitest";
import {
  buildSharingImpact,
  buildSharingLog,
  buildSharingRows,
  shareEndDate,
  type BoardSharingData,
} from "../board-share-models";
import { en, vi, fillBoardShareCopy } from "../board-share.copy";

const initialsOf = (name: string) => name.slice(0, 2).toUpperCase();

const data: BoardSharingData = {
  spaceName: "Công nghệ",
  spacePeople: 24,
  canManage: true,
  entries: [
    {
      id: "e1",
      kind: "person",
      userId: "u-phuc",
      name: "Lê Hoàng Phúc",
      email: "phuc@x.vn",
      you: false,
      owner: true,
      spaceRole: "Admin",
      department: null,
      teamPeople: null,
      outsideSpace: false,
      outsideCompany: false,
      level: "manager",
      expiresAt: null,
    },
    {
      id: "e2",
      kind: "team",
      userId: null,
      name: "Nhóm Kế toán tổng hợp",
      email: null,
      you: false,
      owner: false,
      spaceRole: null,
      department: null,
      teamPeople: 6,
      outsideSpace: true,
      outsideCompany: false,
      level: "viewer",
      expiresAt: null,
    },
    {
      id: "e3",
      kind: "person",
      userId: "u-linh",
      name: "Đỗ Khánh Linh",
      email: "linh@x.vn",
      you: false,
      owner: false,
      spaceRole: null,
      department: "Kế toán",
      teamPeople: null,
      outsideSpace: true,
      outsideCompany: false,
      level: "commenter",
      expiresAt: "2026-12-31T16:59:59.000Z",
    },
    {
      id: "e4",
      kind: "guest",
      userId: "u-tuan",
      name: "Đặng Minh Tuấn",
      email: "tuan@doitac-erp.vn",
      you: false,
      owner: false,
      spaceRole: null,
      department: null,
      teamPeople: null,
      outsideSpace: true,
      outsideCompany: true,
      level: "editor",
      expiresAt: "2027-01-31T16:59:59.000Z",
    },
  ],
  log: [
    {
      id: "l1",
      kind: "added",
      actor: { id: "u-ha", name: "Nguyễn Thu Hà" },
      target: "Đặng Minh Tuấn",
      from: null,
      to: "editor",
      guest: true,
      expiresAt: "2027-01-31T16:59:59.000Z",
      at: "2026-10-08T07:12:00.000Z",
    },
    {
      id: "l2",
      kind: "expired",
      actor: null,
      target: "Nguyễn Văn Bình",
      from: null,
      to: null,
      guest: false,
      expiresAt: null,
      at: "2026-09-30T03:00:00.000Z",
    },
  ],
  privateImpact: {
    lose: 22,
    people: [
      { userId: "u-quan", name: "Trần Minh Quân", items: 2, overdue: 1 },
    ],
    keep: [],
  },
};

const p = vi.people;
const words = {
  owner: p.owner,
  team: (count: number) => fillBoardShareCopy(p.team, { count }),
  guestOnly: p.guestOnly,
  inSpace: (role: "Admin" | "Member" | "Viewer") =>
    fillBoardShareCopy(p.inSpace, {
      role: p.roles[role],
      space: "Phòng Công nghệ",
    }),
  department: (name: string) => `Phòng ${name}`,
  outsideSpace: fillBoardShareCopy(p.outsideDepartment, {
    space: "phòng Công nghệ",
  }),
  formatDay: (iso: string) => iso,
};

describe("buildSharingRows", () => {
  it("words only the entries given on the Board, with level, badge and end date", () => {
    const rows = buildSharingRows(data, words, initialsOf);
    expect(rows.map((row) => [row.name, row.detail, row.level])).toEqual([
      ["Lê Hoàng Phúc", "Chủ board · Quản trị Phòng Công nghệ", "manage"],
      [
        "Nhóm Kế toán tổng hợp",
        "Nhóm workspace · 6 người · ngoài phòng Công nghệ",
        "view",
      ],
      ["Đỗ Khánh Linh", "Phòng Kế toán · ngoài phòng Công nghệ", "comment"],
      [
        "Đặng Minh Tuấn",
        "tuan@doitac-erp.vn · khách, chỉ thấy board này",
        "edit",
      ],
    ]);
    expect(rows[0]).toMatchObject({ fixed: true, badge: null });
    expect(rows[3]).toMatchObject({ badge: "outside", expires: "31/01/2027" });
    expect(rows[2]!.expires).toBe("31/12/2026");
  });
});

describe("buildSharingLog", () => {
  it("words each change with its strong parts and a time within the week", () => {
    const log = buildSharingLog(
      data,
      {
        levels: {
          manage: vi.levels.manage,
          edit: vi.levels.edit,
          comment: vi.levels.comment,
          view: vi.levels.view,
        },
        text: vi.log,
        expiredNote: vi.log.expiredNote,
        fill: fillBoardShareCopy,
        when: (iso) => iso.slice(0, 10),
      },
      initialsOf,
    );
    expect(log[0]!.text).toBe(
      "**Nguyễn Thu Hà** thêm **Đặng Minh Tuấn** (khách, Có thể sửa, hết quyền 31/01/2027)",
    );
    expect(log[1]).toMatchObject({
      text: "**Hệ thống:** quyền của **Nguyễn Văn Bình** hết hạn",
      note: "gia hạn phải thêm lại quyền",
      actor: null,
    });
  });

  it("has an English wording too", () => {
    expect(en.log.changed).toContain("{{from}}");
  });
});

describe("buildSharingImpact", () => {
  it("keeps the entries in the department in one group and names the guest", () => {
    const impact = buildSharingImpact(
      {
        ...data,
        entries: [
          ...data.entries,
          {
            ...data.entries[0]!,
            id: "e5",
            name: "Nguyễn Thu Hà",
            owner: false,
          },
        ],
      },
      {
        keepInSpace: (names) =>
          fillBoardShareCopy(vi.privateConfirm.keepInSpace, { names }),
        keepGuest: (name) =>
          fillBoardShareCopy(vi.privateConfirm.keepGuest, { name }),
        and: vi.privateConfirm.keepAnd,
      },
      initialsOf,
    );
    expect(impact.lose).toBe(22);
    expect(impact.people.map((person) => person.name)).toEqual([
      "Trần Minh Quân",
    ]);
    expect(impact.keepText).toBe(
      "**Lê Hoàng Phúc, Nguyễn Thu Hà** (đã được thêm riêng), **Nhóm Kế toán tổng hợp**, **Đỗ Khánh Linh** và khách **Đặng Minh Tuấn**.",
    );
  });
});

describe("shareEndDate", () => {
  it("reads the day in the member's calendar", () => {
    expect(shareEndDate(new Date(2027, 0, 31, 23, 59).toISOString())).toBe(
      "31/01/2027",
    );
  });
});
