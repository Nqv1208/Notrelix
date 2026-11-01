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

describe("Inviting a person who is already an Active member in the mock world", () => {
  it("answers 409 with the producer's code and creates no invitation", async () => {
    const { store, client } = setup();
    const member = store
      .getWorkspaceMembers(workspaceId)
      .find(
        (candidate) =>
          candidate.role === "admin" || candidate.role === "member",
      )!;
    const email = store.getUser(member.userId)!.email;
    const before = store.getInvitations(workspaceId).length;

    await expect(
      client.api.post(`/workspaces/${workspaceId}/members`, {
        email: ` ${email.toUpperCase()} `,
        role: "Member",
      }),
    ).rejects.toMatchObject({
      status: 409,
      details: { errorCode: "workspaces.invitation.already-member" },
    });
    expect(store.getInvitations(workspaceId)).toHaveLength(before);
  });

  it("still invites someone who is not a member", async () => {
    const { client } = setup();
    await client.api.post(`/workspaces/${workspaceId}/members`, {
      email: "someone.new@example.com",
      role: "Member",
    });
  });
});
