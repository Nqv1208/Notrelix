import { describe, expect, it } from "vitest";
import { createNotrelixClient } from "@notrelix/contracts";
import { createMockFetch } from "../transport/create-mock-fetch";
import { MockStore } from "../state/mock-store";
import { MOCK_AUTH } from "../state/mock-auth-directory";

/** The account pages in the mock world: the Identity, Workspaces and Collaboration contracts. */
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

interface MyWorkspace {
  id: string;
  name: string;
  slug: string;
  role: string;
  memberCount: number | null;
  departmentCount: number | null;
  isDefault: boolean;
  canLeave: boolean;
}

describe("workspaces of the member", () => {
  it("lists only the shape the producer sends: a bare array with a capitalised role and no mock-only fields", async () => {
    const { api } = client();
    const list = (await api.get("/account/workspaces")) as MyWorkspace[];
    expect(Array.isArray(list)).toBe(true);
    expect(list).toHaveLength(3);
    for (const entry of list) {
      expect(Object.keys(entry).sort()).toEqual([
        "canLeave",
        "departmentCount",
        "id",
        "isDefault",
        "memberCount",
        "name",
        "role",
        "slug",
      ]);
      expect(["Owner", "Admin", "Member", "Guest"]).toContain(entry.role);
    }
    expect(list.map((entry) => entry.name)).toEqual(
      [...list.map((entry) => entry.name)].sort(),
    );
    expect(list.filter((entry) => entry.isDefault)).toHaveLength(1);
    // An owner sees the department count; the last owner cannot leave.
    expect(list[0]).toMatchObject({ role: "Owner", canLeave: false });
    expect(list[0]?.departmentCount).not.toBeNull();
  });

  it("makes another workspace the default with a 204 and refuses a workspace the member does not belong to", async () => {
    const { api } = client();
    const list = (await api.get("/account/workspaces")) as MyWorkspace[];
    const second = list.find((entry) => !entry.isDefault)?.id ?? "";
    expect(
      await api.put("/account/default-workspace", { workspaceId: second }),
    ).toBeNull();
    const after = (await api.get("/account/workspaces")) as MyWorkspace[];
    expect(after.find((entry) => entry.isDefault)?.id).toBe(second);
    await expect(
      api.put("/account/default-workspace", { workspaceId: "nope" }),
    ).rejects.toMatchObject({ status: 404 });
    await expect(
      api.put("/account/default-workspace", { workspaceId: "" }),
    ).rejects.toMatchObject({ status: 400 });
  });

  it("refuses the last owner with the dotted code and lets another owner leave, clearing a default that was left", async () => {
    const { api } = client();
    const list = (await api.get("/account/workspaces")) as MyWorkspace[];
    const sole = list.find((entry) => !entry.canLeave);
    const leavable = list.find((entry) => entry.canLeave);
    expect(sole && leavable).toBeTruthy();
    await expect(
      api.delete(`/account/workspaces/${sole?.id}`),
    ).rejects.toMatchObject({
      status: 409,
      code: "workspaces.member.last-owner-cannot-leave",
    });
    await api.put("/account/default-workspace", {
      workspaceId: leavable?.id,
    });
    expect(await api.delete(`/account/workspaces/${leavable?.id}`)).toBeNull();
    const left = (await api.get("/account/workspaces")) as MyWorkspace[];
    expect(left.some((entry) => entry.id === leavable?.id)).toBe(false);
    // Leaving twice: the member no longer belongs there.
    await expect(
      api.delete(`/account/workspaces/${leavable?.id}`),
    ).rejects.toMatchObject({ status: 404 });
  });
});

describe("profile photo", () => {
  const PNG = [0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a, 0, 0, 0, 0];
  const form = (bytes: ArrayLike<number> | Uint8Array, name = "me.png") => {
    const data = new FormData();
    data.append(
      "file",
      new File([Uint8Array.from(bytes)], name, { type: "text/plain" }),
      name,
    );
    return data;
  };

  it("sends the file as a multipart part, answers a relative versioned address and shows it everywhere the person is read", async () => {
    const { api } = client();
    const first = (await api.upload("/profile/avatar", form(PNG), {
      method: "PUT",
    })) as { avatarUrl: string };
    expect(first.avatarUrl).toMatch(/^\/api\/v1\/users\/[^/]+\/avatar\?v=.+/);
    expect(await api.get("/auth/me")).toMatchObject({
      avatarUrl: first.avatarUrl,
    });
    const second = (await api.upload("/profile/avatar", form(PNG), {
      method: "PUT",
    })) as { avatarUrl: string };
    expect(second.avatarUrl).not.toBe(first.avatarUrl);
  });

  it("recognises the bytes, not the declared type or name", async () => {
    const { api } = client();
    await expect(
      api.upload("/profile/avatar", form([0x47, 0x49, 0x46, 0x38], "me.png"), {
        method: "PUT",
      }),
    ).rejects.toMatchObject({
      status: 415,
      code: "identity.avatar.unsupported-format",
    });
    const jpeg = await api.upload(
      "/profile/avatar",
      form([0xff, 0xd8, 0xff, 0xe0], "photo.gif"),
      { method: "PUT" },
    );
    expect(jpeg).toMatchObject({ avatarUrl: expect.any(String) });
  });

  it("refuses a photo over 2,000,000 bytes and a request without the file part", async () => {
    const { api } = client();
    const big = new Uint8Array(2_000_001);
    big.set(PNG);
    await expect(
      api.upload("/profile/avatar", form(big), { method: "PUT" }),
    ).rejects.toMatchObject({
      status: 413,
      code: "identity.avatar.too-large",
    });
    await expect(
      api.upload("/profile/avatar", new FormData(), { method: "PUT" }),
    ).rejects.toMatchObject({ status: 400 });
  });

  it("removes the photo, also when there is none", async () => {
    const { api } = client();
    await api.upload("/profile/avatar", form(PNG), { method: "PUT" });
    expect(await api.delete("/profile/avatar")).toEqual({ avatarUrl: null });
    expect(await api.get("/auth/me")).toMatchObject({ avatarUrl: null });
    expect(await api.delete("/profile/avatar")).toEqual({ avatarUrl: null });
  });
});
