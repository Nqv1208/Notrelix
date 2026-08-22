import { describe, it, expect } from "vitest";
import { MockStore, MockSeedInvariantError } from "../state/mock-store";
import { mockIds } from "../state/mock-ids";
import { createMockFetch } from "../transport/create-mock-fetch";
import { createNotrelixClient, endpoints } from "@notrelix/contracts";
import type { MockPersona } from "../config/mock-config";

describe("MFB-FZ-03: Persona and Actor-World Correctness", () => {
  const personaCases: Array<{
    persona: MockPersona;
    expectedUserId: string;
    expectedRole: string;
  }> = [
    {
      persona: "owner",
      expectedUserId: mockIds.users.owner,
      expectedRole: "owner",
    },
    {
      persona: "admin",
      expectedUserId: mockIds.users.admin,
      expectedRole: "admin",
    },
    {
      persona: "member",
      expectedUserId: mockIds.users.member,
      expectedRole: "member",
    },
    {
      persona: "viewer",
      expectedUserId: mockIds.users.viewer,
      expectedRole: "guest",
    },
  ];

  for (const { persona, expectedUserId, expectedRole } of personaCases) {
    it(`T-MFB-005..008: seeds persona "${persona}" as exact actor and membership`, async () => {
      const store = new MockStore({
        seed: 1001,
        persona,
        state: "default",
        density: "normal",
        overlays: [],
        faultProfile: {},
        latency: "instant",
      });

      // 1. Current user identity matches exact actor
      const currentUser = store.getCurrentUser();
      expect(currentUser.id).toBe(expectedUserId);

      // 2. Primary workspace membership matches exact role
      const members = store.getWorkspaceMembers(mockIds.workspaces.primary);
      const userMembership = members.find((m) => m.userId === expectedUserId);
      expect(userMembership).toBeDefined();
      expect(userMembership?.role).toBe(expectedRole);

      // 3. API profile endpoint returns exact actor
      const mockFetch = createMockFetch(store);
      const client = createNotrelixClient({
        baseUrl: "http://localhost:8000/api/v1",
        fetchImpl: mockFetch,
      });

      const profile = await client.api.get<{ id: string; email: string }>(
        endpoints.auth.profile,
      );
      expect(profile.id).toBe(expectedUserId);
    });
  }

  it("T-MFB-009: throws MockSeedInvariantError when configured persona is not in store", () => {
    const store = new MockStore();
    // Simulate corrupted store where user is missing
    (store as unknown as { usersById: Map<string, unknown> })[
      "usersById"
    ].clear();

    expect(() => store.getCurrentUser()).toThrow(MockSeedInvariantError);
  });
});
