import { describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen } from "@testing-library/react";

import { BoardTrashSheet } from "../board-trash-sheet";
import { en } from "../item-lifecycle.copy";

const base = {
  copy: en,
  open: true,
  loading: false,
  failed: false,
  onRestore: () => undefined,
  onClose: () => undefined,
};
const row = (id: string, extra = {}) => ({
  id,
  name: `Task ${id}`,
  itemKey: `ERP-${id}`,
  note: "Archived by Hà Nguyễn · 12/10/2026 09:30",
  ...extra,
});

describe("the archive and trash list", () => {
  it("FUI[wm.item-lifecycle:list-paging] shows the count, how many are shown, and loads the next page on request", () => {
    const onLoadMore = vi.fn();
    render(
      <BoardTrashSheet
        {...base}
        rows={[row("1"), row("2", { subItemCount: 3 })]}
        total={128}
        hasMore
        onLoadMore={onLoadMore}
      />,
    );

    expect(screen.getByText("128")).toBeTruthy();
    expect(screen.getByText("Showing 2 of 128")).toBeTruthy();
    expect(screen.getByText("3 sub-items")).toBeTruthy();
    fireEvent.click(screen.getByRole("button", { name: "Show 50 more" }));
    expect(onLoadMore).toHaveBeenCalledOnce();
  });

  it("FUI[wm.item-lifecycle:list-search-empty] a search that finds nothing says so and names the words", () => {
    render(
      <BoardTrashSheet
        {...base}
        labels={{
          title: "Archived items",
          body: "",
          empty: "Nothing archived.",
          loading: "Loading",
          error: "Failed",
          action: "Unarchive",
          searchPlaceholder: "Search archived items",
        }}
        search="cold store"
        onSearch={() => undefined}
        rows={[]}
      />,
    );

    expect(screen.getByText("No results")).toBeTruthy();
    expect(screen.getByText("No item here matches “cold store”.")).toBeTruthy();
  });

  it("FUI[wm.item-lifecycle:list-error] a failed read offers Try again", () => {
    const onRetry = vi.fn();
    render(<BoardTrashSheet {...base} rows={[]} failed onRetry={onRetry} />);

    fireEvent.click(screen.getByRole("button", { name: "Try again" }));
    expect(onRetry).toHaveBeenCalledOnce();
    expect(screen.getByRole("alert").textContent).toContain(
      "The list couldn't be loaded",
    );
  });

  it("FUI[wm.item-lifecycle:list-refused] a refused restore keeps the row and says why; the busy row is disabled", () => {
    render(
      <BoardTrashSheet
        {...base}
        rows={[row("1", { refusal: en.restoreParentDeleted }), row("2")]}
        restoringId="2"
      />,
    );

    expect(screen.getByRole("alert").textContent).toContain(
      "the parent item is still in Deleted items",
    );
    const busy = screen.getByRole("button", { name: "Working…" });
    expect(busy.getAttribute("aria-disabled")).toBe("true");
  });
});
