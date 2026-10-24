import { describe, expect, it } from "vitest";
import { createNotrelixClient } from "@notrelix/contracts";
import { createMockFetch } from "../transport/create-mock-fetch";
import { MockStore } from "../state/mock-store";
import { MOCK_AUTH } from "../state/mock-auth-directory";

/** The account pages in the mock world: the Identity, Workspaces and Collaboration contracts, and the pages that still have no producer (CTR-GAP-ACC-PAGES). */
function client() {
  const store = new MockStore();
  const { api } = createNotrelixClient({
    baseUrl: "http://localhost:8000/api/v1",
    fetchImpl: createMockFetch(store),
  });
  return { store, api };
}

describe("account preferences", () => {
  it("reads defaults at version 0, saves with the version and refuses a stale or unknown value", async () => {
    const { api } = client();
    expect(await api.get("/profile/preferences")).toMatchObject({
      locale: "vi",
      theme: "system",
      phone: null,
      phoneVisibleToWorkspaceMembers: false,
      version: 0,
    });
    expect(
      await api.patch("/profile/preferences", {
        expectedVersion: 0,
        theme: "dark",
        phone: "+84 988 000 111",
      }),
    ).toMatchObject({ theme: "dark", phone: "+84988000111", version: 1 });
    // A no-op keeps the version; a stale version answers 412.
    expect(
      await api.patch("/profile/preferences", {
        expectedVersion: 1,
        theme: "dark",
      }),
    ).toMatchObject({ version: 1 });
    await expect(
      api.patch("/profile/preferences", { expectedVersion: 0, theme: "light" }),
    ).rejects.toMatchObject({ status: 412 });
    await expect(
      api.patch("/profile/preferences", {
        expectedVersion: 1,
        phone: "call me",
      }),
    ).rejects.toBeDefined();
    await expect(
      api.patch("/profile/preferences", {
        expectedVersion: 1,
        timezone: "Mars/Base",
      }),
    ).rejects.toBeDefined();
  });
});

describe("job title of a member", () => {
  it("is saved per workspace with the membership version", async () => {
    const { api } = client();
    const url = "/workspaces/mock-workspace-primary/members/me/job-title";
    const mine = (await api.get(
      "/workspaces/mock-workspace-primary/members/me",
    )) as { jobTitle: string | null; version: number };
    expect(mine.version).toBe(0);
    expect(
      await api.put(url, { jobTitle: "  Trưởng  phòng ", expectedVersion: 0 }),
    ).toMatchObject({ jobTitle: "Trưởng phòng", version: 1 });
    await expect(
      api.put(url, { jobTitle: "x", expectedVersion: 0 }),
    ).rejects.toMatchObject({ status: 412 });
  });
});

describe("security", () => {
  it("lists sessions, ends one, then all the others, and signs the device out when it ends itself", async () => {
    const { api } = client();
    const first = (await api.get("/auth/sessions")) as {
      id: string;
      status: number;
      isCurrent: boolean;
    }[];
    expect(first).toHaveLength(3);
    expect(first.filter((s) => s.isCurrent)).toHaveLength(1);
    await api.post("/auth/sessions/session-ipad/revoke", {});
    await api.post("/auth/sessions/revoke-others", {});
    const after = (await api.get("/auth/sessions")) as typeof first;
    expect(after.filter((s) => s.status === 0).map((s) => s.id)).toEqual([
      "session-macbook",
    ]);
    // The current session can be ended: the device is signed out.
    await api.post("/auth/sessions/session-macbook/revoke", {});
    await expect(api.get("/auth/me")).rejects.toMatchObject({ status: 401 });
  });

  it("pages the activity by cursor", async () => {
    const { api } = client();
    const first = (await api.get("/auth/security/activity")) as {
      items: unknown[];
      nextCursor: string | null;
    };
    expect(first.items).toHaveLength(20);
    const second = (await api.get(
      `/auth/security/activity?cursor=${first.nextCursor}`,
    )) as typeof first;
    expect(second.items).toHaveLength(5);
    expect(second.nextCursor).toBeNull();
  });

  it("turns two-factor off only with a proof and on again through the enrollment", async () => {
    const { api } = client();
    expect(await api.get("/auth/security/summary")).toMatchObject({
      mfa: { isEnabled: true, recoveryCodesRemaining: 8 },
    });
    // No proof, no change.
    await expect(
      api.post("/auth/mfa/disable", { stepUpToken: "nope" }),
    ).rejects.toMatchObject({ status: 412 });
    const requirement = (await api.post("/auth/security/step-up/requirement", {
      purpose: "DisableMfa",
    })) as { requiredFactor: number; challengeToken: string };
    expect(requirement.requiredFactor).toBe(0);
    const proof = (await api.post("/auth/security/step-up/complete-mfa", {
      purpose: "DisableMfa",
      challengeToken: requirement.challengeToken,
      code: MOCK_AUTH.otpCode,
    })) as { proofToken: string };
    await api.post("/auth/mfa/disable", { stepUpToken: proof.proofToken });
    expect(await api.get("/auth/security/summary")).toMatchObject({
      mfa: { isEnabled: false },
    });
    const enrollment = (await api.post("/auth/mfa/enrollment/start", {})) as {
      mfaMethodId: string;
    };
    await expect(
      api.post("/auth/mfa/enrollment/verify", {
        mfaMethodId: enrollment.mfaMethodId,
        code: "000000",
      }),
    ).rejects.toMatchObject({ status: 400 });
    const done = (await api.post("/auth/mfa/enrollment/verify", {
      mfaMethodId: enrollment.mfaMethodId,
      code: MOCK_AUTH.otpCode,
    })) as { recoveryCodes: string[] };
    expect(done.recoveryCodes).toHaveLength(10);
  });

  it("throttles a step-up after five wrong proofs", async () => {
    const { api } = client();
    for (let i = 0; i < 4; i += 1)
      await expect(
        api.post("/auth/security/step-up/complete-password", {
          purpose: "ChangePassword",
          password: "wrong",
        }),
      ).rejects.toMatchObject({ status: 400 });
    await expect(
      api.post("/auth/security/step-up/complete-password", {
        purpose: "ChangePassword",
        password: "wrong",
      }),
    ).rejects.toMatchObject({ status: 429 });
  });
});

describe("notification preferences", () => {
  const url = "/workspaces/mock-workspace-primary/notification-preferences";

  it("saves the cells that changed with the version they were read at and keeps the version on a no-op", async () => {
    const { api } = client();
    const first = (await api.get(url)) as {
      version: number;
      rows: { type: string; inApp: boolean; email: boolean }[];
    };
    expect(first.version).toBe(0);
    expect(first.rows.map((row) => row.type)).toEqual([
      "assigned",
      "mention",
      "approval",
      "overdue",
      "watching",
      "invitation",
      "digest",
    ]);
    const saved = (await api.patch(url, {
      expectedVersion: 0,
      rows: [{ type: "digest", email: true }],
    })) as typeof first;
    expect(saved.version).toBe(1);
    expect(saved.rows.find((row) => row.type === "digest")).toEqual({
      type: "digest",
      inApp: true,
      email: true,
    });
    const same = (await api.patch(url, {
      expectedVersion: 1,
      rows: [{ type: "digest", email: true }],
    })) as typeof first;
    expect(same.version).toBe(1);
  });

  it("refuses a stale version with 412 and an unknown type", async () => {
    const { api } = client();
    await expect(
      api.patch(url, { expectedVersion: 5, rows: [] }),
    ).rejects.toMatchObject({ status: 412 });
    await expect(
      api.patch(url, {
        expectedVersion: 0,
        rows: [{ type: "mobile-push", email: true }],
      }),
    ).rejects.toBeDefined();
  });

  it("is per workspace: a save in one does not touch another", async () => {
    const { api, store } = client();
    const other = store
      .getWorkspaces()
      .find((workspace) => workspace.id !== "mock-workspace-primary");
    if (!other) return;
    await api.patch(url, {
      expectedVersion: 0,
      rows: [{ type: "digest", email: true }],
    });
    const untouched = (await api.get(
      `/workspaces/${other.id}/notification-preferences`,
    )) as { version: number };
    expect(untouched.version).toBe(0);
  });
});

describe("workspaces of the member", () => {
  it("makes another workspace the default and leaves one the member does not own alone", async () => {
    const { api } = client();
    const list = (await api.get("/account/workspaces")) as {
      workspaces: {
        id: string;
        isDefault: boolean;
        canLeave: boolean;
        role: string;
      }[];
    };
    expect(list.workspaces).toHaveLength(3);
    expect(list.workspaces[0]).toMatchObject({
      isDefault: true,
      canLeave: false,
    });
    expect(list.workspaces[1]).toMatchObject({ role: "member" });
    expect(list.workspaces[2]).toMatchObject({ role: "guest" });
    const second = list.workspaces[1]?.id ?? "";
    const after = (await api.put("/account/default-workspace", {
      workspaceId: second,
    })) as typeof list;
    expect(after.workspaces.find((w) => w.isDefault)?.id).toBe(second);
    // The only owner cannot leave the primary workspace.
    await expect(
      api.delete(`/account/workspaces/${list.workspaces[0]?.id}`),
    ).rejects.toBeDefined();
    await api.delete(`/account/workspaces/${second}`);
    const left = (await api.get("/account/workspaces")) as typeof list;
    expect(left.workspaces).toHaveLength(2);
    expect(left.workspaces.some((w) => w.id === second)).toBe(false);
  });
});
