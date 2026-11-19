import { describe, expect, it } from "vitest";
import { createNotrelixClient } from "@notrelix/contracts";
import { defaultConfig, type MockPersona } from "../config/mock-config";
import { MOCK_AUTH } from "../state/mock-auth-directory";
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

type Api = ReturnType<typeof setup>["api"];

/** A single-use proof for a purpose, answered the way the account page does, with whichever factor the mock person has. */
async function proofFor(
  api: Api,
  purpose = "TransferWorkspaceOwnership",
): Promise<string> {
  const requirement = (await api.post("/auth/security/step-up/requirement", {
    purpose,
  })) as { requiredFactor: string; challengeToken: string | null };
  const proof = (await (requirement.requiredFactor === "MfaChallenge"
    ? api.post("/auth/security/step-up/complete-mfa", {
        purpose,
        challengeToken: requirement.challengeToken,
        code: MOCK_AUTH.otpCode,
      })
    : api.post("/auth/security/step-up/complete-password", {
        purpose,
        password: MOCK_AUTH.personalPassword,
      }))) as { proofToken: string };
  return proof.proofToken;
}

const roleOf = (store: MockStore, userId: string) =>
  store.getWorkspaceMembers(workspaceId).find((m) => m.userId === userId)?.role;

describe("Transfer ownership in the mock world (LD-05)", () => {
  it("makes the person Owner and the caller Admin, once the version matches", async () => {
    const { store, api } = setup();
    await api.post(url, {
      newOwnerId: mockIds.users.admin,
      expectedVersion: store.getWorkspaceVersion(workspaceId),
      stepUpToken: await proofFor(api),
    });
    expect(roleOf(store, mockIds.users.admin)).toBe("owner");
    expect(roleOf(store, mockIds.users.owner)).toBe("admin");
  });

  it("refuses a handover without a proof with 412 step-up-required and changes nobody", async () => {
    const { store, api } = setup();
    for (const stepUpToken of [undefined, ""]) {
      await expect(
        api.post(url, {
          newOwnerId: mockIds.users.admin,
          expectedVersion: store.getWorkspaceVersion(workspaceId),
          stepUpToken,
        }),
      ).rejects.toMatchObject({
        status: 412,
        code: "identity.security.step-up-required",
      });
    }
    expect(roleOf(store, mockIds.users.owner)).toBe("owner");
    expect(roleOf(store, mockIds.users.admin)).toBe("admin");
  });

  it("refuses an unknown or other-purpose proof with 400 step-up-invalid and changes nobody", async () => {
    const { store, api } = setup();
    const expectedVersion = store.getWorkspaceVersion(workspaceId);
    const otherPurpose = await proofFor(api, "RevokeOtherSessions");
    for (const stepUpToken of ["not-a-proof", otherPurpose]) {
      await expect(
        api.post(url, {
          newOwnerId: mockIds.users.admin,
          expectedVersion,
          stepUpToken,
        }),
      ).rejects.toMatchObject({
        status: 400,
        code: "identity.security.step-up-invalid",
      });
    }
    expect(roleOf(store, mockIds.users.owner)).toBe("owner");
    expect(roleOf(store, mockIds.users.admin)).toBe("admin");
  });

  it("spends the proof: one refused for its rule cannot be used again", async () => {
    const { store, api } = setup();
    const expectedVersion = store.getWorkspaceVersion(workspaceId);
    const stepUpToken = await proofFor(api);
    await expect(
      api.post(url, {
        newOwnerId: mockIds.users.viewer,
        expectedVersion,
        stepUpToken,
      }),
    ).rejects.toMatchObject({ status: 400 });
    await expect(
      api.post(url, {
        newOwnerId: mockIds.users.admin,
        expectedVersion,
        stepUpToken,
      }),
    ).rejects.toMatchObject({
      status: 400,
      code: "identity.security.step-up-invalid",
    });
    expect(roleOf(store, mockIds.users.admin)).toBe("admin");
  });

  it("refuses a stale version with 412 and changes nobody", async () => {
    const { store, api } = setup();
    await expect(
      api.post(url, {
        newOwnerId: mockIds.users.admin,
        expectedVersion: store.getWorkspaceVersion(workspaceId) + 5,
        stepUpToken: await proofFor(api),
      }),
    ).rejects.toMatchObject({ status: 412 });
    expect(roleOf(store, mockIds.users.owner)).toBe("owner");
    expect(roleOf(store, mockIds.users.admin)).toBe("admin");
  });

  it("refuses a Guest, yourself and a person who is not a member", async () => {
    const { store, api } = setup();
    const expectedVersion = store.getWorkspaceVersion(workspaceId);
    for (const [newOwnerId, status] of [
      [mockIds.users.viewer, 400],
      [mockIds.users.owner, 400],
      ["not-a-member", 404],
    ] as const) {
      await expect(
        api.post(url, {
          newOwnerId,
          expectedVersion,
          stepUpToken: await proofFor(api),
        }),
      ).rejects.toMatchObject({ status });
    }
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
