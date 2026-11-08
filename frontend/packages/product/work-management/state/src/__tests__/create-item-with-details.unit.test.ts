import { describe, expect, it, vi } from "vitest";
import {
  runCreateItemWithDetails,
  writeItemDetails,
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
  const itemExtras = {
    addLink: vi.fn(async () => undefined),
    createChildItem: vi.fn(
      async () =>
        ({ id: "child-1", version: 1 }) as
          { id: string; version: number } | undefined,
    ),
  };
  return { cards, labels, itemExtras };
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
    expect(result).toMatchObject({ itemId: "item-1", failed: [] });
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
    expect(result).toMatchObject({
      itemId: "item-1",
      failed: ["status", "assignees"],
      remaining: { statusOptionId: "s-doing", assigneeIds: ["u-1"] },
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
    expect(result).toMatchObject({ itemId: undefined, failed: [] });
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

  it("creates a sub-item under its parent with the same facts and no top-level create", async () => {
    const w = writers();
    const result = await runCreateItemWithDetails(w as never, "b-1", {
      listId: "g-parent",
      parentItemId: "parent-1",
      title: "Child",
      descriptionMd: "Scope",
      dueDate: "2026-11-09T00:00:00.000Z",
      statusOptionId: "s-doing",
    });
    expect(result).toMatchObject({ itemId: "child-1", failed: [] });
    expect(w.itemExtras.createChildItem).toHaveBeenCalledWith(
      "parent-1",
      {
        title: "Child",
        descriptionMd: "Scope",
        startDate: undefined,
        dueDate: "2026-11-09T00:00:00.000Z",
      },
      { idempotencyKey: expect.any(String) },
    );
    expect(w.cards.createCard).not.toHaveBeenCalled();
    expect(w.cards.updateStatus).toHaveBeenCalledWith("child-1", "s-doing", 1);
  });

  it("retries only the missing part on the existing item and never creates a second one", async () => {
    const updateStatus = vi
      .fn()
      .mockRejectedValueOnce(new Error("network"))
      .mockResolvedValueOnce({ version: 3 });
    const w = writers({ updateStatus });
    const first = await runCreateItemWithDetails(w as never, "b-1", {
      listId: "g-1",
      title: "Plan",
      statusOptionId: "s-doing",
      labelIds: ["l-1"],
    });
    expect(first.failed).toEqual(["status"]);
    expect(first.remaining).toEqual({ statusOptionId: "s-doing" });

    const retry = await writeItemDetails(
      w as never,
      first.itemId ?? "",
      first.version,
      first.remaining,
    );
    expect(retry).toMatchObject({ failed: [], remaining: {} });
    expect(w.cards.createCard).toHaveBeenCalledTimes(1);
    expect(w.labels.addLabelToCard).toHaveBeenCalledTimes(1);
    expect(updateStatus).toHaveBeenLastCalledWith("item-1", "s-doing", 1);
  });

  it("keeps the list items already written out of the remainder", async () => {
    const addLabelToCard = vi
      .fn()
      .mockResolvedValueOnce(undefined)
      .mockRejectedValueOnce(new Error("timeout"));
    const w = writers();
    w.labels.addLabelToCard = addLabelToCard;
    const first = await runCreateItemWithDetails(w as never, "b-1", {
      listId: "g-1",
      title: "Plan",
      labelIds: ["l-1", "l-2", "l-3"],
    });
    expect(first.failed).toEqual(["labels"]);
    expect(first.remaining.labelIds).toEqual(["l-2", "l-3"]);
  });

  it("resumes a checklist that was created before its item write failed, without a copy", async () => {
    const createChecklistItem = vi
      .fn()
      .mockRejectedValueOnce(new Error("boom"))
      .mockResolvedValue(undefined);
    const createChecklist = vi.fn(async () => undefined);
    const getChecklists = vi
      .fn()
      .mockResolvedValueOnce([
        { id: "c-1", title: "Prep", version: 1, position: 0, items: [] },
      ])
      .mockResolvedValueOnce([
        { id: "c-1", title: "Prep", version: 2, position: 0, items: [] },
      ])
      .mockResolvedValue([
        {
          id: "c-1",
          title: "Prep",
          version: 3,
          position: 0,
          items: [{ id: "i-1", title: "One", isDone: false, position: 0 }],
        },
      ]);
    const checklists = { createChecklist, getChecklists, createChecklistItem };
    const w = { ...writers(), checklists };
    const first = await writeItemDetails(w as never, "item-1", 1, {
      checklists: [{ title: "Prep", items: ["One", "Two"] }],
    });
    expect(first.failed).toEqual(["checklists"]);
    expect(first.remaining.checklists).toEqual([
      { title: "Prep", items: ["One", "Two"] },
    ]);

    createChecklist.mockClear();
    const retry = await writeItemDetails(
      w as never,
      "item-1",
      first.version,
      first.remaining,
      { resuming: true },
    );
    expect(retry.failed).toEqual([]);
    expect(createChecklist).not.toHaveBeenCalled();
    expect(createChecklistItem).toHaveBeenLastCalledWith(
      expect.objectContaining({ checklistId: "c-1", title: "Two" }),
    );
  });
});
