import { describe, expect, it } from "vitest";
import { createNotrelixClient } from "@notrelix/contracts";
import { createMockFetch } from "../transport/create-mock-fetch";
import { MockStore } from "../state/mock-store";
import {
  departmentSuggestions,
  isSlugTaken,
  suggestSlug,
} from "../state/mock-onboarding";

/** Onboarding of the Onboarding mockup in the mock world (CTR-GAP-ONBOARD-FLOW). */
function client() {
  const store = new MockStore();
  const { api } = createNotrelixClient({
    baseUrl: "http://localhost:8000/api/v1",
    fetchImpl: createMockFetch(store),
  });
  return { store, api };
}

const KEY = { idempotencyKey: "test-key" } as const;

describe("workspace address", () => {
  it("is free until a workspace has it, then taken with a suggestion close by", async () => {
    const { api } = client();
    expect(await api.get("/onboarding/slug-check?slug=saomai")).toMatchObject({
      available: true,
    });
    await api.post(
      "/onboarding/workspace",
      {
        name: "Công ty CP Thực phẩm Sao Mai",
        slug: "saomai",
        size: "l",
        locale: "vi",
      },
      KEY,
    );
    expect(await api.get("/onboarding/slug-check?slug=saomai")).toEqual({
      available: false,
      reason: "taken",
      suggestion: "saomai-food",
    });
    expect(
      await api.get("/onboarding/slug-check?slug=Sao%20Mai!"),
    ).toMatchObject({
      available: false,
      reason: "invalid",
    });
    await expect(
      api.post(
        "/onboarding/workspace",
        { name: "Sao Mai 2", slug: "saomai", size: "s", locale: "vi" },
        KEY,
      ),
    ).rejects.toMatchObject({ status: 409 });
  });

  it("keeps reserved words and the mock world's own addresses taken", () => {
    expect(isSlugTaken("notrelix", [])).toBe(true);
    expect(isSlugTaken("minhan", [])).toBe(true);
    expect(isSlugTaken("congty", ["congty"])).toBe(true);
    expect(suggestSlug("congty", (slug) => slug === "congty-vn")).toBe(
      "congty-co",
    );
  });
});

describe("suggestions", () => {
  it("follow the size and the line of business read from the name", () => {
    expect(
      departmentSuggestions(
        "l",
        "Công ty CP Thực phẩm Sao Mai",
      ).departments.map((entry) => entry.name),
    ).toEqual(["Kinh doanh", "Kế toán", "Sản xuất", "Nhân sự"]);
    expect(
      departmentSuggestions("l", "Công ty CP Thực phẩm Sao Mai"),
    ).toMatchObject({
      industry: "food",
    });
    expect(departmentSuggestions("s", "Studio Hoa").departments).toHaveLength(
      2,
    );
    expect(departmentSuggestions("s", "Studio Hoa").industry).toBeNull();
  });
});

describe("the setup through the client", () => {
  it("creates the workspace, its private departments, invitations and first board", async () => {
    const { api, store } = client();
    const made = await api.post<{ id: string }>(
      "/onboarding/workspace",
      {
        name: "Công ty CP Thực phẩm Sao Mai",
        slug: "saomai",
        size: "l",
        locale: "vi",
      },
      KEY,
    );
    const departments = await api.post<{
      departments: { id: string; name: string; color: string }[];
    }>(
      `/workspaces/${made.id}/onboarding/departments`,
      {
        departments: [
          { name: "Kinh doanh", color: "coral" },
          { name: "Sản xuất", color: "blue" },
        ],
      },
      KEY,
    );
    expect(departments.departments.map((entry) => entry.name)).toEqual([
      "Kinh doanh",
      "Sản xuất",
    ]);
    const spaces = store.spaces.spacesOf(made.id);
    expect(
      spaces.map((space) => [space.name, space.visibility, space.color]),
    ).toEqual([
      ["Kinh doanh", "Private", "coral"],
      ["Sản xuất", "Private", "blue"],
    ]);

    const invites = await api.post<{ sent: number; skipped: string[] }>(
      `/workspaces/${made.id}/onboarding/invitations`,
      {
        invites: [
          {
            email: "khoa@saomaifood.vn",
            spaceId: departments.departments[1]?.id,
            role: "Admin",
          },
          {
            email: "lan@saomaifood.vn",
            spaceId: departments.departments[0]?.id,
            role: "Member",
          },
          { email: "khoa@saomaifood.vn", spaceId: null, role: "Viewer" },
        ],
      },
      KEY,
    );
    // The same email twice is sent once.
    expect(invites).toEqual({ sent: 2, skipped: ["khoa@saomaifood.vn"] });

    const board = await api.post<{ id: string }>(
      `/workspaces/${made.id}/onboarding/board`,
      {
        templateId: "project",
        name: "Ra mắt nước chấm Sao Mai",
        spaceId: departments.departments[1]?.id,
      },
      KEY,
    );
    expect(store.getBoard(board.id)?.title).toBe("Ra mắt nước chấm Sao Mai");
    expect(store.getBoard(board.id)?.spaceId).toBe(
      departments.departments[1]?.id,
    );

    const list = await api.get<{
      visible: boolean;
      items: { key: string; done: boolean; pendingInvites: number | null }[];
    }>(`/workspaces/${made.id}/getting-started`);
    expect(list.visible).toBe(true);
    expect(list.items.map((item) => item.key)).toEqual([
      "first-item",
      "invite-more",
      "mobile",
    ]);
    expect(list.items[1]?.pendingInvites).toBe(2);
    await api.post(
      `/workspaces/${made.id}/getting-started/mobile/done`,
      {},
      KEY,
    );
    await api.post(`/workspaces/${made.id}/getting-started/dismiss`, {}, KEY);
    expect(
      (
        await api.get<{ visible: boolean }>(
          `/workspaces/${made.id}/getting-started`,
        )
      ).visible,
    ).toBe(false);
  });

  it("refuses an unnamed department, a bad email and a missing template", async () => {
    const { api } = client();
    const made = await api.post<{ id: string }>(
      "/onboarding/workspace",
      { name: "Studio Hoa", slug: "studiohoa", size: "s", locale: "en" },
      KEY,
    );
    await expect(
      api.post(
        `/workspaces/${made.id}/onboarding/departments`,
        { departments: [{ name: " " }] },
        KEY,
      ),
    ).rejects.toMatchObject({ status: 422 });
    await expect(
      api.post(
        `/workspaces/${made.id}/onboarding/invitations`,
        { invites: [{ email: "khong-phai-email", role: "Member" }] },
        KEY,
      ),
    ).rejects.toMatchObject({ status: 422 });
    await expect(
      api.post(
        `/workspaces/${made.id}/onboarding/board`,
        { templateId: "nope", name: "" },
        KEY,
      ),
    ).rejects.toMatchObject({ status: 422 });
  });

  it("keeps a draft per person and clears it", async () => {
    const { api } = client();
    expect(await api.get("/onboarding/draft")).toEqual({ draft: null });
    await api.put("/onboarding/draft", {
      step: "departments",
      workspaceId: "w1",
      data: { draft: { workspace: { name: "A" } } },
    });
    expect(await api.get("/onboarding/draft")).toMatchObject({
      draft: { step: "departments", workspaceId: "w1" },
    });
    await api.delete("/onboarding/draft");
    expect(await api.get("/onboarding/draft")).toEqual({ draft: null });
  });
});

describe("what an invited person sees", () => {
  it("lists the departments they can open with their boards, and saves the profile", async () => {
    const { api } = client();
    const join = await api.get<{
      workspace: { name: string };
      spaces: { name: string; boards: unknown[] }[];
      profile: { displayName: string; notifyEmail: string };
    }>("/workspaces/mock-workspace-primary/onboarding/join");
    expect(join.workspace.name).toBe("Minh An Group");
    expect(join.spaces.length).toBeGreaterThan(0);
    expect(join.profile.notifyEmail).toBe("digest");
    await api.put("/workspaces/mock-workspace-primary/onboarding/join", {
      displayName: "Lý Thu Trang",
      notifyEmail: "mentions",
    });
    const again = await api.get<typeof join>(
      "/workspaces/mock-workspace-primary/onboarding/join",
    );
    expect(again.profile).toEqual({
      displayName: "Lý Thu Trang",
      notifyEmail: "mentions",
    });
    await expect(
      api.put("/workspaces/mock-workspace-primary/onboarding/join", {
        displayName: " ",
      }),
    ).rejects.toMatchObject({ status: 422 });
  });
});
