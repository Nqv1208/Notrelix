import { describe, expect, it } from "vitest";
import {
  cardFixture,
  createKanbanScenario,
  FIXED_NOW,
  kanbanDefaultScenario,
  kanbanDenseScenario,
  kanbanEdgeScenario,
  kanbanEmptyScenario,
} from "../index";

describe("deterministic Work Management fixtures", () => {
  it("returns deep-equal output for the same input", () => {
    expect(createKanbanScenario({ seed: 42 })).toEqual(
      createKanbanScenario({ seed: 42 }),
    );
  });

  it("applies typed overrides without changing unrelated defaults", () => {
    const card = cardFixture({ title: "Changed" });
    expect(card.title).toBe("Changed");
    expect(card.id).toBe("card-test");
    expect(card.createdAt).toBe(FIXED_NOW);
  });

  it("uses stable unique IDs and correct group references", () => {
    const scenario = kanbanDefaultScenario();
    const cards = scenario.columns.flatMap((column) => column.cards);
    expect(new Set(cards.map((card) => card.id)).size).toBe(cards.length);
    for (const column of scenario.columns) {
      expect(column.cards.every((card) => card.listId === column.id)).toBe(
        true,
      );
    }
  });

  it("honors the locked scenario cardinalities", () => {
    expect(
      kanbanDefaultScenario().columns.map((column) => column.cards.length),
    ).toEqual([4, 4, 4]);
    expect(kanbanEmptyScenario().columns).toHaveLength(0);
    expect(
      kanbanEdgeScenario().columns.flatMap((column) => column.cards),
    ).toHaveLength(12);
    expect(
      kanbanDenseScenario().columns.flatMap((column) => column.cards),
    ).toHaveLength(320);
  });

  it("applies deterministic combined edge overlays", () => {
    const cards = kanbanEdgeScenario().columns.flatMap(
      (column) => column.cards,
    );
    expect(cards[0]?.title).toContain("deliberately long");
    expect(cards[1]?.title).toContain("日本語");
    expect(cards[2]?.dueDate).toBeUndefined();
  });
});
