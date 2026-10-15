import { describe, expect, it } from "vitest";
import { identityToneFor } from "../../../core/model/navigation";
import type { SpaceMemberEntry } from "../../../core/model/spaces";
import {
  buildAccessView,
  buildAccessViewFromSummary,
  buildHeadPeople,
  buildHeadPeopleOfTotal,
  spaceWhenLabel,
} from "../space-page-models";
import { workspaceSpacesCopy } from "../workspace-spaces.copy";

const copy = workspaceSpacesCopy("vi");

const entry = (
  over: Partial<SpaceMemberEntry> & Pick<SpaceMemberEntry, "subjectId">,
): SpaceMemberEntry => ({
  subjectType: "User",
  role: "Member",
  displayName: over.subjectId,
  avatarUrl: null,
  addedAt: "2026-09-01T00:00:00.000Z",
  ...over,
});

describe("buildHeadPeople", () => {
  it("shows the first four people and counts the rest, teams included", () => {
    const members = [
      entry({ subjectId: "a", role: "Admin" }),
      entry({ subjectId: "t", subjectType: "Team" }),
      ...["b", "c", "d", "e"].map((id) => entry({ subjectId: id })),
    ];
    const people = buildHeadPeople(copy, members, identityToneFor);
    expect(people.avatars.map((avatar) => avatar.id)).toEqual([
      "a",
      "b",
      "c",
      "d",
    ]);
    expect(people.more).toBe(2);
    expect(people.label).toBe("6 người và nhóm");
  });

  it("draws no bubble when everyone fits", () => {
    const people = buildHeadPeople(
      copy,
      [entry({ subjectId: "a", role: "Admin" })],
      identityToneFor,
    );
    expect(people.more).toBe(0);
  });
});

describe("buildAccessView", () => {
  const members = [
    entry({ subjectId: "a", role: "Admin", displayName: "Hà" }),
    entry({ subjectId: "b", role: "Admin", displayName: "Phúc" }),
    entry({ subjectId: "c", role: "Member", displayName: "Quân" }),
    entry({
      subjectId: "t1",
      subjectType: "Team",
      role: "Member",
      displayName: "Kỹ sư ERP",
    }),
    entry({
      subjectId: "t2",
      subjectType: "Team",
      role: "Viewer",
      displayName: "Ban giám đốc",
    }),
  ];

  it("summarises the entries by role and names the admins", () => {
    const view = buildAccessView(
      copy,
      { visibility: "Private" },
      members,
      [
        { id: "t1", name: "Kỹ sư ERP", memberCount: 4 },
        { id: "t2", name: "Ban giám đốc", memberCount: 3 },
      ],
      { meId: "a" },
    );
    expect(view.summary).toBe(
      "3 người và 2 nhóm: 2 quản trị · 2 thành viên · 1 người xem",
    );
    expect(view.rows[0]).toMatchObject({
      label: "Quản trị",
      value: "Hà, Phúc",
    });
    const member = view.rows.find((row) => row.key === "Member");
    expect(member?.value).toBe("1 người");
    expect(member?.note).toBe("gồm nhóm Kỹ sư ERP (4 người)");
    expect(view.rows.find((row) => row.key === "Viewer")?.value).toBe("1 nhóm");
  });

  it("leaves the team head count out when the member cannot read the teams", () => {
    const view = buildAccessView(
      copy,
      { visibility: "Private" },
      members,
      null,
      {
        meId: null,
      },
    );
    expect(view.rows.find((row) => row.key === "Member")?.note).toBeNull();
  });

  it("marks a role nobody holds as empty", () => {
    const view = buildAccessView(
      copy,
      { visibility: "Workspace" },
      [entry({ subjectId: "a", role: "Admin", displayName: "Hà" })],
      [],
      { meId: null },
    );
    expect(view.rows.find((row) => row.key === "Viewer")).toMatchObject({
      value: "Chưa có ai",
      empty: true,
    });
  });

  it("has no summary until the members are known", () => {
    expect(
      buildAccessView(copy, { visibility: "Private" }, null, null, {
        meId: null,
      }),
    ).toEqual({ visibility: "Private", summary: null, rows: [] });
  });
});

describe("spaceWhenLabel", () => {
  const now = Date.parse("2026-10-12T10:40:00+07:00");
  const at = (iso: string) => spaceWhenLabel(copy, iso, now);

  it("reads minutes, hours, yesterday, days and then the date", () => {
    expect(at("2026-10-12T10:39:30+07:00")).toBe("vừa xong");
    expect(at("2026-10-12T10:30:00+07:00")).toBe("10 phút trước");
    expect(at("2026-10-12T08:40:00+07:00")).toBe("2 giờ trước");
    expect(at("2026-10-11T08:40:00+07:00")).toBe("hôm qua");
    // The day before is "hôm qua" even when it is under 24 hours ago.
    expect(at("2026-10-11T16:00:00+07:00")).toBe("hôm qua");
    expect(at("2026-10-07T09:00:00+07:00")).toBe("5 ngày trước");
    expect(at("2026-10-03T09:00:00+07:00")).toBe("03/10");
  });

  it("says nothing for a missing or future time", () => {
    expect(spaceWhenLabel(copy, null, now)).toBe("");
    expect(at("2026-10-13T09:00:00+07:00")).toBe("");
  });
});

describe("buildHeadPeopleOfTotal", () => {
  it("names the people it is given and counts the rest of the head count", () => {
    const people = buildHeadPeopleOfTotal(
      copy,
      [
        { id: "a", name: "Lê Hoàng Phúc" },
        { id: "b", name: "Nguyễn Thu Hà" },
      ],
      24,
      identityToneFor,
    );
    expect(people.avatars.map((avatar) => avatar.id)).toEqual(["a", "b"]);
    expect(people.more).toBe(22);
    expect(people.label).toBe("24 thành viên");
  });
});

describe("buildAccessViewFromSummary", () => {
  const summary = {
    peopleTotal: 24,
    admins: [
      { id: "a", name: "Lê Hoàng Phúc" },
      { id: "b", name: "Nguyễn Thu Hà" },
    ],
    members: { people: 19, teams: [{ name: "Kỹ sư ERP", count: 4 }] },
    viewers: { people: 3, teams: [{ name: "Ban giám đốc", count: 3 }] },
    guests: [
      {
        id: "g",
        name: "Đặng Minh Tuấn",
        boards: [{ id: "erp", name: "Triển khai ERP Q4" }],
      },
    ],
  };

  it("counts everybody once and notes the teams under their role", () => {
    const view = buildAccessViewFromSummary(
      copy,
      { visibility: "Private" },
      summary,
    );
    expect(view.summary).toBe(
      "24 người: 2 quản trị · 19 thành viên · 3 người xem",
    );
    expect(view.rows.map((row) => [row.label, row.value, row.note])).toEqual([
      ["Quản trị", "Lê Hoàng Phúc, Nguyễn Thu Hà", null],
      ["Thành viên", "19 người", "gồm nhóm Kỹ sư ERP (4 người)"],
      ["Người xem", "3 người", "nhóm Ban giám đốc, chỉ xem"],
      [
        "Khách ở board",
        "Đặng Minh Tuấn",
        "chỉ board Triển khai ERP Q4, không thuộc phòng",
      ],
    ]);
    // A single guest on a single Board opens that Board.
    expect(view.rows[3]!.destination).toEqual({
      section: "board",
      boardId: "erp",
    });
  });

  it("says nobody holds a role that has no one and omits the guest row", () => {
    const view = buildAccessViewFromSummary(
      copy,
      { visibility: "Private" },
      {
        peopleTotal: 1,
        admins: [{ id: "a", name: "Nguyễn Thu Hà" }],
        members: { people: 0, teams: [] },
        viewers: { people: 0, teams: [] },
        guests: [],
      },
    );
    expect(view.rows.map((row) => row.key)).toEqual([
      "admins",
      "Member",
      "Viewer",
    ]);
    expect(view.rows[1]).toMatchObject({ value: "Chưa có ai", empty: true });
  });
});
