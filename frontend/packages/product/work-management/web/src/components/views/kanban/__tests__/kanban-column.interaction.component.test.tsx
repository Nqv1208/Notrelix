import { describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen } from "@notrelix/testing";
import { kanbanDefaultScenario } from "@notrelix/work-management-testing";
import { KanbanColumn } from "../kanban-column";

function renderColumn(onRename = vi.fn()) {
  const scenario = kanbanDefaultScenario();
  renderPureUi(
    <KanbanColumn
      board={scenario.board}
      group={scenario.columns[0]!}
      onOpenDetails={vi.fn()}
      onRename={onRename}
      onColorChange={vi.fn()}
      onDelete={vi.fn()}
      onDuplicateCard={vi.fn()}
      onDeleteCard={vi.fn()}
      onCreateCard={vi.fn()}
    />,
  );
  return onRename;
}

describe("KanbanColumn interactions", () => {
  it("renames on Enter", () => {
    const onRename = renderColumn();
    fireEvent.doubleClick(screen.getByRole("heading", { name: "Column 1" }));
    const input = screen.getByDisplayValue("Column 1");
    fireEvent.change(input, { target: { value: "Renamed" } });
    fireEvent.keyDown(input, { key: "Enter" });
    expect(onRename).toHaveBeenCalledWith("Renamed");
  });

  it("cancels rename on Escape", () => {
    const onRename = renderColumn();
    fireEvent.doubleClick(screen.getByRole("heading", { name: "Column 1" }));
    const input = screen.getByDisplayValue("Column 1");
    fireEvent.change(input, { target: { value: "Ignored" } });
    fireEvent.keyDown(input, { key: "Escape" });
    expect(onRename).not.toHaveBeenCalled();
  });
});
