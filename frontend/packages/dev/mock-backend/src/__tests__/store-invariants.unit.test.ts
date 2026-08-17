import { describe, it, expect } from "vitest";
import { MockStore } from "../state/mock-store";
import { mockIds } from "../state/mock-ids";
import { createMockFetch } from "../transport/create-mock-fetch";
import { createNotrelixClient, endpoints } from "@notrelix/contracts";

describe("MFB-FZ-05: Store Relational Invariants and Mutations", () => {
  it("T-MFB-016: workspace creation creates workspace and owner membership atomically", async () => {
    const store = new MockStore();
    const mockFetch = createMockFetch(store);
    const client = createNotrelixClient({
      baseUrl: "http://localhost:8000/api/v1",
      fetchImpl: mockFetch,
    });

    const created = await client.api.post<{
      id: string;
      name: string;
      memberCount: number;
    }>(endpoints.workspaces.list, {
      name: "Brand New Workspace",
      slug: "brand-new-ws",
      isPersonal: false,
    });

    expect(created.name).toBe("Brand New Workspace");
    expect(created.memberCount).toBe(1);

    // Verify membership actually exists in store
    const members = store.getWorkspaceMembers(created.id);
    expect(members).toHaveLength(1);
    expect(members[0]?.userId).toBe(store.getCurrentUser().id);
    expect(members[0]?.role).toBe("owner");

    // Invariants check
    expect(() => store.assertInvariants()).not.toThrow();
  });

  it("T-MFB-017: GET /workspaces/:id with unknown ID returns 404, never first workspace", async () => {
    const store = new MockStore();
    const mockFetch = createMockFetch(store);
    const client = createNotrelixClient({
      baseUrl: "http://localhost:8000/api/v1",
      fetchImpl: mockFetch,
    });

    await expect(
      client.api.get(endpoints.workspaces.detail("non-existent-workspace-id")),
    ).rejects.toThrow();
  });

  it("T-MFB-018: invalid card move across boards fails without mutating index", () => {
    const store = new MockStore();

    // Create a second board with its own list
    const board2 = store.createBoard(mockIds.workspaces.primary, {
      title: "Board 2",
    });
    const listBoard2 = store.createList(board2.id, { title: "Board 2 List" });

    // Try moving a card from Board 1 to Board 2's list
    const cards = store.getCards("list-inprogress");
    const firstCard = cards[0]!;

    const result = store.moveCard(firstCard.id, listBoard2.id, 0);
    expect(result).toBe(false);

    // Verify card is still in original list
    const cardAfter = store.getCard(firstCard.id);
    expect(cardAfter?.listId).toBe("list-inprogress");

    // Invariants check
    expect(() => store.assertInvariants()).not.toThrow();
  });

  it("T-MFB-019: secondary indexes maintain consistency across multiple mutations", () => {
    const store = new MockStore();

    // 1. Create workspace
    const { workspace } = store.createWorkspaceForCurrentUser({
      name: "Workspace Mutation Test",
    });

    // 2. Create board
    const board = store.createBoard(workspace.id, { title: "Test Board" });

    // 3. Create lists
    const list1 = store.createList(board.id, { title: "List A" });
    const list2 = store.createList(board.id, { title: "List B" });

    // 4. Create cards
    const card1 = store.createCard(board.id, list1.id, { title: "Card 1" });
    store.createCard(board.id, list1.id, { title: "Card 2" });

    // 5. Move card
    const moved = store.moveCard(card1.id, list2.id, 0);
    expect(moved).toBe(true);

    // 6. Assert indexes and relations
    expect(store.getCards(list1.id)).toHaveLength(1);
    expect(store.getCards(list2.id)).toHaveLength(1);
    expect(() => store.assertInvariants()).not.toThrow();
  });

  it("T-MFB-027: instances of MockStore are isolated and do not share mutations", () => {
    const storeA = new MockStore();
    const storeB = new MockStore();

    storeA.createWorkspaceForCurrentUser({ name: "Workspace Only In A" });

    expect(
      storeA.getWorkspaces().find((w) => w.name === "Workspace Only In A"),
    ).toBeDefined();
    expect(
      storeB.getWorkspaces().find((w) => w.name === "Workspace Only In A"),
    ).toBeUndefined();
  });
});
