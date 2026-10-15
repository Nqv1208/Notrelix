import { describe, expect, it } from "vitest";
import { defaultConfig } from "../config/mock-config";
import { commandSearch, foldText } from "../state/mock-command-search";
import { mockIds } from "../state/mock-ids";
import { MockStore } from "../state/mock-store";

function search(
  query: string,
  canRead: (boardId: string) => boolean = () => true,
) {
  const store = new MockStore({ ...defaultConfig });
  return commandSearch(store, mockIds.workspaces.primary, query, canRead);
}

describe("command search in the mock world (CTR-GAP-WS-COMMAND-SEARCH)", () => {
  it("folds diacritics and đ", () => {
    expect(foldText("Đối chiếu HOÁ đơn")).toBe("doi chieu hoa don");
  });

  it("finds the design's seven results for a reconciliation", () => {
    const found = search("đối chiếu");
    expect(found.work.map((item) => item.key)).toEqual([
      "KT-88",
      "KT-91",
      "ERP-142",
    ]);
    expect(found.work[2]!.match.kind).toBe("child");
    expect(found.work[0]!.mine).toBe(true);
    expect(found.docs).toHaveLength(2);
    expect(found.docs.some((doc) => doc.match.kind === "content")).toBe(true);
    expect(found.boards.map((board) => board.name)).toEqual([
      "Đóng sổ tháng 10",
    ]);
    expect(found.boards[0]!.matchedItems).toBe(4);
  });

  it("matches without diacritics as it does with them", () => {
    expect(search("hoa don")).toEqual(search("hoá đơn"));
    expect(search("hoa don").docs.length).toBeGreaterThan(0);
  });

  it("returns nothing for a short or unknown text", () => {
    expect(search("a")).toEqual({ work: [], docs: [], boards: [] });
    const none = search("hợp đồng bảo trì thang máy");
    expect(none.work.length + none.docs.length + none.boards.length).toBe(0);
  });

  it("leaves out the Boards the member cannot read", () => {
    const found = search("đối chiếu", () => false);
    expect(found.work).toEqual([]);
    expect(found.boards).toEqual([]);
  });
});
