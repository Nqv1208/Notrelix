import { describe, expect, it } from "vitest";
import {
  buildSharingImpact,
  buildSharingLog,
  buildSharingRows,
  planLevelChange,
  planRemoval,
  toBoardSharingData,
  type BoardSharingData,
} from "../board-share-models";
import { en, vi, fillBoardShareCopy } from "../board-share.copy";

const initialsOf = (name: string) => name.slice(0, 2).toUpperCase();

type Entry = BoardSharingData["entries"][number];

const entry = (over: Partial<Entry> & Pick<Entry, "id" | "name">): Entry => ({
  kind: "person",
  email: null,
  you: false,
  owner: false,
  level: "viewer",
  levelCapped: false,
  source: "grant",
  permissionId: null,
  memberRole: null,
  spaceRole: null,
  teamPeople: null,
  outsideSpace: false,
  ...over,
});

const data: BoardSharingData = {
  visibility: "Workspace",
  spaceName: "Công nghệ",
  spacePeople: 24,
  canManage: true,
  entriesDisclosed: true,
  peopleCount: 27,
  guestCount: 1,
  entries: [
    entry({
      id: "u-phuc",
      name: "Lê Hoàng Phúc",
      email: "phuc@x.vn",
      owner: true,
      level: "manager",
      source: "member",
      memberRole: "Owner",
      spaceRole: "Admin",
    }),
    entry({
      id: "t-kt",
      kind: "team",
      name: "Nhóm Kế toán tổng hợp",
      permissionId: "perm-kt",
      teamPeople: 6,
      outsideSpace: true,
    }),
    entry({
      id: "u-linh",
      name: "Đỗ Khánh Linh",
      level: "commenter",
      permissionId: "perm-linh",
      outsideSpace: true,
    }),
    entry({
      id: "u-tuan",
      kind: "guest",
      name: "Đặng Minh Tuấn",
      level: "editor",
      levelCapped: true,
      permissionId: "perm-tuan",
      outsideSpace: true,
    }),
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
      at: "2026-10-08T07:12:00.000Z",
    },
    {
      id: "l2",
      kind: "visibility",
      actor: { id: "u-ha", name: "Nguyễn Thu Hà" },
      target: null,
      from: null,
      to: null,
      guest: false,
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
  levelCapped: p.levelCapped,
  outsideSpace: fillBoardShareCopy(p.outsideDepartment, {
    space: "phòng Công nghệ",
  }),
};

describe("toBoardSharingData", () => {
  it("maps the producer's read, naming levels, kinds, sources and roles", () => {
    const mapped = toBoardSharingData({
      canManage: true,
      entriesDisclosed: true,
      visibility: "Private",
      peopleCount: 3,
      guestCount: 1,
      entries: [
        {
          id: "u1",
          kind: "guest",
          name: "Khách",
          level: "editor",
          levelCapped: true,
          source: "both",
          permissionId: "perm-1",
          memberRole: "Guest",
        } as never,
      ],
      log: [
        {
          id: "l1",
          kind: "private",
          actor: { userId: "u9", name: "Hà" },
          at: "2026-10-01T00:00:00Z",
        },
        { id: "l2", kind: "somethingNew", at: "2026-10-02T00:00:00Z" },
      ],
      privateImpact: null,
    });
    expect(mapped.visibility).toBe("Private");
    expect(mapped.entries[0]).toMatchObject({
      kind: "guest",
      level: "editor",
      levelCapped: true,
      source: "both",
      permissionId: "perm-1",
      memberRole: "Guest",
    });
    expect(mapped.log.map((item) => item.kind)).toEqual([
      "private",
      "visibility",
    ]);
    expect(mapped.log[0]!.actor).toEqual({ id: "u9", name: "Hà" });
    expect(mapped.privateImpact).toBeNull();
  });

  it("reads a counts-only answer as no entries, no log and no impact", () => {
    const mapped = toBoardSharingData({
      canManage: false,
      entriesDisclosed: false,
      peopleCount: 24,
      guestCount: 2,
      entries: [],
      log: [],
      privateImpact: null,
    });
    expect(mapped).toMatchObject({
      canManage: false,
      entriesDisclosed: false,
      peopleCount: 24,
      guestCount: 2,
      entries: [],
      log: [],
      privateImpact: null,
    });
  });
});

describe("buildSharingRows", () => {
  it("words only the entries given on the Board, with level, badge and the guest cap", () => {
    const rows = buildSharingRows(data, words, initialsOf);
    expect(rows.map((row) => [row.name, row.detail, row.level])).toEqual([
      ["Lê Hoàng Phúc", "Chủ board · Quản trị Phòng Công nghệ", "manage"],
      [
        "Nhóm Kế toán tổng hợp",
        "Nhóm workspace · 6 người · ngoài phòng Công nghệ",
        "view",
      ],
      ["Đỗ Khánh Linh", null, "comment"],
      [
        "Đặng Minh Tuấn",
        "khách, chỉ thấy board này · khách tối đa Có thể xem",
        "edit",
      ],
    ]);
    expect(rows[0]).toMatchObject({ fixed: true, badge: null });
    expect(rows[3]).toMatchObject({ badge: "outside" });
  });
});

describe("buildSharingLog", () => {
  const logWords = {
    levels: {
      manage: vi.levels.manage,
      edit: vi.levels.edit,
      comment: vi.levels.comment,
      view: vi.levels.view,
    },
    text: vi.log,
    fill: fillBoardShareCopy,
    when: (iso: string) => iso.slice(0, 10),
  };

  it("words each change with its strong parts, a guest without an end date", () => {
    const log = buildSharingLog(data, logWords, initialsOf);
    expect(log[0]!.text).toBe(
      "**Nguyễn Thu Hà** thêm **Đặng Minh Tuấn** (khách, Có thể sửa)",
    );
    expect(log[1]).toMatchObject({
      text: "**Nguyễn Thu Hà** đổi người được mở board",
      note: null,
    });
  });

  it("has an English wording too", () => {
    expect(en.log.changed).toContain("{{from}}");
    expect(en.log.visibility).toContain("{{actor}}");
  });
});

describe("buildSharingImpact", () => {
  const impactWords = {
    keepInSpace: (names: string) =>
      fillBoardShareCopy(vi.privateConfirm.keepInSpace, { names }),
    keepGuest: (name: string) =>
      fillBoardShareCopy(vi.privateConfirm.keepGuest, { name }),
    and: vi.privateConfirm.keepAnd,
  };

  it("keeps the entries in the department in one group and names the guest", () => {
    const impact = buildSharingImpact(
      {
        ...data,
        entries: [
          ...data.entries,
          entry({
            id: "u-ha",
            name: "Nguyễn Thu Hà",
            spaceRole: "Admin",
            level: "manager",
          }),
        ],
      },
      impactWords,
      initialsOf,
    )!;
    expect(impact.lose).toBe(22);
    expect(impact.people.map((person) => person.name)).toEqual([
      "Trần Minh Quân",
    ]);
    expect(impact.keepText).toBe(
      "**Lê Hoàng Phúc, Nguyễn Thu Hà** (đã được thêm riêng), **Nhóm Kế toán tổng hợp**, **Đỗ Khánh Linh** và khách **Đặng Minh Tuấn**.",
    );
  });

  it("has no impact for a reader the producer gives none", () => {
    expect(
      buildSharingImpact(
        { ...data, privateImpact: null },
        impactWords,
        initialsOf,
      ),
    ).toBeNull();
  });
});

describe("planLevelChange", () => {
  it("changes a Team and a person held by grant with the grant", () => {
    expect(planLevelChange(data.entries[1]!, "editor")).toEqual([
      {
        kind: "grant",
        subjectType: "Team",
        subjectId: "t-kt",
        level: "editor",
        expectedVersion: null,
      },
    ]);
    expect(planLevelChange(data.entries[2]!, "viewer")).toEqual([
      {
        kind: "grant",
        subjectType: "User",
        subjectId: "u-linh",
        level: "viewer",
        expectedVersion: null,
      },
    ]);
  });

  it("carries the version of the grant it changes", () => {
    const held = { ...data.entries[2]!, permissionVersion: 4 };
    expect(planLevelChange(held, "editor")).toEqual([
      {
        kind: "grant",
        subjectType: "User",
        subjectId: "u-linh",
        level: "editor",
        expectedVersion: 4,
      },
    ]);
  });

  it("changes a Board member's role with the member operation", () => {
    const member = entry({
      id: "u-m",
      name: "M",
      level: "editor",
      source: "member",
      memberRole: "Member",
    });
    expect(planLevelChange(member, "manager")).toEqual([
      { kind: "member-role", userId: "u-m", role: "Admin" },
    ]);
    expect(planLevelChange(member, "editor")).toEqual([]);
  });

  it("adds a grant for Can comment, which no Board role gives, and for an entry that also has a grant", () => {
    const member = entry({
      id: "u-m",
      name: "M",
      level: "editor",
      source: "member",
      memberRole: "Member",
    });
    expect(planLevelChange(member, "commenter")).toEqual([
      { kind: "member-role", userId: "u-m", role: "Observer" },
      {
        kind: "grant",
        subjectType: "User",
        subjectId: "u-m",
        level: "commenter",
        expectedVersion: null,
      },
    ]);
    const both = { ...member, source: "both" as const, permissionId: "perm-m" };
    expect(planLevelChange(both, "viewer")).toEqual([
      { kind: "member-role", userId: "u-m", role: "Observer" },
      {
        kind: "grant",
        subjectType: "User",
        subjectId: "u-m",
        level: "viewer",
        expectedVersion: null,
      },
    ]);
  });

  it("changes a Guest by grant and leaves the owner and the caller alone", () => {
    expect(planLevelChange(data.entries[3]!, "manager")).toEqual([
      {
        kind: "grant",
        subjectType: "User",
        subjectId: "u-tuan",
        level: "manager",
        expectedVersion: null,
      },
    ]);
    expect(planLevelChange(data.entries[0]!, "viewer")).toEqual([]);
    expect(
      planLevelChange(entry({ id: "me", name: "Me", you: true }), "viewer"),
    ).toEqual([]);
  });
});

describe("planRemoval", () => {
  it("revokes a grant and removes a membership, each with its own operation", () => {
    expect(planRemoval(data.entries[2]!)).toEqual([
      { kind: "revoke", permissionId: "perm-linh" },
    ]);
    expect(
      planRemoval(
        entry({
          id: "u-m",
          name: "M",
          source: "both",
          permissionId: "perm-m",
          memberRole: "Member",
        }),
      ),
    ).toEqual([
      { kind: "revoke", permissionId: "perm-m" },
      { kind: "member-remove", userId: "u-m" },
    ]);
  });

  it("never removes the owner or the caller", () => {
    expect(planRemoval(data.entries[0]!)).toEqual([]);
    expect(planRemoval(entry({ id: "me", name: "Me", you: true }))).toEqual([]);
  });
});
