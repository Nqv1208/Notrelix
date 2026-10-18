import { describe, expect, it } from "vitest";
import { createNotrelixClient } from "@notrelix/contracts";
import { createMockFetch } from "../transport/create-mock-fetch";
import { MockStore } from "../state/mock-store";

/** The account pages of the Account mockup in the mock world (CTR-GAP-ACC-PAGES). */
function client() {
  const store = new MockStore();
  const { api } = createNotrelixClient({
    baseUrl: "http://localhost:8000/api/v1",
    fetchImpl: createMockFetch(store),
  });
  return { store, api };
}

const KEY = { idempotencyKey: "test-key" } as const;

describe("profile details", () => {
  it("saves what the member changed and refuses what no field can hold", async () => {
    const { api } = client();
    expect(await api.get("/account/profile-details")).toMatchObject({
      phone: "0912 468 135",
      timezone: "Asia/Ho_Chi_Minh",
      avatarUrl: null,
    });
    expect(
      await api.patch("/account/profile-details", { phone: "0988 000 111" }),
    ).toMatchObject({ phone: "0988 000 111" });
    await expect(
      api.patch("/account/profile-details", { phone: "call me" }),
    ).rejects.toBeDefined();
    await expect(
      api.patch("/account/profile-details", { timezone: "Mars/Base" }),
    ).rejects.toBeDefined();
  });
});

describe("security", () => {
  it("ends one device, then all the others, and never the one that asks", async () => {
    const { api } = client();
    const first = (await api.get("/account/security-details")) as {
      sessions: { id: string }[];
    };
    expect(first.sessions).toHaveLength(3);
    await expect(
      api.delete("/account/security/sessions/session-macbook"),
    ).rejects.toBeDefined();
    await api.delete("/account/security/sessions/session-ipad");
    await api.post("/account/security/sessions/revoke-others", {}, KEY);
    const last = (await api.get("/account/security-details")) as {
      sessions: { id: string }[];
    };
    expect(last.sessions.map((session) => session.id)).toEqual([
      "session-macbook",
    ]);
  });

  it("turns two-factor off and on, with fresh backup codes only while it is on", async () => {
    const { api } = client();
    await api.put("/account/security/two-factor", { enabled: false });
    await expect(
      api.post("/account/security/backup-codes", {}, KEY),
    ).rejects.toBeDefined();
    expect(
      await api.put("/account/security/two-factor", { enabled: true }),
    ).toMatchObject({ twoFactorEnabled: true, backupCodesLeft: 10 });
  });
});

describe("notification matrix", () => {
  it("keeps the saved cells and quiet hours and refuses an incomplete matrix", async () => {
    const { api } = client();
    const current = (await api.get("/account/notification-matrix")) as {
      rows: Record<string, { app: boolean; email: boolean; mobile: boolean }>;
      quietHours: { enabled: boolean; from: string; to: string; days: string };
    };
    const next = {
      rows: {
        ...current.rows,
        digest: { app: true, email: true, mobile: true },
      },
      quietHours: { ...current.quietHours, from: "22:00" },
    };
    await api.put("/account/notification-matrix", next);
    expect(await api.get("/account/notification-matrix")).toEqual(next);
    await expect(
      api.put("/account/notification-matrix", { rows: {}, quietHours: {} }),
    ).rejects.toBeDefined();
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
