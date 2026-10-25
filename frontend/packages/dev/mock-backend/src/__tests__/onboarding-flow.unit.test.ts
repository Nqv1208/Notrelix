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
const ACCOUNT = "account-x";
const slugCheck = (slug: string) =>
  `/accounts/${ACCOUNT}/workspaces/slug-check?slug=${slug}`;
const workspaces = `/accounts/${ACCOUNT}/workspaces`;

describe("workspace address", () => {
  it("is free until a workspace has it, then taken with a suggestion close by", async () => {
    const { api } = client();
    expect(await api.get(slugCheck("saomai"))).toMatchObject({
      status: "free",
    });
    await api.post(
      workspaces,
      {
        name: "Công ty CP Thực phẩm Sao Mai",
        slug: "saomai",
        size: "51-200",
        language: "vi",
      },
      KEY,
    );
    expect(await api.get(slugCheck("saomai"))).toEqual({
      status: "taken",
      reason: "taken",
      suggestion: "saomai-food",
    });
    expect(await api.get(slugCheck("Sao%20Mai!"))).toMatchObject({
      status: "invalid",
      reason: "invalid",
    });
    await expect(
      api.post(
        workspaces,
        { name: "Sao Mai 2", slug: "saomai", size: "1-10", language: "vi" },
        KEY,
      ),
    ).rejects.toMatchObject({ status: 409 });
  });

  it("refuses the reserved words and a short or long address", async () => {
    const { api } = client();
    for (const slug of ["admin", "workspaces", "ab", "a".repeat(64)])
      expect(await api.get(slugCheck(slug))).toMatchObject({
        status: "invalid",
        reason: "invalid",
      });
    expect(await api.get(slugCheck("a".repeat(63)))).toMatchObject({
      status: "free",
    });
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
  const create = async (
    api: ReturnType<typeof client>["api"],
    slug: string,
  ) => ({
    id: await api.post<string>(
      workspaces,
      {
        name: "Công ty CP Thực phẩm Sao Mai",
        slug,
        size: "51-200",
        language: "vi",
      },
      KEY,
    ),
  });

  it("creates private departments, invitations with their entries and the first board, with the producer's operations", async () => {
    const { api, store } = client();
    const made = await create(api, "saomai");
    const space = (name: string) =>
      api.post<string>(
        `/workspaces/${made.id}/spaces`,
        { name, visibility: "Private", spaceType: "Department" },
        KEY,
      );
    const sales = await space("Kinh doanh");
    const factory = await space("Sản xuất");
    expect(
      store.spaces
        .spacesOf(made.id)
        .map((entry) => [entry.name, entry.visibility, entry.type]),
    ).toEqual([
      ["Kinh doanh", "Private", "Department"],
      ["Sản xuất", "Private", "Department"],
    ]);

    const invite = (email: string, targetId: string | null, role: string) =>
      api.post(
        `/workspaces/${made.id}/members`,
        {
          email,
          role: "Member",
          initialEntries: targetId ? [{ kind: "Space", targetId, role }] : [],
        },
        KEY,
      );
    await invite("khoa@saomaifood.vn", factory, "Admin");
    await invite("lan@saomaifood.vn", sales, "Member");
    // The same address twice is refused the second time: the page counts it as skipped.
    await expect(
      invite("khoa@saomaifood.vn", null, "Viewer"),
    ).rejects.toMatchObject({ status: 422 });

    const boardId = await api.post<string>(
      `/workspaces/${made.id}/boards`,
      { title: "Ra mắt nước chấm Sao Mai", spaceId: factory },
      KEY,
    );
    expect(store.getBoard(boardId)?.title).toBe("Ra mắt nước chấm Sao Mai");
    expect(store.getBoard(boardId)?.spaceId).toBe(factory);
  });

  it("derives the getting-started card and lets a member hide it", async () => {
    const { api } = client();
    const made = await create(api, "saomai2");
    const card = () =>
      api.get<{
        visible: boolean;
        dismissed: boolean;
        items: { key: string; done: boolean }[];
      }>(`/workspaces/${made.id}/getting-started`);
    expect(await card()).toMatchObject({
      visible: true,
      items: [
        { key: "first-item", done: false },
        { key: "invite-more", done: false },
      ],
    });
    await api.post(
      `/workspaces/${made.id}/members`,
      { email: "khoa@saomaifood.vn", role: "Member" },
      KEY,
    );
    expect((await card()).items[1]).toMatchObject({
      key: "invite-more",
      done: true,
    });
    await api.post(`/workspaces/${made.id}/getting-started/dismiss`, {}, KEY);
    expect(await card()).toMatchObject({ visible: false, dismissed: true });
  });

  it("keeps a draft per person and clears it", async () => {
    const { api } = client();
    expect(await api.get("/workspaces/onboarding-draft")).toEqual({
      draft: null,
    });
    await expect(
      api.put("/workspaces/onboarding-draft", { workspaceId: null, data: {} }),
    ).rejects.toMatchObject({ status: 422 });
    await api.put("/workspaces/onboarding-draft", {
      step: "departments",
      workspaceId: "w1",
      data: { draft: { workspace: { name: "A" } } },
    });
    expect(await api.get("/workspaces/onboarding-draft")).toMatchObject({
      draft: { step: "departments", workspaceId: "w1" },
    });
    await api.delete("/workspaces/onboarding-draft");
    expect(await api.get("/workspaces/onboarding-draft")).toEqual({
      draft: null,
    });
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
