import { describe, expect, it } from "vitest";
import type { BoardSharingData } from "@notrelix/work-management-web";
import {
  normalizeSharing,
  planChangeLevel,
  planRemove,
} from "../board-sharing-writes";

type Entry = BoardSharingData["entries"][number];

function entry(over: Partial<Entry>): Entry {
  return {
    id: "user-1",
    kind: "person",
    userId: "user-1",
    name: "A",
    email: null,
    you: false,
    owner: false,
    spaceRole: null,
    department: null,
    teamPeople: null,
    outsideSpace: false,
    outsideCompany: false,
    level: "viewer",
    expiresAt: null,
    permissionId: "perm-1",
    source: "grant",
    ...over,
  };
}

const BOARD = "board-1";
const GRANTS = "/resources/work-management.board/board-1/permissions";

describe("Board share writes use the producer routes", () => {
  it("changes a grant by granting the person again", () => {
    expect(planChangeLevel(BOARD, entry({}), "editor")).toEqual([
      {
        method: "POST",
        path: GRANTS,
        body: { subjectType: "User", subjectId: "user-1", level: "editor" },
      },
    ]);
  });

  it("grants a Team by its id", () => {
    const team = entry({ id: "team-1", kind: "team", userId: null });
    expect(planChangeLevel(BOARD, team, "viewer")[0]).toMatchObject({
      body: { subjectType: "Team", subjectId: "team-1" },
    });
  });

  it("changes a membership through its Board role", () => {
    const member = entry({ permissionId: null, source: "member" });
    expect(planChangeLevel(BOARD, member, "manager")).toEqual([
      {
        method: "PATCH",
        path: "/boards/board-1/members/user-1/role",
        body: { role: "Admin" },
      },
    ]);
  });

  it("refuses a level a membership cannot hold instead of guessing", () => {
    const member = entry({ permissionId: null, source: "member" });
    expect(() => planChangeLevel(BOARD, member, "commenter")).toThrow();
  });

  it("removes the grant and the membership of an entry that has both", () => {
    const both = entry({ source: "both" });
    expect(planRemove(BOARD, both).map((r) => [r.method, r.path])).toEqual([
      ["DELETE", `${GRANTS}/perm-1`],
      ["DELETE", "/boards/board-1/members/user-1"],
    ]);
  });

  it("reads a producer entry for the dialog", () => {
    const raw = {
      entries: [
        {
          ...entry({ id: "g", kind: "guest" }),
          userId: undefined,
          outsideCompany: undefined,
        },
        {
          ...entry({ id: "t", kind: "team" }),
          userId: undefined,
          outsideCompany: undefined,
        },
      ],
    } as unknown as BoardSharingData;
    const [guest, team] = normalizeSharing(raw).entries;
    expect(guest).toMatchObject({ userId: "g", outsideCompany: true });
    expect(team).toMatchObject({ userId: null, outsideCompany: false });
  });
});
