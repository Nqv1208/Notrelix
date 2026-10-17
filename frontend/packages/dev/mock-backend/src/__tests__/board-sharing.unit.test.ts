import { describe, expect, it } from "vitest";
import { createNotrelixClient } from "@notrelix/contracts";
import { defaultConfig } from "../config/mock-config";
import { boardSharing } from "../state/mock-board-sharing";
import { mockIds } from "../state/mock-ids";
import { MockStore } from "../state/mock-store";
import { createMockFetch } from "../transport/create-mock-fetch";

function setup() {
  const store = new MockStore({ ...defaultConfig });
  const client = createNotrelixClient({
    baseUrl: "http://localhost:8000/api/v1",
    fetchImpl: createMockFetch(store),
  });
  return { store, client };
}

describe("Board permissions in the mock world (CTR-GAP-WM-BOARD-SHARING)", () => {
  it("lists only the permissions given on the Board, in the mockup's order", () => {
    const { store } = setup();
    const sharing = boardSharing(store, mockIds.boards.erp, true)!;
    expect(sharing.entries.map((entry) => entry.name)).toEqual([
      "Lê Hoàng Phúc",
      "Nhóm Kế toán tổng hợp",
      "Đỗ Khánh Linh",
      "Đặng Minh Tuấn",
      "Nguyễn Thu Hà",
    ]);
    const [phuc, team, linh, tuan, ha] = sharing.entries;
    expect(phuc).toMatchObject({
      owner: true,
      spaceRole: "Admin",
      level: "manager",
    });
    expect(team).toMatchObject({
      kind: "team",
      teamPeople: 6,
      level: "viewer",
    });
    expect(linh).toMatchObject({
      department: "Kế toán",
      outsideSpace: true,
      level: "commenter",
      expiresAt: "2026-12-31T16:59:59.000Z",
    });
    expect(tuan).toMatchObject({
      kind: "guest",
      outsideCompany: true,
      email: "tuan@doitac-erp.vn",
      level: "editor",
    });
    expect(ha).toMatchObject({ you: true, spaceRole: "Admin" });
  });

  it("counts who loses the Board when it goes Private", () => {
    const { store } = setup();
    const { privateImpact } = boardSharing(store, mockIds.boards.erp, true)!;
    expect(privateImpact.lose).toBe(22);
    expect(privateImpact.people.map((person) => person.name)).toEqual([
      "Trần Minh Quân",
      "Phan Ngọc Anh",
      "Vũ Thanh",
    ]);
  });

  it("changes a level, refuses a guest as manager and logs the change", async () => {
    const { client, store } = setup();
    await client.api.patch(`/boards/${mockIds.boards.erp}/sharing/share-kt`, {
      level: "editor",
    });
    const after = boardSharing(store, mockIds.boards.erp, true)!;
    expect(after.entries[1]!.level).toBe("editor");
    expect(after.log[0]).toMatchObject({
      kind: "changed",
      from: "viewer",
      to: "editor",
    });
    await expect(
      client.api.patch(`/boards/${mockIds.boards.erp}/sharing/share-dt`, {
        level: "manager",
      }),
    ).rejects.toBeTruthy();
  });

  it("removes a permission but keeps the owner and the caller", async () => {
    const { client, store } = setup();
    await client.api.delete(`/boards/${mockIds.boards.erp}/sharing/share-kl`);
    expect(
      boardSharing(store, mockIds.boards.erp, true)!.entries.map((e) => e.name),
    ).not.toContain("Đỗ Khánh Linh");
    await expect(
      client.api.delete(`/boards/${mockIds.boards.erp}/sharing/share-hp`),
    ).rejects.toBeTruthy();
  });

  it("makes the Board Private and adds the chosen people", async () => {
    const { client, store } = setup();
    await client.api.post(`/boards/${mockIds.boards.erp}/sharing/private`, {
      addUserIds: ["mock-user-erp-quan"],
    });
    expect(store.getBoard(mockIds.boards.erp)!.visibility).toBe("private");
    const names = boardSharing(store, mockIds.boards.erp, true)!.entries.map(
      (entry) => entry.name,
    );
    expect(names).toContain("Trần Minh Quân");
  });
});
