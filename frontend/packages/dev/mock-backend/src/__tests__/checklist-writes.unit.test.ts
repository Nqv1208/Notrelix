import { describe, expect, it } from "vitest";
import { defaultConfig } from "../config/mock-config";
import { erpItemId } from "../state/mock-erp-board";
import { MockStore } from "../state/mock-store";

describe("checklist writes of the mock world", () => {
  const card = erpItemId("ERP-142");

  it("toggles an item and puts items and lists between their neighbours", () => {
    const s = new MockStore({ ...defaultConfig });
    const a = s.createChecklist(card, "A");
    const b = s.createChecklist(card, "B");
    const [i1, i2, i3] = ["x", "y", "z"].map(
      (t) => s.createChecklistItem(a.id, t).id,
    );

    expect(s.toggleChecklistItem(i1!)).toBe(true);
    expect(s.getCardChecklists(card)[0]!.items[0]!.isChecked).toBe(true);
    expect(s.toggleChecklistItem(i1!)).toBe(true);
    expect(s.getCardChecklists(card)[0]!.items[0]!.isChecked).toBe(false);

    s.moveChecklistItem(i1!, i3!, null);
    expect(s.getCardChecklists(card)[0]!.items.map((i) => i.id)).toEqual([
      i2,
      i3,
      i1,
    ]);
    s.moveChecklistItem(i3!, null, i2!);
    expect(s.getCardChecklists(card)[0]!.items.map((i) => i.id)).toEqual([
      i3,
      i2,
      i1,
    ]);

    s.moveChecklist(b.id, null, a.id);
    expect(s.getCardChecklists(card).map((c) => c.id)).toEqual([b.id, a.id]);
    expect(s.toggleChecklistItem("nope")).toBe(false);
    expect(s.moveChecklist("nope", null, null)).toBe(false);
  });
});
