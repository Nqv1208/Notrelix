import { describe, expect, it } from "vitest";
import { defaultConfig } from "../config/mock-config";
import { mockIds } from "../state/mock-ids";
import { erpItemId } from "../state/mock-erp-board";
import { MockStore } from "../state/mock-store";

const store = () => new MockStore({ ...defaultConfig });

describe("item keys resolve on the board that issued them (UC-WM-A10)", () => {
  it("a current key resolves to its own item and is not moved", () => {
    const found = store().resolveItemKey(mockIds.boards.erp, "ERP-154");
    expect(found?.card.id).toBe(erpItemId("ERP-154"));
    expect(found?.moved).toBe(false);
  });

  it("an old key resolves to the item on the board it moved to", () => {
    const s = store();
    const id = erpItemId("ERP-154");
    expect(s.moveItemToBoard(id, mockIds.boards.accounting).result).toBe("ok");
    const found = s.resolveItemKey(mockIds.boards.erp, "ERP-154");
    expect(found?.moved).toBe(true);
    expect(found?.card.id).toBe(id);
    expect(found?.card.boardId).toBe(mockIds.boards.accounting);
  });

  it("a key the board never issued resolves to nothing", () => {
    expect(
      store().resolveItemKey(mockIds.boards.erp, "ERP-9999"),
    ).toBeUndefined();
  });
});
