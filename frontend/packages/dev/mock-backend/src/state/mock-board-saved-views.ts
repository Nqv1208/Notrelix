/**
 * The saved views of the mock world's boards (the producer's `BoardView`s): a
 * shared one is every board member's, a private one is its creator's alone.
 * The board's legacy view record (`MockStore.getBoardView`) is its first
 * shared view and the default until another is chosen.
 */
import type { MockStore } from "./mock-store";

export type MockSavedViewVisibility = "Board" | "Private";

export interface MockSavedView {
  id: string;
  boardId: string;
  name: string;
  type: string;
  config: string;
  isDefault: boolean;
  visibility: MockSavedViewVisibility;
  createdBy: string | null;
  version: number;
}

const TYPE_NAME: Record<string, string> = {
  table: "Table",
  kanban: "Kanban",
  calendar: "Calendar",
  timeline: "Timeline",
};

export function savedViewTypeName(raw: string | undefined): string | null {
  return TYPE_NAME[(raw ?? "").trim().toLowerCase()] ?? null;
}

export class MockBoardSavedViews {
  private readonly byBoard = new Map<string, MockSavedView[]>();

  constructor(private readonly store: () => MockStore) {}

  clear(): void {
    this.byBoard.clear();
  }

  /** Every view of the board, the legacy record seeded as the default on first use. */
  private all(boardId: string): MockSavedView[] {
    let views = this.byBoard.get(boardId);
    if (!views) {
      views = [];
      const legacy = this.store().getBoardView(boardId);
      if (legacy) {
        views.push({
          id: `${boardId}-view-default`,
          boardId,
          name: savedViewTypeName(legacy.viewMode) ?? "Table",
          type: savedViewTypeName(legacy.viewMode) ?? "Table",
          config: legacy.viewConfig,
          isDefault: true,
          visibility: "Board",
          createdBy: null,
          version: 1,
        });
      }
      this.byBoard.set(boardId, views);
    }
    return views;
  }

  /** The views `userId` sees: the default first, then oldest first. */
  list(boardId: string, userId: string): MockSavedView[] {
    return this.all(boardId)
      .filter((v) => v.visibility === "Board" || v.createdBy === userId)
      .sort((a, b) => Number(b.isDefault) - Number(a.isDefault));
  }

  /** The default shared view (the legacy save's target). */
  defaultShared(boardId: string): MockSavedView | undefined {
    const shared = this.all(boardId).filter((v) => v.visibility === "Board");
    return shared.find((v) => v.isDefault) ?? shared[0];
  }

  find(boardId: string, viewId: string): MockSavedView | undefined {
    return this.all(boardId).find((v) => v.id === viewId);
  }

  add(
    view: Omit<MockSavedView, "id" | "isDefault" | "version">,
  ): MockSavedView {
    const views = this.all(view.boardId);
    const created: MockSavedView = {
      ...view,
      id: this.store().nextId("bview"),
      isDefault: views.length === 0 && view.visibility === "Board",
      version: 1,
    };
    views.push(created);
    return created;
  }

  remove(boardId: string, viewId: string): void {
    const views = this.all(boardId);
    const index = views.findIndex((v) => v.id === viewId);
    if (index >= 0) views.splice(index, 1);
  }

  setDefault(boardId: string, viewId: string): void {
    for (const v of this.all(boardId)) v.isDefault = v.id === viewId;
  }
}
