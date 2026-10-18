import { describe, expect, it } from "vitest";
import { identityToneFor } from "../../../core/model/navigation";
import type { SpaceMemberEntry, SpaceTeam } from "../../../core/model/spaces";
import {
  buildMemberRows,
  filterMemberRows,
  memberSummary,
  NO_MEMBER_FILTERS,
  sortMemberEntries,
  suggestTokens,
  teamRemoval,
  type SpaceDirectoryPerson,
  type TeamPersonView,
} from "../space-members-models";
import { workspaceSpacesCopy } from "../workspace-spaces.copy";

const copy = workspaceSpacesCopy("vi");

const entry = (
  over: Partial<SpaceMemberEntry> & Pick<SpaceMemberEntry, "subjectId">,
): SpaceMemberEntry => ({
  subjectType: "User",
  role: "Member",
  displayName: over.subjectId,
  avatarUrl: null,
  addedAt: "2026-09-01T09:00:00+07:00",
  ...over,
});

const entries = [
  entry({ subjectId: "u1", role: "Viewer", displayName: "Zed" }),
  entry({ subjectId: "u2", role: "Admin", displayName: "Hà" }),
  entry({
    subjectId: "t1",
    subjectType: "Team",
    role: "Member",
    displayName: "ERP",
  }),
  entry({ subjectId: "u3", role: "Member", displayName: "Quân" }),
];

const options = (
  over: Partial<Parameters<typeof buildMemberRows>[2]> = {},
) => ({
  directory: new Map<string, SpaceDirectoryPerson>([
    ["u2", { email: "ha@x.vn", workspaceRole: "admin" }],
  ]),
  teams: [{ id: "t1", name: "ERP", memberCount: 4 }] as SpaceTeam[],
  meId: "u2",
  canManage: true,
  pendingKeys: new Set<string>(),
  toneOf: identityToneFor,
  formatDay: (iso: string) => iso.slice(8, 10) + "/" + iso.slice(5, 7),
  ...over,
});

describe("sortMemberEntries", () => {
  it("puts teams first, then Admin, Member, Viewer, keeping the producer's order", () => {
    expect(sortMemberEntries(entries).map((item) => item.subjectId)).toEqual([
      "t1",
      "u2",
      "u3",
      "u1",
    ]);
  });
});

describe("buildMemberRows", () => {
  it("describes a team by its head count and a person by workspace role", () => {
    const rows = buildMemberRows(copy, entries, options());
    const team = rows[0];
    expect(team).toMatchObject({
      kind: "team",
      detail: "Nhóm workspace · 4 người",
      teamCount: 4,
      activity: "từ 01/09",
    });
    expect(team?.sources[0]?.text).toBe("Thêm trực tiếp cả nhóm");
    const ha = rows[1];
    expect(ha).toMatchObject({
      isYou: true,
      detail: "ha@x.vn · Quản trị workspace",
    });
    expect(ha?.avatar?.id).toBe("u2");
  });

  it("leaves the head count out when the Team list is not readable", () => {
    const rows = buildMemberRows(copy, entries, options({ teams: null }));
    expect(rows[0]).toMatchObject({
      detail: "Nhóm workspace",
      teamCount: null,
    });
  });

  it("offers no role change to a member who cannot manage", () => {
    const rows = buildMemberRows(copy, entries, options({ canManage: false }));
    expect(rows.every((row) => row.roles.length === 0 && !row.canRemove)).toBe(
      true,
    );
  });

  it("blocks lowering and removing the only admin", () => {
    const rows = buildMemberRows(copy, entries, options());
    const ha = rows.find((row) => row.key === "User:u2");
    expect(ha).toMatchObject({ blockedBy: "last-admin", canRemove: false });
    expect(ha?.roles).toEqual(["Admin"]);
  });

  it("adds the other way a person joins, and a note on the first source", () => {
    const rows = buildMemberRows(
      copy,
      entries,
      options({
        directory: new Map([
          [
            "u3",
            {
              email: null,
              workspaceRole: "member",
              sourceNote: "người tạo phòng",
            },
          ],
        ]),
        also: new Map([["u3", { team: "ERP", role: "Member" as const }]]),
      }),
    );
    const quan = rows.find((row) => row.key === "User:u3");
    expect(quan?.sources.map((source) => source.text)).toEqual([
      "Trực tiếp · người tạo phòng",
      "ERP",
    ]);
  });
});

describe("filterMemberRows", () => {
  const rows = buildMemberRows(copy, entries, options());
  it("matches a name without accents and the second line", () => {
    const names = (query: string) =>
      filterMemberRows(rows, { ...NO_MEMBER_FILTERS, query }).map(
        (row) => row.name,
      );
    expect(names("zed")).toEqual(["Zed"]);
    expect(names("ha@x")).toEqual(["Hà"]);
  });
  it("narrows by role and by joining as a person or a whole team", () => {
    expect(
      filterMemberRows(rows, { ...NO_MEMBER_FILTERS, role: "Viewer" }).map(
        (r) => r.name,
      ),
    ).toEqual(["Zed"]);
    expect(
      filterMemberRows(rows, { ...NO_MEMBER_FILTERS, via: "team" }).map(
        (r) => r.name,
      ),
    ).toEqual(["ERP"]);
    expect(
      filterMemberRows(rows, { ...NO_MEMBER_FILTERS, via: "direct" }),
    ).toHaveLength(3);
  });
});

describe("memberSummary", () => {
  it("counts entries by role", () => {
    expect(memberSummary(copy, entries)).toBe(
      "1 quản trị · 2 thành viên · 1 người xem",
    );
  });
});

describe("teamRemoval", () => {
  const person = (id: string): TeamPersonView => ({
    id,
    name: id,
    tone: "blue",
    note: null,
  });
  it("keeps those who are also added directly, with their role", () => {
    const impact = teamRemoval(
      [person("u3"), person("x"), person("u1")],
      entries,
    );
    expect(impact.lose.map((item) => item.id)).toEqual(["x"]);
    expect(impact.keep.map((item) => [item.person.id, item.role])).toEqual([
      ["u3", "Member"],
      ["u1", "Viewer"],
    ]);
  });
});

describe("suggestTokens", () => {
  const users = [
    { id: "u1", name: "Đỗ Văn Hùng", avatarUrl: null, email: "hung@x.vn" },
    { id: "u2", name: "Hà", avatarUrl: null },
  ];
  const teams = [{ id: "t1", name: "Nhóm QA", memberCount: 5 }];
  it("finds people by name or email and teams by name, never an existing entry", () => {
    expect(suggestTokens("hung", users, teams, new Set())).toHaveLength(1);
    expect(suggestTokens("qa", users, teams, new Set())).toHaveLength(1);
    expect(
      suggestTokens("qa", users, teams, new Set(["Team:t1"])),
    ).toHaveLength(0);
    expect(
      suggestTokens("do van", users, teams, new Set(["User:u1"])),
    ).toHaveLength(0);
    expect(suggestTokens("  ", users, teams, new Set())).toEqual([]);
  });
});

describe("what the producer adds to a member row", () => {
  it("shows a person's last activity and a short chip for the team they are also in", () => {
    const rows = buildMemberRows(copy, entries, {
      ...options(),
      directory: new Map<string, SpaceDirectoryPerson>([
        [
          "u3",
          {
            email: "quan@x.vn",
            workspaceRole: null,
            title: "Kỹ sư dữ liệu",
            activity: "2 giờ trước",
          },
        ],
      ]),
      also: new Map([
        ["u3", { team: "Nhóm Kỹ sư ERP", role: "Member" as const }],
      ]),
    });
    const quan = rows.find((row) => row.key === "User:u3")!;
    expect(quan.activity).toBe("2 giờ trước");
    expect(quan.detail).toBe("quan@x.vn · Kỹ sư dữ liệu");
    expect(quan.sources.map((source) => source.text)).toEqual([
      "Trực tiếp",
      "Kỹ sư ERP",
    ]);
    // Without the producer's activity the date the person was added shows.
    expect(rows.find((row) => row.key === "User:u1")!.activity).toBe(
      "từ 01/09",
    );
  });

  it("counts the summary over the producer's head counts when it has them", () => {
    expect(memberSummary(copy, entries)).toBe(
      "1 quản trị · 2 thành viên · 1 người xem",
    );
    expect(
      memberSummary(copy, entries, { admins: 2, members: 19, viewers: 3 }),
    ).toBe("2 quản trị · 19 thành viên · 3 người xem");
  });
});
