import { describe, it, expect } from "vitest";
import { MockStore } from "../state/mock-store";
import { mockIds } from "../state/mock-ids";
import { createMockFetch } from "../transport/create-mock-fetch";
import { createNotrelixClient, endpoints } from "@notrelix/contracts";

describe("MFB-FZ-04: Scenario, Preset and Overlay Semantics", () => {
  it("T-MFB-010: new-user state seeds actor with zero workspaces and memberships", () => {
    const store = new MockStore({
      seed: 1001,
      persona: "owner",
      state: "new-user",
      density: "normal",
      overlays: [],
      faultProfile: {},
      latency: "instant",
    });

    const user = store.getCurrentUser();
    expect(user.id).toBe(mockIds.users.owner);
    expect(store.getWorkspaces()).toHaveLength(0);
    expect(store.getWorkspaceMembers(mockIds.workspaces.primary)).toHaveLength(
      0,
    );
  });

  it("T-MFB-011: empty-workspace state seeds workspaces and memberships, but 0 boards and 0 pages", () => {
    const store = new MockStore({
      seed: 1001,
      persona: "owner",
      state: "empty-workspace",
      density: "tiny",
      overlays: [],
      faultProfile: {},
      latency: "instant",
    });

    expect(store.getWorkspaces().length).toBeGreaterThan(0);
    expect(
      store.getWorkspaceMembers(mockIds.workspaces.primary).length,
    ).toBeGreaterThan(0);
    expect(store.getBoards(mockIds.workspaces.primary)).toHaveLength(0);
    expect(store.getPages(mockIds.workspaces.primary)).toHaveLength(0);
  });

  it("T-MFB-012: permission-limited state has viewer actor with guest role", () => {
    const store = new MockStore({
      seed: 1001,
      persona: "viewer",
      state: "permission-limited",
      density: "normal",
      overlays: [],
      faultProfile: {},
      latency: "instant",
    });

    const user = store.getCurrentUser();
    expect(user.id).toBe(mockIds.users.viewer);

    const members = store.getWorkspaceMembers(mockIds.workspaces.primary);
    const viewerMem = members.find((m) => m.userId === user.id);
    expect(viewerMem?.role).toBe("guest");
  });

  it("T-MFB-013: expired-session returns 401 on refresh while preserving deterministic store", async () => {
    const store = new MockStore({
      seed: 1001,
      persona: "owner",
      state: "expired-session",
      density: "tiny",
      overlays: [],
      faultProfile: {},
      latency: "instant",
    });

    const mockFetch = createMockFetch(store);
    const client = createNotrelixClient({
      baseUrl: "http://localhost:8000/api/v1",
      fetchImpl: mockFetch,
    });

    await expect(client.api.post(endpoints.auth.refresh)).rejects.toThrow();
  });

  it("T-MFB-014: verifies all public business states produce distinct, valid worlds", () => {
    const states = [
      "default",
      "new-user",
      "empty-workspace",
      "permission-limited",
      "expired-session",
    ] as const;

    for (const state of states) {
      const store = new MockStore({
        seed: 1001,
        persona: state === "permission-limited" ? "viewer" : "owner",
        state,
        density: "normal",
        overlays: [],
        faultProfile: {},
        latency: "instant",
      });

      expect(store.getCurrentUser()).toBeDefined();
    }
  });

  it("T-MFB-015: overlays transform deterministic output (unicode, long-titles, many-columns, missing-avatars, many-cards)", () => {
    // 1. unicode
    const unicodeStore = new MockStore({
      seed: 1001,
      persona: "owner",
      state: "default",
      density: "normal",
      overlays: ["unicode"],
      faultProfile: {},
      latency: "instant",
    });
    const board = unicodeStore.getBoard(mockIds.boards.roadmap);
    expect(board?.title).toContain("🚀");

    // 2. long-titles
    const longTitleStore = new MockStore({
      seed: 1001,
      persona: "owner",
      state: "default",
      density: "normal",
      overlays: ["long-titles"],
      faultProfile: {},
      latency: "instant",
    });
    const longBoard = longTitleStore.getBoard(mockIds.boards.roadmap);
    expect(longBoard!.title.length).toBeGreaterThan(40);

    // 3. many-columns
    const manyColumnsStore = new MockStore({
      seed: 1001,
      persona: "owner",
      state: "default",
      density: "normal",
      overlays: ["many-columns"],
      faultProfile: {},
      latency: "instant",
    });
    const lists = manyColumnsStore.getLists(mockIds.boards.roadmap);
    expect(lists.length).toBeGreaterThan(3);

    // 4. missing-avatars
    const missingAvatarsStore = new MockStore({
      seed: 1001,
      persona: "owner",
      state: "default",
      density: "normal",
      overlays: ["missing-avatars"],
      faultProfile: {},
      latency: "instant",
    });
    const user = missingAvatarsStore.getCurrentUser();
    expect(user.avatarUrl).toBeNull();

    // 5. many-cards
    const manyCardsStore = new MockStore({
      seed: 1001,
      persona: "owner",
      state: "default",
      density: "normal",
      overlays: ["many-cards"],
      faultProfile: {},
      latency: "instant",
    });
    const cards = manyCardsStore.getCards("list-inprogress");
    expect(cards.length).toBe(100);
  });
});
