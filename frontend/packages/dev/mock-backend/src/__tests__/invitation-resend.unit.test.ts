import { describe, expect, it } from "vitest";
import { createNotrelixClient } from "@notrelix/contracts";
import { defaultConfig } from "../config/mock-config";
import { mockIds } from "../state/mock-ids";
import { MockStore } from "../state/mock-store";
import { createMockFetch } from "../transport/create-mock-fetch";

const workspaceId = mockIds.workspaces.primary;

function setup(persona: "owner" | "member" = "owner") {
  const store = new MockStore({ ...defaultConfig, persona });
  const client = createNotrelixClient({
    baseUrl: "http://localhost:8000/api/v1",
    fetchImpl: createMockFetch(store),
  });
  return { store, client };
}

describe("Resending a workspace invitation in the mock world", () => {
  it("keeps the invitation and waits seven more days", async () => {
    const { store, client } = setup();
    const id = store.createInvitation(
      workspaceId,
      "new@example.com",
      "member",
    )!;
    const before = store.getInvitations(workspaceId)[0]!.expiresAt;
    const result = await client.api.post<string>(
      `/workspaces/${workspaceId}/invitations/${id}/resend`,
      {},
    );
    expect(result).toBe(id);
    const after = store.getInvitations(workspaceId)[0]!;
    expect(after.id).toBe(id);
    expect(after.status).toBe("pending");
    expect(after.expiresAt >= before).toBe(true);
  });

  it("changes the role a waiting invitation offers and refuses Admin", async () => {
    const { store, client } = setup();
    const id = store.createInvitation(
      workspaceId,
      "role@example.com",
      "member",
    )!;
    const path = `/workspaces/${workspaceId}/invitations/${id}/role`;
    await client.api.patch(path, { newRole: "Guest" });
    expect(store.getInvitations(workspaceId)[0]!.role).toBe("guest");
    await expect(
      client.api.patch(path, { newRole: "Admin" }),
    ).rejects.toBeTruthy();
    expect(store.getInvitations(workspaceId)[0]!.role).toBe("guest");
    store.revokeInvitation(workspaceId, id);
    await expect(
      client.api.patch(path, { newRole: "Member" }),
    ).rejects.toBeTruthy();
  });

  it("refuses a revoked invitation, an unknown one and a caller who cannot invite", async () => {
    const { store, client } = setup();
    const id = store.createInvitation(
      workspaceId,
      "gone@example.com",
      "member",
    )!;
    store.revokeInvitation(workspaceId, id);
    await expect(
      client.api.post(
        `/workspaces/${workspaceId}/invitations/${id}/resend`,
        {},
      ),
    ).rejects.toBeTruthy();
    await expect(
      client.api.post(
        `/workspaces/${workspaceId}/invitations/no-such-invitation/resend`,
        {},
      ),
    ).rejects.toBeTruthy();

    const member = setup("member");
    const other = member.store.createInvitation(
      workspaceId,
      "x@example.com",
      "member",
    )!;
    await expect(
      member.client.api.post(
        `/workspaces/${workspaceId}/invitations/${other}/resend`,
        {},
      ),
    ).rejects.toBeTruthy();
  });
});
