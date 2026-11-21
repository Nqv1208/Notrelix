import { describe, expect, it } from "vitest";
import { createNotrelixClient } from "@notrelix/contracts";
import { defaultConfig } from "../config/mock-config";
import { mockIds } from "../state/mock-ids";
import { MockStore } from "../state/mock-store";
import { createMockFetch } from "../transport/create-mock-fetch";

const workspaceId = mockIds.workspaces.primary;

function setup(state: "default" | "account-suspended" | "account-closed") {
  const store = new MockStore({ ...defaultConfig, persona: "owner", state });
  const client = createNotrelixClient({
    baseUrl: "http://localhost:8000/api/v1",
    fetchImpl: createMockFetch(store),
  });
  return { store, client };
}

const workspace = (client: ReturnType<typeof setup>["client"]) =>
  client.api.get<{ accountStatus?: string }>(`/workspaces/${workspaceId}`);

describe("A suspended Account in the mock world (D06)", () => {
  it("reads Active on the workspace by default", async () => {
    const { client } = setup("default");
    expect((await workspace(client)).accountStatus).toBe("Active");
  });

  it("reports Suspended or Closed on the workspace read and still serves reads", async () => {
    const suspended = setup("account-suspended");
    expect((await workspace(suspended.client)).accountStatus).toBe("Suspended");
    await expect(
      suspended.client.api.get(`/workspaces/${workspaceId}/members`),
    ).resolves.toBeDefined();
    const closed = setup("account-closed");
    expect((await workspace(closed.client)).accountStatus).toBe("Closed");
  });

  it("refuses a workspace write and a board write with 403 account.suspended, changing nothing", async () => {
    const { store, client } = setup("account-suspended");
    const invitations = store.getInvitations(workspaceId).length;
    await expect(
      client.api.post(`/workspaces/${workspaceId}/members`, {
        email: "new.person@example.com",
        role: "Member",
      }),
    ).rejects.toMatchObject({
      status: 403,
      code: "account.suspended",
      details: { errorCode: "account.suspended" },
    });
    expect(store.getInvitations(workspaceId)).toHaveLength(invitations);

    const board = store.getBoards(workspaceId)[0]!;
    await expect(
      client.api.post(`/boards/${board.id}/groups`, { name: "Late" }),
    ).rejects.toMatchObject({ status: 403, code: "account.suspended" });
  });

  it("can be set on one workspace, and a restored Account writes again", async () => {
    const { store, client } = setup("default");
    store.setAccountStatus(workspaceId, "Closed");
    await expect(
      client.api.post(`/workspaces/${workspaceId}/members`, {
        email: "a.b@example.com",
        role: "Member",
      }),
    ).rejects.toMatchObject({ status: 403, code: "account.suspended" });
    store.setAccountStatus(workspaceId, "Active");
    await expect(
      client.api.post(`/workspaces/${workspaceId}/members`, {
        email: "a.b@example.com",
        role: "Member",
      }),
    ).resolves.toBeDefined();
  });
});
