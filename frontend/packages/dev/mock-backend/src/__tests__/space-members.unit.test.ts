import { describe, expect, it } from "vitest";
import { defaultConfig } from "../config/mock-config";
import { mockIds } from "../state/mock-ids";
import { spaceMembersExtra } from "../state/mock-space-members";
import { MockStore } from "../state/mock-store";

function extra(spaceId: string = mockIds.spaces.engineering) {
  const store = new MockStore({ ...defaultConfig });
  return {
    store,
    result: spaceMembersExtra(
      store,
      mockIds.workspaces.primary,
      spaceId,
      () => true,
    )!,
  };
}

describe("department members in the mock world (CTR-GAP-WS-MEMBERS-EXT)", () => {
  it("counts the roles with the teams expanded and the waiting invitation", () => {
    const { result } = extra();
    expect(result.summary).toEqual({
      admins: 2,
      members: 19,
      viewers: 3,
      invites: 1,
    });
    expect(result.people).toHaveLength(24);
    expect(result.invitations).toEqual([
      expect.objectContaining({
        email: "khoa.ngo@minhan.vn",
        name: "Ngô Đăng Khoa",
        role: "Member",
      }),
    ]);
  });

  it("gives each person an email, a job line and a last activity", () => {
    const { result } = extra();
    const phuc = result.people.find((p) => p.userId === "mock-user-erp-phuc")!;
    expect(phuc).toMatchObject({
      email: "phuc.le@minhan.vn",
      title: "Trưởng phòng",
      activeNow: false,
      viaTeams: [],
    });
    const ha = result.people.find((p) => p.userId === mockIds.users.owner)!;
    expect(ha).toMatchObject({ activeNow: true, creator: true });
  });

  it("says Trần Minh Quân is also in the team Nhóm Kỹ sư ERP", () => {
    const { result } = extra();
    const quan = result.people.find((p) => p.userId === "mock-user-erp-quan")!;
    expect(quan.viaTeams).toEqual(["Nhóm Kỹ sư ERP"]);
  });

  it("lists the guest of a Board with level", () => {
    const { result } = extra();
    expect(result.guests).toEqual([
      expect.objectContaining({
        name: "Đặng Minh Tuấn",
        outside: true,
        boardName: "Triển khai ERP Q4",
        level: "editor",
      }),
    ]);
  });

  it("hides the Boards the viewer cannot read", () => {
    const store = new MockStore({ ...defaultConfig });
    const none = spaceMembersExtra(
      store,
      mockIds.workspaces.primary,
      mockIds.spaces.engineering,
      () => false,
    )!;
    expect(none.guests).toEqual([]);
  });
});
