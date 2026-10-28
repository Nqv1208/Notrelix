/**
 * The saved filters of the mock world's boards (the producer's `SavedFilter`s):
 * a personal one is its creator's alone, a shared one ("Public") is every
 * member's of the board. Deleting is soft; a deleted filter can be restored.
 */
import type { MockStore } from "./mock-store";

export type MockSavedFilterVisibility = "Private" | "Public";

export interface MockSavedFilter {
  id: string;
  boardId: string;
  name: string;
  visibility: MockSavedFilterVisibility;
  createdBy: string;
  createdAt: string;
  /** The filter bar state as the producer stores it: an opaque JSON object. */
  criteria: string | null;
  version: number;
  deleted: boolean;
}

export class MockBoardSavedFilters {
  private readonly byId = new Map<string, MockSavedFilter>();

  constructor(private readonly store: () => MockStore) {}

  clear(): void {
    this.byId.clear();
  }

  /** The live filters `userId` sees on the board: their own and the shared ones, oldest first. */
  list(boardId: string, userId: string): MockSavedFilter[] {
    return [...this.byId.values()]
      .filter(
        (f) =>
          f.boardId === boardId &&
          !f.deleted &&
          (f.visibility === "Public" || f.createdBy === userId),
      )
      .sort((a, b) => a.createdAt.localeCompare(b.createdAt));
  }

  find(filterId: string): MockSavedFilter | undefined {
    return this.byId.get(filterId);
  }

  add(
    input: Pick<MockSavedFilter, "boardId" | "name" | "criteria" | "createdBy">,
  ): MockSavedFilter {
    const created: MockSavedFilter = {
      ...input,
      id: this.store().nextId("sfilter"),
      visibility: "Private",
      createdAt: this.store().now(),
      version: 1,
      deleted: false,
    };
    this.byId.set(created.id, created);
    return created;
  }
}
