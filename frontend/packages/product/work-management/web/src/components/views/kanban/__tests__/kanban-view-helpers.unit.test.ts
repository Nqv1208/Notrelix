import { describe, expect, it } from "vitest";
import type { Card } from "@notrelix/work-management-core";
import { buildCardLink } from "../kanban-view-helpers";
import { boardCapabilities } from "../../../shared/board-capabilities";
import { isKanbanCardOverdue } from "../kanban-filter";

/** 12/10/2026 10:40 in the runtime's own time zone. */
const NOW = new Date(2026, 9, 12, 10, 40);

function card(extra: Partial<Card>): Card {
  return {
    id: "c",
    listId: "g",
    boardId: "b",
    workspaceId: "w",
    title: "t",
    position: 1,
    status: "",
    isArchived: false,
    isDeleted: false,
    members: [],
    labels: [],
    checklists: [],
    fieldValues: {},
    _count: { comments: 0, attachments: 0, checklistItems: 0 },
    createdAt: "2026-10-01T00:00:00.000Z",
    ...extra,
  };
}

describe("isKanbanCardOverdue", () => {
  it("is overdue from the day after the due day, like the card's wording", () => {
    const due = (day: number) => new Date(2026, 9, day, 17, 0).toISOString();
    expect(isKanbanCardOverdue(card({ dueAt: due(10) }), NOW)).toBe(true);
    expect(isKanbanCardOverdue(card({ dueAt: due(12) }), NOW)).toBe(false);
    expect(isKanbanCardOverdue(card({ dueDate: due(11) }), NOW)).toBe(true);
    expect(isKanbanCardOverdue(card({}), NOW)).toBe(false);
  });

  it("never counts a done item as overdue", () => {
    const due = new Date(2026, 9, 1, 17, 0).toISOString();
    expect(
      isKanbanCardOverdue(card({ dueAt: due, statusCategory: "done" }), NOW),
    ).toBe(false);
    expect(
      isKanbanCardOverdue(
        card({ dueAt: due, completedAt: "2026-10-02T00:00:00.000Z" }),
        NOW,
      ),
    ).toBe(false);
  });
});

describe("boardCapabilities", () => {
  it("gives editors and managers the full sheet and board actions", () => {
    for (const level of ["editor", "manager"] as const) {
      expect(boardCapabilities(level)).toMatchObject({
        canCreateCard: true,
        canMoveCard: true,
        canEditFields: true,
        canComment: true,
      });
    }
  });

  it("keeps only commenting for a commenter", () => {
    expect(boardCapabilities("commenter")).toEqual({
      canCreateCard: false,
      canMoveCard: false,
      canRenameGroup: false,
      canDelete: false,
      canEditFields: false,
      canComment: true,
    });
  });

  it("is read-only for a viewer and while the level is unknown", () => {
    for (const level of ["viewer", null, undefined] as const) {
      expect(boardCapabilities(level)).toEqual({
        canCreateCard: false,
        canMoveCard: false,
        canRenameGroup: false,
        canDelete: false,
        canEditFields: false,
        canComment: false,
      });
    }
  });
});

describe("buildCardLink", () => {
  const location = (search: string) => ({
    origin: "https://app.notrelix.test",
    pathname: "/workspaces/ws-1/boards/b-1",
    search,
  });

  it("keeps the item parameter and the other parameters", () => {
    const link = new URL(
      buildCardLink(location("?view=kanban&filter=p.low&item=old"), "card-1"),
    );
    expect(link.pathname).toBe("/workspaces/ws-1/boards/b-1");
    expect(link.searchParams.get("item")).toBe("card-1");
    expect(link.searchParams.get("view")).toBe("kanban");
    expect(link.searchParams.get("filter")).toBe("p.low");
  });

  it("adds view=kanban when the URL has no view", () => {
    const link = new URL(buildCardLink(location(""), "card-1"));
    expect(link.searchParams.get("view")).toBe("kanban");
    expect(link.searchParams.get("item")).toBe("card-1");
  });
});
