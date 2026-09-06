import { describe, expect, it } from "vitest";
import type { FullBoardResponse } from "@notrelix/work-management-core";
import { addOptimisticCard } from "../cache/optimistic-card";

function createFullBoard(): FullBoardResponse {
  const board = {
    id: "board-test",
    workspaceId: "workspace-test",
    title: "Test Board",
    description: "",
    background: { type: "color" as const, value: "#6161ff" },
    visibility: "workspace" as const,
    isArchived: false,
    fieldDefinitions: [],
    members: [],
    createdAt: "2026-01-01T00:00:00.000Z",
    updatedAt: "2026-01-01T00:00:00.000Z",
  };

  return {
    board,
    fieldDefinitions: [],
    groups: [
      {
        id: "group-one",
        title: "To Do",
        color: "#676879",
        position: 1,
        isCollapsed: false,
        cards: [],
      },
      {
        id: "group-two",
        title: "Working on it",
        color: "#6161ff",
        position: 2,
        isCollapsed: false,
        cards: [],
      },
    ],
  };
}

describe("Kanban create-card optimistic projection", () => {
  it("adds the card only to the scoped target group", () => {
    const before = createFullBoard();
    const next = addOptimisticCard(
      before,
      { listId: "group-two", title: "Optimistic", position: 9 },
      "optimistic-fixed",
    )!;
    expect(next.groups[1]!.cards.at(-1)?.id).toBe("optimistic-fixed");
    expect(next.groups[0]).toEqual(before.groups[0]);
    expect(next.board).toBe(before.board);
  });

  it("does not corrupt cache when the target group is absent", () => {
    const before = createFullBoard();
    expect(
      addOptimisticCard(
        before,
        { listId: "missing", title: "Ignored" },
        "optimistic-fixed",
      ),
    ).toBe(before);
  });
});
