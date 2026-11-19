import { describe, expect, it } from "vitest";
import { createNotrelixClient } from "@notrelix/contracts";
import { defaultConfig, type MockPersona } from "../config/mock-config";
import { mockIds } from "../state/mock-ids";
import { MockStore } from "../state/mock-store";
import { createMockFetch } from "../transport/create-mock-fetch";

const workspaceId = mockIds.workspaces.primary;
const url = `/workspaces/${workspaceId}/transfer-ownership`;

function setup(persona: MockPersona = "owner") {
  const store = new MockStore({ ...defaultConfig, persona });
  const client = createNotrelixClient({
    baseUrl: "http://localhost:8000/api/v1",
    fetchImpl: createMockFetch(store),
  });
  return { store, api: client.api };
}

const roleOf = (store: MockStore, userId: string) =>
  store.getWorkspaceMembers(workspaceId).find((m) => m.userId === userId)?.role;

describe("Transfer ownership in the mock world (LD-05)", () => {
  it("makes the person Owner and the caller Admin, once the version matches", async () => {
    const { store, api } = setup();
    await api.post(url, {
      newOwnerId: mockIds.users.admin,
      expectedVersion: store.getWorkspaceVersion(workspaceId),
    });
    expect(roleOf(store, mockIds.users.admin)).toBe("owner");
    expect(roleOf(store, mockIds.users.owner)).toBe("admin");
  });

  it("refuses a stale version with 412 and changes nobody", async () => {
    const { store, api } = setup();
    await expect(
      api.post(url, {
        newOwnerId: mockIds.users.admin,
        expectedVersion: store.getWorkspaceVersion(workspaceId) + 5,
      }),
    ).rejects.toMatchObject({ status: 412 });
    expect(roleOf(store, mockIds.users.owner)).toBe("owner");
    expect(roleOf(store, mockIds.users.admin)).toBe("admin");
  });

  it("refuses a Guest, yourself and a person who is not a member", async () => {
    const { store, api } = setup();
    const expectedVersion = store.getWorkspaceVersion(workspaceId);
    await expect(
      api.post(url, { newOwnerId: mockIds.users.viewer, expectedVersion }),
    ).rejects.toMatchObject({ status: 400 });
    await expect(
      api.post(url, { newOwnerId: mockIds.users.owner, expectedVersion }),
    ).rejects.toMatchObject({ status: 400 });
    await expect(
      api.post(url, { newOwnerId: "not-a-member", expectedVersion }),
    ).rejects.toMatchObject({ status: 404 });
    expect(roleOf(store, mockIds.users.owner)).toBe("owner");
  });

  it.each<MockPersona>(["admin", "member", "viewer"])(
    "answers 403 to a %s and promotes nobody",
    async (persona) => {
      const { store, api } = setup(persona);
      await expect(
        api.post(url, {
          newOwnerId: mockIds.users.member,
          expectedVersion: store.getWorkspaceVersion(workspaceId),
        }),
      ).rejects.toMatchObject({ status: 403 });
      expect(roleOf(store, mockIds.users.member)).toBe("member");
      expect(roleOf(store, mockIds.users.owner)).toBe("owner");
    },
  );

  it("answers 404 for a workspace the caller is not in", async () => {
    const { api } = setup();
    await expect(
      api.post("/workspaces/not-my-workspace/transfer-ownership", {
        newOwnerId: mockIds.users.admin,
        expectedVersion: 1,
      }),
    ).rejects.toMatchObject({ status: 404 });
  });
});
