import { describe, expect, it } from "vitest";
import { defaultConfig } from "../config/mock-config";
import { mockIds } from "../state/mock-ids";
import { MockStore } from "../state/mock-store";
import { securityExt, teamsExt } from "../state/mock-teams-ext";

function ext() {
  const store = new MockStore({ ...defaultConfig });
  return { store, result: teamsExt(store, mockIds.workspaces.primary) };
}

describe("Teams pages in the mock world (CTR-GAP-WS-TEAMS-EXT)", () => {
  it("seeds four active Teams and one archived, consistent with Space and Members", () => {
    const { store, result } = ext();
    const teams = store.spaces.teamsOf(mockIds.workspaces.primary);
    expect(
      teams
        .filter((team) => !team.archived)
        .map((t) => [t.name, t.members.length]),
    ).toEqual([
      ["Nhóm Kỹ sư ERP", 4],
      ["Ban giám đốc", 3],
      ["Nhóm Kế toán tổng hợp", 6],
      ["Nhóm Kiểm thử QA", 5],
    ]);
    expect(teams.filter((team) => team.archived).map((t) => t.name)).toEqual([
      "Nhóm Trực hệ thống",
    ]);
    expect(result.teams).toHaveLength(5);
  });

  it("says where each Team is in use", () => {
    const { result } = ext();
    const usage = (id: string) =>
      result.teams
        .find((team) => team.id === id)!
        .usage.map((u) => [u.name, u.grant]);
    expect(usage(mockIds.teams.erpEngineers)).toEqual([
      ["Công nghệ", "Member"],
    ]);
    expect(usage(mockIds.teams.executive)).toEqual([
      ["Công nghệ", "Viewer"],
      ["Kế toán", "Viewer"],
    ]);
    expect(usage(mockIds.teams.accounting)).toEqual([
      ["Triển khai ERP Q4", "viewer"],
    ]);
    expect(usage(mockIds.teams.qa)).toEqual([]);
    expect(usage(mockIds.teams.oncall)).toEqual([
      ["Hạ tầng & bảo mật", "viewer"],
    ]);
  });

  it("gives each person an email, a job line and the day joined, and the leaders", () => {
    const { result } = ext();
    const erp = result.teams.find(
      (team) => team.id === mockIds.teams.erpEngineers,
    )!;
    expect(erp.people[0]).toMatchObject({
      name: "Trần Minh Quân",
      role: "Lead",
      email: "quan.tran@minhan.vn",
      title: "Kỹ sư dữ liệu",
    });
    expect(erp.people.find((p) => p.name === "Lâm Gia Minh")!.since).toBe(
      "2026-09-15T02:00:00.000Z",
    );
    const lead = (id: string) =>
      result.teams
        .find((team) => team.id === id)!
        .people.find((p) => p.role === "Lead")!.name;
    expect(lead(mockIds.teams.executive)).toBe("Trịnh Quốc Hưng");
    expect(lead(mockIds.teams.accounting)).toBe("Võ Thị Hạnh");
    expect(lead(mockIds.teams.qa)).toBe("Tô Minh Nhật");
  });

  it("records who archived the paused Team and lists the people who keep a department", () => {
    const { result } = ext();
    const oncall = result.teams.find(
      (team) => team.id === mockIds.teams.oncall,
    )!;
    expect(oncall).toMatchObject({ archivedAt: "2026-09-20T02:00:00.000Z" });
    expect(oncall.archivedBy?.name).toBe("Nguyễn Thu Hà");
    const erp = result.teams.find(
      (team) => team.id === mockIds.teams.erpEngineers,
    )!;
    expect(
      erp.usage[0]!.direct?.some(
        (entry) => entry.userId === "mock-user-erp-quan",
      ),
    ).toBe(true);
  });

  it("lists the workspace admins and the people a Team may take", () => {
    const { result } = ext();
    expect(result.admins.map((admin) => admin.role)).toContain("owner");
    expect(
      result.directory.some((person) => person.name === "Đặng Quốc Bảo"),
    ).toBe(true);
    expect(
      result.directory.every((person) =>
        (person.email ?? "").endsWith("@minhan.vn"),
      ),
    ).toBe(true);
  });

  it("reports the sharing and security extras", () => {
    const store = new MockStore({ ...defaultConfig });
    expect(securityExt(store, mockIds.workspaces.primary)).toMatchObject({
      guestCount: 2,
      guestExpiryDays: 90,
      companyDomain: "minhan.vn",
      linksOffSince: "2026-09-15T02:00:00.000Z",
    });
  });
});
