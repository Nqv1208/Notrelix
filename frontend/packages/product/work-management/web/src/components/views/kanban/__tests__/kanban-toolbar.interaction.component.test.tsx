import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen } from "@notrelix/testing";
import { KanbanToolbar } from "../kanban-toolbar";

describe("KanbanToolbar interactions", () => {
  it("emits search and create callbacks", () => {
    const onSearchChange = vi.fn();
    const onAddColumn = vi.fn();
    const onCreateCard = vi.fn();
    renderPureUi(
      <KanbanToolbar
        searchQuery=""
        onSearchChange={onSearchChange}
        onClearFilters={vi.fn()}
        activeSort="position"
        onSortChange={vi.fn()}
        onCreateCard={onCreateCard}
        onAddColumn={onAddColumn}
      />,
    );
    fireEvent.change(screen.getByPlaceholderText("Search cards..."), {
      target: { value: "alpha" },
    });
    fireEvent.click(screen.getByRole("button", { name: "Column" }));
    fireEvent.click(screen.getByRole("button", { name: "Card" }));
    expect(onSearchChange).toHaveBeenCalledWith("alpha");
    expect(onAddColumn).toHaveBeenCalledOnce();
    expect(onCreateCard).toHaveBeenCalledOnce();
  });
});
