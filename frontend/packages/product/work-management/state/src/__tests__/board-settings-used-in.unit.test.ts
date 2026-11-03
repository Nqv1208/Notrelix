import { describe, expect, it } from "vitest";
import type { SavedBoardView } from "@notrelix/work-management-core";
import { usedInOf } from "../api/board-settings.api";
import { defaultTableViewConfig } from "../api/board.api";

const view = (
  name: string,
  viewMode: SavedBoardView["viewMode"],
  config: Partial<SavedBoardView["viewConfig"]>,
): SavedBoardView => ({
  id: name,
  name,
  viewMode,
  viewConfig: { ...defaultTableViewConfig, ...config },
  isDefault: false,
  visibility: "board",
  createdBy: null,
  version: 1,
});

describe("usedInOf", () => {
  it("names each saved view that filters, shows, sorts or splits by the field, once", () => {
    const views = [
      view("Bảng", "table", {}),
      view("Ẩn cột", "table", { hiddenFields: ["f1"] }),
      view("Lọc", "table", {
        hiddenFields: ["f1"],
        filters: [{ fieldId: "f1", operator: "is", value: "x" }],
      }),
      view("Kanban", "kanban", { cardFieldIds: ["f1"] }),
      view("Chia", "kanban", { groupBy: "f1" }),
      view("Lịch", "calendar", {}),
    ];
    expect(usedInOf("f1", views)).toEqual([
      { kind: "view", name: "Bảng", how: "" },
      { kind: "filter", name: "Lọc", how: "" },
      { kind: "card", name: "Kanban", how: "" },
      { kind: "view", name: "Chia", how: "" },
    ]);
    expect(usedInOf("other", [views[1]!])).toEqual([
      { kind: "view", name: "Ẩn cột", how: "" },
    ]);
  });
});
