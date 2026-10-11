import { describe, expect, it, vi } from "vitest";
import { renderPureUi, screen } from "@notrelix/testing";
import {
  cardFixture,
  boardGroupFixture,
  fixedClock,
  fixedIso,
  kanbanDefaultScenario,
} from "@notrelix/work-management-testing";
import { KanbanBoard } from "../kanban-board";
import { kanbanCardCopy } from "../kanban-card.copy";
import { KanbanColumn } from "../kanban-column";

const NOW = fixedClock();
const scenario = kanbanDefaultScenario();

function groupWith(dueDate: string) {
  return boardGroupFixture({
    id: "group-pass-through",
    title: "Doing",
    position: 1,
    cards: [
      cardFixture({
        id: "card-pass-through",
        listId: "group-pass-through",
        title: "Pass-through card",
        status: "status-working",
        dueDate,
      }),
    ],
  });
}

const noop = vi.fn();
const columnHandlers = {
  onOpenDetails: noop,
  onCopyCardLink: noop,
  onRename: noop,
  onColorChange: noop,
  onDelete: noop,
  onDuplicateCard: noop,
  onDeleteCard: noop,
  onCreateCard: noop,
};
const boardHandlers = {
  onOpenDetails: noop,
  onCopyCardLink: noop,
  onMoveCard: noop,
  onReorderColumns: noop,
  onAdd: noop,
  onRenameColumn: noop,
  onColorChangeColumn: noop,
  onDeleteColumn: noop,
  onDuplicateCard: noop,
  onDeleteCard: noop,
  onCreateCard: noop,
};

describe("Kanban card props reach the card through the column", () => {
  it("renders the Vietnamese card copy passed as cardCopy", () => {
    renderPureUi(
      <KanbanColumn
        board={scenario.board}
        group={groupWith(fixedIso(-5))}
        now={NOW}
        cardCopy={kanbanCardCopy("vi")}
        {...columnHandlers}
      />,
    );
    expect(screen.getByText("Quá hạn")).toBeTruthy();
    expect(screen.getByText("Đang thực hiện")).toBeTruthy();
    expect(screen.queryByText("Overdue")).toBeNull();
  });

  it("judges overdue against the now passed down, not the wall clock", () => {
    renderPureUi(
      <KanbanColumn
        board={scenario.board}
        group={groupWith(fixedIso(5))}
        now={NOW}
        {...columnHandlers}
      />,
    );
    expect(screen.queryByText("Overdue")).toBeNull();
  });
});

describe("Kanban card props reach the card through the board", () => {
  it("renders the Vietnamese card copy passed as cardCopy", () => {
    renderPureUi(
      <KanbanBoard
        board={scenario.board}
        columns={[groupWith(fixedIso(-5))]}
        now={NOW}
        cardCopy={kanbanCardCopy("vi")}
        {...boardHandlers}
      />,
    );
    expect(screen.getByText("Quá hạn")).toBeTruthy();
    expect(screen.queryByText("Overdue")).toBeNull();
  });

  it("judges overdue against the now passed down, not the wall clock", () => {
    renderPureUi(
      <KanbanBoard
        board={scenario.board}
        columns={[groupWith(fixedIso(5))]}
        now={NOW}
        {...boardHandlers}
      />,
    );
    expect(screen.queryByText("Overdue")).toBeNull();
  });
});
