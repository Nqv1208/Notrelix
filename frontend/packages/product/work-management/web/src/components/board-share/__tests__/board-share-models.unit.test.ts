import { describe, expect, it } from "vitest";
import type { BoardAccess, BoardMember } from "@notrelix/work-management-core";
import { buildShareRows, levelOfBoardRole } from "../board-share-models";

const words = {
  owner: "Chủ board",
  team: (count: number) => `Nhóm workspace · ${count} người`,
  guestOnly: "khách, chỉ thấy board này",
  teamName: "Nhóm",
  guests: (count: number) => `${count} khách`,
};
const initialsOf = (name: string) => name.slice(0, 2).toUpperCase();

const member = (
  userId: string,
  name: string,
  role: BoardMember["role"],
): BoardMember => ({
  id: userId,
  userId,
  name,
  initials: "",
  role,
  color: "",
});

const access = (sources: BoardAccess["sources"]): BoardAccess => ({
  boardId: "b",
  version: 1,
  myLevel: "manager",
  mySource: { kind: "owner", grantedBy: null },
  peopleCount: 10,
  guestCount: 1,
  peopleCountComplete: true,
  sources,
});

describe("levelOfBoardRole", () => {
  it("maps the Board member roles to levels", () => {
    expect(levelOfBoardRole("owner")).toBe("manage");
    expect(levelOfBoardRole("editor")).toBe("edit");
    expect(levelOfBoardRole("viewer")).toBe("view");
  });
});

describe("buildShareRows", () => {
  it("puts the owner first with a fixed Manager level, then the other members", () => {
    const rows = buildShareRows(
      [member("u2", "Quân", "editor"), member("u1", "Hà", "owner")],
      null,
      "u1",
      words,
      initialsOf,
    );
    expect(rows.map((row) => row.name)).toEqual(["Hà", "Quân"]);
    expect(rows[0]).toMatchObject({
      isYou: true,
      level: "manage",
      fixed: true,
      detail: "Chủ board",
    });
    expect(rows[1]).toMatchObject({ level: "edit", fixed: false });
  });

  it("adds the teams and the guests the access read lists, without a level", () => {
    const rows = buildShareRows(
      [member("u1", "Hà", "owner")],
      access([
        { kind: "space", id: "s", name: "Phòng", count: 24 },
        { kind: "team", id: "t", name: "Kế toán", count: 6 },
        { kind: "guest", id: null, name: null, count: 2 },
      ]),
      null,
      words,
      initialsOf,
    );
    expect(rows.map((row) => row.kind)).toEqual(["person", "team", "guest"]);
    expect(rows[1]).toMatchObject({
      detail: "Nhóm workspace · 6 người",
      level: null,
    });
    expect(rows[2]).toMatchObject({
      name: "2 khách",
      badge: "guest",
      level: null,
    });
  });

  it("does not repeat a member who is also a named user source", () => {
    const rows = buildShareRows(
      [member("u1", "Hà", "owner")],
      access([{ kind: "user", id: "u1", name: "Hà", count: 1 }]),
      null,
      words,
      initialsOf,
    );
    expect(rows).toHaveLength(1);
  });

  it("hides a subject the caller may not see", () => {
    const rows = buildShareRows(
      [],
      access([
        { kind: "team", id: null, name: null, count: 3 },
        { kind: "user", id: null, name: null, count: 1 },
      ]),
      null,
      words,
      initialsOf,
    );
    expect(rows).toEqual([]);
  });
});
