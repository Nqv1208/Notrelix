import { describe, expect, it, vi } from "vitest";
import {
  runCreateItemWithDetails,
  type ItemDetailWriters,
} from "../mutations/use-create-item-with-details";

function writers(overrides: Partial<ItemDetailWriters["cards"]> = {}) {
  const cards = {
    // Each write answers the version the next one names.
    createCard: vi.fn(
      async () =>
        ({ id: "item-1", version: 1 }) as
          { id: string; version: number } | undefined,
    ),
    updateStatus: vi.fn(async () => ({ version: 2 })),
    updateFieldValue: vi.fn(async () => ({ version: 3 })),
    setAssignees: vi.fn(async () => 4),
    ...overrides,
  };
  const labels = { addLabelToCard: vi.fn(async () => undefined) };
  return { cards, labels };
}

describe("creating an item with the dialog's details", () => {
  it("sends the item's own facts with the create and the other facts as their own writes", async () => {
    const w = writers();
    const result = await runCreateItemWithDetails(w as never, "b-1", {
      listId: "g-1",
      title: "Plan",
      descriptionMd: "Why",
      startDate: "2026-11-02T00:00:00.000Z",
      dueDate: "2026-11-09T00:00:00.000Z",
      statusOptionId: "s-doing",
      priority: { fieldDefinitionId: "f-prio", value: "p-high" },
      assigneeIds: ["u-1", "u-2"],
      labelIds: ["l-1", "l-2"],
    });
    expect(result).toEqual({ itemId: "item-1", failed: [] });
    expect(w.cards.createCard).toHaveBeenCalledWith(
      "b-1",
      {
        listId: "g-1",
        title: "Plan",
        descriptionMd: "Why",
        startDate: "2026-11-02T00:00:00.000Z",
        dueDate: "2026-11-09T00:00:00.000Z",
      },
      expect.objectContaining({ idempotencyKey: expect.any(String) }),
    );
    expect(w.cards.updateStatus).toHaveBeenCalledWith("item-1", "s-doing", 1);
    expect(w.cards.updateFieldValue).toHaveBeenCalledWith(
      {
        cardId: "item-1",
        fieldDefinitionId: "f-prio",
        value: "p-high",
      },
      { expectedVersion: 2 },
    );
    expect(w.cards.setAssignees).toHaveBeenCalledWith(
      "item-1",
      ["u-1", "u-2"],
      [],
      3,
    );
    expect(w.labels.addLabelToCard).toHaveBeenCalledTimes(2);
  });

  it("keeps the item and names the details that failed", async () => {
    const w = writers({
      updateStatus: vi.fn(async () => {
        throw new Error("conflict");
      }),
      setAssignees: vi.fn(async () => {
        throw new Error("forbidden");
      }),
    });
    const result = await runCreateItemWithDetails(w as never, "b-1", {
      listId: "g-1",
      title: "Plan",
      statusOptionId: "s-doing",
      assigneeIds: ["u-1"],
      labelIds: ["l-1"],
    });
    expect(result).toEqual({
      itemId: "item-1",
      failed: ["status", "assignees"],
    });
    expect(w.labels.addLabelToCard).toHaveBeenCalledWith("item-1", "l-1");
  });

  it("does not write details when the server returned no id, and rethrows a failed create", async () => {
    const none = writers({ createCard: vi.fn(async () => undefined) });
    const result = await runCreateItemWithDetails(none as never, "b-1", {
      listId: "g-1",
      title: "Plan",
      statusOptionId: "s-doing",
    });
    expect(result).toEqual({ itemId: undefined, failed: [] });
    expect(none.cards.updateStatus).not.toHaveBeenCalled();

    const broken = writers({
      createCard: vi.fn(async () => {
        throw new Error("403");
      }),
    });
    await expect(
      runCreateItemWithDetails(broken as never, "b-1", {
        listId: "g-1",
        title: "Plan",
        statusOptionId: "s-doing",
      }),
    ).rejects.toThrow("403");
    expect(broken.cards.updateStatus).not.toHaveBeenCalled();
  });
});
