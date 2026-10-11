import { describe, expect, it, vi } from "vitest";
import { renderPureUi, screen } from "@notrelix/testing";
import { kanbanDefaultScenario } from "@notrelix/work-management-testing";
import { KanbanBoard } from "../kanban-board";
import { KanbanColumn } from "../kanban-column";
import { KanbanSkeleton } from "../kanban-skeleton";
import { kanbanColumnWidthClass } from "../kanban-layout";

const noop = vi.fn();

describe("Kanban column width", () => {
  it("uses the shared token-backed width on the column, the add-column slot and the skeleton", () => {
    expect(kanbanColumnWidthClass).toBe("w-(--layout-kanban-column)");

    const scenario = kanbanDefaultScenario();
    const column = renderPureUi(
      <KanbanColumn
        board={scenario.board}
        group={scenario.columns[0]!}
        onOpenDetails={noop}
        onCopyCardLink={noop}
        onRename={noop}
        onColorChange={noop}
        onDelete={noop}
        onDuplicateCard={noop}
        onDeleteCard={noop}
        onCreateCard={noop}
      />,
    );
    const columnSection = screen.getByRole("region", {
      name: /kanban column/i,
    });
    expect(columnSection.className).toContain(kanbanColumnWidthClass);
    expect(columnSection.className).not.toMatch(/w-\[\d+px\]/);
    column.unmount();

    const skeleton = renderPureUi(<KanbanSkeleton />);
    const skeletonColumns = skeleton.container.querySelectorAll(
      `.${CSS.escape(kanbanColumnWidthClass)}`,
    );
    expect(skeletonColumns.length).toBe(4);
    skeleton.unmount();

    const board = renderPureUi(
      <KanbanBoard
        board={scenario.board}
        columns={scenario.columns}
        onOpenDetails={noop}
        onCopyCardLink={noop}
        onMoveCard={noop}
        onReorderColumns={noop}
        onAdd={noop}
        onRenameColumn={noop}
        onColorChangeColumn={noop}
        onDeleteColumn={noop}
        onDuplicateCard={noop}
        onDeleteCard={noop}
        onCreateCard={noop}
      />,
    );
    const boardColumns = board.container.querySelectorAll(
      `.${CSS.escape(kanbanColumnWidthClass)}`,
    );
    // every column plus the add-column slot
    expect(boardColumns.length).toBe(scenario.columns.length + 1);
  });
});
