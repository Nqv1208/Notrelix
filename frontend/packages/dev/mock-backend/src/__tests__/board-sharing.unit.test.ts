import { describe, expect, it } from "vitest";
import { createNotrelixClient, endpoints } from "@notrelix/contracts";
import { defaultConfig } from "../config/mock-config";
import { boardSharing } from "../state/mock-board-sharing";
import { mockIds } from "../state/mock-ids";
import { MockStore } from "../state/mock-store";
import { createMockFetch } from "../transport/create-mock-fetch";

const BOARD = mockIds.boards.erp;
const KIND = "work-management.board";

function setup() {
  const store = new MockStore({ ...defaultConfig });
  const client = createNotrelixClient({
    baseUrl: "http://localhost:8000/api/v1",
    fetchImpl: createMockFetch(store),
  });
  return { store, client };
}

const grant = (
  client: ReturnType<typeof setup>["client"],
  body: Record<string, unknown>,
) =>
  client.api.post(endpoints.resourcePermissions.list(KIND, BOARD), body, {
    headers: { "Idempotency-Key": crypto.randomUUID() },
  });

describe("Board sharing in the mock world (WorkManagement.Boards.GetSharing)", () => {
  it("lists only the permissions given on the Board, in the mockup's order", () => {
    const { store } = setup();
    const sharing = boardSharing(store, BOARD, true)!;
    expect(sharing.entriesDisclosed).toBe(true);
    expect(sharing.visibility).toBe("Workspace");
    expect(sharing.entries!.map((entry) => entry.name)).toEqual([
      "Lê Hoàng Phúc",
      "Nhóm Kế toán tổng hợp",
      "Đỗ Khánh Linh",
      "Đặng Minh Tuấn",
      "Nguyễn Thu Hà",
    ]);
    const [phuc, team, linh, tuan, ha] = sharing.entries!;
    expect(phuc).toMatchObject({
      owner: true,
      spaceRole: "Admin",
      level: "manager",
      source: "member",
      memberRole: "Owner",
      permissionId: null,
    });
    expect(team).toMatchObject({
      kind: "team",
      teamPeople: 6,
      level: "viewer",
      source: "grant",
    });
    expect(linh).toMatchObject({
      kind: "person",
      outsideSpace: true,
      level: "commenter",
    });
    expect(tuan).toMatchObject({
      kind: "guest",
      level: "viewer",
      levelCapped: false,
      email: null,
    });
    expect(ha).toMatchObject({ you: true, memberRole: "Admin" });
  });

  it("shows the email to a workspace Owner or Admin, never of a Guest", () => {
    const { store } = setup();
    const entries = boardSharing(store, BOARD, true)!.entries!;
    expect(entries.find((entry) => entry.name === "Lê Hoàng Phúc")!.email).toBe(
      "phuc.le@minhan.vn",
    );
    expect(entries.find((entry) => entry.kind === "guest")!.email).toBeNull();
  });

  it("logs only the kinds the producer has", () => {
    const { store } = setup();
    const kinds = boardSharing(store, BOARD, true)!.log!.map(
      (item) => item.kind,
    );
    expect(kinds).toEqual(["added", "changed", "visibility"]);
  });

  it("counts who loses the Board when it goes Private", () => {
    const { store } = setup();
    const { privateImpact } = boardSharing(store, BOARD, true)!;
    expect(privateImpact!.lose).toBe(22);
    expect(privateImpact!.people!.map((person) => person.name)).toEqual([
      "Trần Minh Quân",
      "Phan Ngọc Anh",
      "Vũ Thanh",
    ]);
  });

  it("gives a reader who is not a member the counts only", () => {
    const { store } = setup();
    const sharing = boardSharing(store, mockIds.boards.erp, false)!;
    // The seeded caller is a Board member, so they still see the entries.
    expect(sharing.entriesDisclosed).toBe(true);
    expect(sharing.log).toEqual([]);
    expect(sharing.privateImpact).toBeUndefined();
    const entry = store.sharing
      .entriesOf(BOARD)
      .find((item) => item.userId === store.getCurrentUser().id)!;
    entry.memberRole = null;
    entry.grantLevel = "viewer";
    const counts = boardSharing(store, BOARD, false)!;
    expect(counts).toMatchObject({ entriesDisclosed: false, entries: [] });
    expect(counts.peopleCount).toBeGreaterThan(0);
    expect(counts.guestCount).toBe(1);
  });

  it("changes a grant's level with Grant and logs the change", async () => {
    const { client, store } = setup();
    await grant(client, {
      subjectType: "Team",
      subjectId: "mock-team-erp-accounting",
      level: "editor",
    });
    const after = boardSharing(store, BOARD, true)!;
    expect(after.entries![1]!.level).toBe("editor");
    expect(after.log![0]).toMatchObject({
      kind: "changed",
      from: "viewer",
      to: "editor",
    });
  });

  it("caps a Guest at Viewer and shows it, instead of refusing a higher level", async () => {
    const { client, store } = setup();
    await grant(client, {
      subjectType: "User",
      subjectId: "mock-user-erp-tuan",
      level: "manager",
    });
    const guest = boardSharing(store, BOARD, true)!.entries!.find(
      (entry) => entry.kind === "guest",
    )!;
    expect(guest).toMatchObject({ level: "viewer", levelCapped: true });
  });

  it("changes a member's role and removes a member with the member operations", async () => {
    const { client, store } = setup();
    await client.api.post(endpoints.boards.sharingPrivate(BOARD), {
      addUserIds: ["mock-user-erp-quan"],
    });
    const quan = () =>
      boardSharing(store, BOARD, true)!.entries!.find(
        (entry) => entry.id === "mock-user-erp-quan",
      );
    expect(quan()).toMatchObject({
      source: "member",
      memberRole: "Member",
      level: "editor",
    });
    await client.api.patch(
      endpoints.boards.memberRole(BOARD, "mock-user-erp-quan"),
      { role: "Observer" },
    );
    expect(quan()).toMatchObject({ level: "viewer", memberRole: "Observer" });
    await client.api.delete(
      endpoints.boards.member(BOARD, "mock-user-erp-quan"),
    );
    expect(quan()).toBeUndefined();
    await expect(
      client.api.delete(endpoints.boards.member(BOARD, "mock-user-erp-quan")),
    ).rejects.toBeTruthy();
  });

  it("revokes a grant but keeps the owner and the caller", async () => {
    const { client, store } = setup();
    await client.api.delete(
      endpoints.resourcePermissions.detail(KIND, BOARD, "share-kl"),
    );
    expect(
      boardSharing(store, BOARD, true)!.entries!.map((e) => e.name),
    ).not.toContain("Đỗ Khánh Linh");
    await expect(
      client.api.delete(
        endpoints.resourcePermissions.detail(KIND, BOARD, "share-hp"),
      ),
    ).rejects.toBeTruthy();
    await expect(
      client.api.delete(
        endpoints.boards.member(BOARD, store.getCurrentUser().id),
      ),
    ).rejects.toBeTruthy();
  });

  it("makes the Board Private and adds the chosen people as members", async () => {
    const { client, store } = setup();
    await client.api.post(endpoints.boards.sharingPrivate(BOARD), {
      addUserIds: ["mock-user-erp-quan"],
    });
    expect(store.getBoard(BOARD)!.visibility).toBe("private");
    const sharing = boardSharing(store, BOARD, true)!;
    expect(sharing.visibility).toBe("Private");
    expect(sharing.entries!.map((entry) => entry.name)).toContain(
      "Trần Minh Quân",
    );
    expect(sharing.log![0]).toMatchObject({ kind: "private" });
    expect(sharing.log![1]).toMatchObject({ kind: "added", to: "editor" });
  });
});
