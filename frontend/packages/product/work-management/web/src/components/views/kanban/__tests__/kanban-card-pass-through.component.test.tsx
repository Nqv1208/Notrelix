import { describe, expect, it, vi } from "vitest";
import { renderPureUi, screen } from "@notrelix/testing";
import type { KanbanStatusLane } from "@notrelix/work-management-core";
import {
  cardFixture,
  fixedClock,
  fixedIso,
} from "@notrelix/work-management-testing";
import { KanbanBoard } from "../kanban-board";
import { kanbanCardCopy } from "../kanban-card.copy";
import { KanbanColumn } from "../kanban-column";
import { kanbanColumnWidthClass } from "../kanban-layout";
import { kanbanLaneCopy } from "../kanban-lane.copy";
import { KanbanSkeleton } from "../kanban-skeleton";

const NOW = fixedClock();
const noop = vi.fn();

function laneWith(dueDate: string): KanbanStatusLane {
  const card = cardFixture({
    id: "card-pass-through",
    listId: "group-pass-through",
    title: "Pass-through card",
    statusOptionId: "status-doing",
    statusCategory: "in_progress",
    dueDate,
  });
  return {
    optionId: "status-doing",
    name: "Đang làm",
    category: "in_progress",
    color: null,
    cards: [card],
    total: 1,
  };
}

const laneProps = {
  name: "Đang làm",
  copy: kanbanLaneCopy("vi"),
  filterActive: false,
  canAdd: true,
  canDrag: true,
  collapsed: false,
  onCollapsedChange: noop,
  onHide: noop,
  onOpenDetails: noop,
  onCreateItem: noop,
};

const boardProps = {
  copy: kanbanLaneCopy("vi"),
  filterActive: false,
  canAdd: true,
  canDrag: true,
  onOpenDetails: noop,
  onDropCard: noop,
  onCreateItem: noop,
};

describe("Kanban card props reach the card through the lane", () => {
  it("renders the Vietnamese card copy passed as cardCopy", () => {
    renderPureUi(
      <KanbanColumn
        lane={laneWith(fixedIso(-5))}
        now={NOW}
        cardCopy={kanbanCardCopy("vi")}
        {...laneProps}
      />,
    );
    expect(screen.getByText(/^Quá hạn \d+ ngày$/)).toBeTruthy();
    expect(screen.queryByText(/^Overdue/)).toBeNull();
  });

  it("judges overdue against the now passed down, not the wall clock", () => {
    renderPureUi(
      <KanbanColumn lane={laneWith(fixedIso(5))} now={NOW} {...laneProps} />,
    );
    expect(screen.queryByText(/^Overdue/)).toBeNull();
  });
});

describe("Kanban card props reach the card through the board", () => {
  it("renders the Vietnamese card copy passed as cardCopy", () => {
    renderPureUi(
      <KanbanBoard
        lanes={[laneWith(fixedIso(-5))]}
        now={NOW}
        cardCopy={kanbanCardCopy("vi")}
        {...boardProps}
      />,
    );
    expect(screen.getByText(/^Quá hạn \d+ ngày$/)).toBeTruthy();
    expect(screen.queryByText(/^Overdue/)).toBeNull();
  });

  it("judges overdue against the now passed down, not the wall clock", () => {
    renderPureUi(
      <KanbanBoard lanes={[laneWith(fixedIso(5))]} now={NOW} {...boardProps} />,
    );
    expect(screen.queryByText(/^Overdue/)).toBeNull();
  });
});

describe("Kanban lane width", () => {
  it("uses the shared token-backed width on the lane and the loading skeleton", () => {
    expect(kanbanColumnWidthClass).toBe("w-(--layout-kanban-column)");
    const lane = renderPureUi(
      <KanbanColumn lane={laneWith(fixedIso(5))} {...laneProps} />,
    );
    const region = screen.getByRole("region", { name: "Đang làm" });
    expect(region.className).toContain(kanbanColumnWidthClass);
    expect(region.className).not.toMatch(/w-\[\d+px\]/);
    lane.unmount();

    const skeleton = renderPureUi(<KanbanSkeleton />);
    expect(
      skeleton.container.querySelectorAll(
        `.${CSS.escape(kanbanColumnWidthClass)}`,
      ).length,
    ).toBe(4);
  });
});
