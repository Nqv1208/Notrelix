import { describe, expect, it } from "vitest";
import { createNotrelixClient } from "@notrelix/contracts";
import { defaultConfig } from "../config/mock-config";
import { mockIds } from "../state/mock-ids";
import { MockStore } from "../state/mock-store";
import { createMockFetch } from "../transport/create-mock-fetch";

const workspaceId = mockIds.workspaces.primary;

function setup() {
  const store = new MockStore({ ...defaultConfig, persona: "owner" });
  const client = createNotrelixClient({
    baseUrl: "http://localhost:8000/api/v1",
    fetchImpl: createMockFetch(store),
  });
  return { store, client };
}

describe("The guest invitations setting in the mock world", () => {
  it("is off by default, as on the server", () => {
    const { store } = setup();
    expect(store.getWorkspaceSettings(workspaceId).allowGuestInvites).toBe(
      false,
    );
  });

  it("refuses a Guest invitation with the producer's 403 code and still sends a Member one", async () => {
    const { store, client } = setup();
    const before = store.getInvitations(workspaceId).length;

    await expect(
      client.api.post(`/workspaces/${workspaceId}/members`, {
        email: "guest.new@example.com",
        role: "Guest",
      }),
    ).rejects.toMatchObject({
      status: 403,
      details: { errorCode: "workspace.guest_invites_off" },
    });
    expect(store.getInvitations(workspaceId)).toHaveLength(before);

    await client.api.post(`/workspaces/${workspaceId}/members`, {
      email: "member.new@example.com",
      role: "Member",
    });
    expect(store.getInvitations(workspaceId)).toHaveLength(before + 1);
  });

  it("lets a Guest invitation through once the Owner switches the setting on", async () => {
    const { store, client } = setup();
    store.getWorkspaceSettings(workspaceId).allowGuestInvites = true;

    await client.api.post(`/workspaces/${workspaceId}/members`, {
      email: "guest.new@example.com",
      role: "Guest",
    });

    expect(
      store
        .getInvitations(workspaceId)
        .some((invitation) => invitation.email === "guest.new@example.com"),
    ).toBe(true);
  });
});
