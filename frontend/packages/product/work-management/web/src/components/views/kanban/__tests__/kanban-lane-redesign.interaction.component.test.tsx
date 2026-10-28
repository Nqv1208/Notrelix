import { afterEach, describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen, within } from "@notrelix/testing";
import {
  groupCardsByStatus,
  type KanbanStatusLane,
} from "@notrelix/work-management-core";
import { erpBoardScenario } from "@notrelix/work-management-testing";
import { KanbanBoard } from "../kanban-board";
import { kanbanCardCopy } from "../kanban-card.copy";
import { kanbanLaneCopy } from "../kanban-lane.copy";
import {
  useKanbanCollapsedLanes,
  type CollapsedLanesStorage,
} from "../use-kanban-collapsed-lanes";

const NOW = new Date(2026, 9, 12, 10, 40);
const copy = kanbanLaneCopy("vi");
const KEY = "notrelix.kanban.collapsed.test";

function erpLanes(): KanbanStatusLane[] {
  const { board, groups } = erpBoardScenario();
  return groupCardsByStatus({
    fieldDefinitions: board.fieldDefinitions,
    groups,
  });
}

function renderBoard(
  props: Partial<Parameters<typeof KanbanBoard>[0]> = {},
  lanes: KanbanStatusLane[] = erpLanes(),
) {
  const handlers = {
    onOpenDetails: vi.fn(),
    onDropCard: vi.fn(),
    onCreateItem: vi.fn(),
    onRequestCreate: vi.fn(),
    onExpandCreate: vi.fn(),
    onWipLimitChange: vi.fn(),
  };
  const view = renderPureUi(
    <KanbanBoard
      lanes={lanes}
      copy={copy}
      cardCopy={kanbanCardCopy("vi")}
      now={NOW}
      filterActive={false}
      canAdd
      canDrag
      {...handlers}
      {...props}
    />,
  );
  return { ...view, ...handlers };
}

/** The board with the container's remembered collapsed lanes, as the view wires it. */
function Harness({ storage }: { storage: CollapsedLanesStorage }) {
  const collapsed = useKanbanCollapsedLanes(KEY, storage);
  return (
    <KanbanBoard
      lanes={erpLanes()}
      copy={copy}
      cardCopy={kanbanCardCopy("vi")}
      now={NOW}
      filterActive={false}
      canAdd
      canDrag
      onOpenDetails={vi.fn()}
      onDropCard={vi.fn()}
      onCreateItem={vi.fn()}
      initialCollapsed={collapsed.initial}
      onCollapsedChange={collapsed.onChange}
    />
  );
}

const lane = (name: string) => screen.getByRole("region", { name });

afterEach(() => window.localStorage.clear());

describe("KanbanBoard lanes (redesign)", () => {
  it("FUI[wm.kanban.column:header-add] opens the full form for the lane from its header plus", () => {
    const { onRequestCreate } = renderBoard();
    fireEvent.click(
      within(lane("Cần làm")).getByRole("button", {
        name: copy.addToLane.replace("{{lane}}", "Cần làm"),
      }),
    );
    expect(onRequestCreate).toHaveBeenCalledTimes(1);
    expect(onRequestCreate.mock.calls[0]?.[0]).toMatchObject({
      name: "Cần làm",
    });
  });

  it("FUI[wm.kanban.column:quick-expand] hands the typed title to the full form from Thêm chi tiết and from Ctrl+Enter", () => {
    const { onExpandCreate, onCreateItem } = renderBoard();
    const todo = lane("Cần làm");
    fireEvent.click(
      within(todo).getByRole("button", {
        name: copy.addTo.replace("{{lane}}", "Cần làm"),
      }),
    );
    const input = within(todo).getByRole("textbox");
    fireEvent.change(input, { target: { value: "Chốt lịch đào tạo" } });
    fireEvent.click(
      within(todo).getByRole("button", { name: copy.quickDetail }),
    );
    expect(onExpandCreate).toHaveBeenCalledWith(
      expect.objectContaining({ name: "Cần làm" }),
      "Chốt lịch đào tạo",
    );
    expect(onCreateItem).not.toHaveBeenCalled();

    fireEvent.click(
      within(lane("Cần làm")).getByRole("button", {
        name: copy.addTo.replace("{{lane}}", "Cần làm"),
      }),
    );
    const again = within(lane("Cần làm")).getByRole("textbox");
    fireEvent.change(again, { target: { value: "Việc thứ hai" } });
    fireEvent.keyDown(again, { key: "Enter", ctrlKey: true });
    expect(onExpandCreate).toHaveBeenLastCalledWith(
      expect.anything(),
      "Việc thứ hai",
    );
  });

  it("FUI[wm.kanban.column:wip] shows the count over the limit, warns at and over it and never refuses", () => {
    const lanes = erpLanes();
    const todo = lanes.find((candidate) => candidate.name === "Cần làm");
    expect(todo).toBeDefined();
    const optionId = todo!.optionId!;
    const count = todo!.cards.length;
    const { unmount } = renderBoard({ wipLimits: { [optionId]: count } });
    const full = lane("Cần làm");
    expect(full.querySelector("[data-wip='full']")?.textContent).toBe(
      `${count} / ${count}`,
    );
    expect(
      within(full).getByText(copy.wipFull.replace("{{limit}}", String(count))),
    ).toBeTruthy();
    unmount();

    renderBoard({ wipLimits: { [optionId]: count - 1 } });
    const over = lane("Cần làm");
    expect(over.querySelector("[data-wip='over']")?.textContent).toBe(
      `${count} / ${count - 1}`,
    );
    expect(over.querySelector("[data-wip-note='over']")).not.toBeNull();
    // Warn only: adding is still offered.
    expect(
      within(over).getByRole("button", {
        name: copy.addTo.replace("{{lane}}", "Cần làm"),
      }),
    ).toBeTruthy();
  });

  it("FUI[wm.kanban.column:wip-set] sets and clears a lane limit from the lane menu", async () => {
    const { onWipLimitChange } = renderBoard();
    fireEvent.keyDown(
      within(lane("Đang làm")).getByRole("button", {
        name: copy.laneOptions.replace("{{lane}}", "Đang làm"),
      }),
      { key: "Enter" },
    );
    fireEvent.click(screen.getByRole("menuitem", { name: copy.wipSet }));
    const input = await screen.findByRole("textbox", {
      name: copy.wipLabel.replace("{{lane}}", "Đang làm"),
    });
    fireEvent.change(input, { target: { value: "0" } });
    fireEvent.click(screen.getByRole("button", { name: copy.wipSave }));
    expect(screen.getByText(copy.wipInvalid)).toBeTruthy();
    expect(onWipLimitChange).not.toHaveBeenCalled();
    fireEvent.change(input, { target: { value: "5" } });
    fireEvent.click(screen.getByRole("button", { name: copy.wipSave }));
    expect(onWipLimitChange).toHaveBeenCalledWith(
      expect.objectContaining({ name: "Đang làm" }),
      5,
    );
  });

  it("FUI[wm.kanban.column:collapse-persist] collapses from the visible button and remembers it per workspace and view", () => {
    const first = renderPureUi(<Harness storage={window.localStorage} />);
    fireEvent.click(
      within(lane("Cần làm")).getByRole("button", {
        name: copy.collapseLane.replace("{{lane}}", "Cần làm"),
      }),
    );
    expect(
      screen.getByRole("button", {
        name: copy.expand.replace("{{lane}}", "Cần làm"),
      }),
    ).toBeTruthy();
    expect(JSON.parse(window.localStorage.getItem(KEY) ?? "[]")).toHaveLength(
      1,
    );
    first.unmount();

    renderPureUi(<Harness storage={window.localStorage} />);
    expect(
      screen.getByRole("button", {
        name: copy.expand.replace("{{lane}}", "Cần làm"),
      }),
    ).toBeTruthy();
    // Expanding again forgets it.
    fireEvent.click(
      screen.getByRole("button", {
        name: copy.expand.replace("{{lane}}", "Cần làm"),
      }),
    );
    expect(window.localStorage.getItem(KEY)).toBeNull();
  });

  it("FUI[wm.kanban.column:storage-unavailable] still collapses when the storage throws", () => {
    const blocked = {
      getItem: () => {
        throw new Error("blocked");
      },
      setItem: () => {
        throw new Error("blocked");
      },
      removeItem: () => {
        throw new Error("blocked");
      },
    };
    renderPureUi(<Harness storage={blocked} />);
    fireEvent.click(
      within(lane("Cần làm")).getByRole("button", {
        name: copy.collapseLane.replace("{{lane}}", "Cần làm"),
      }),
    );
    expect(
      screen.getByRole("button", {
        name: copy.expand.replace("{{lane}}", "Cần làm"),
      }),
    ).toBeTruthy();
  });

  it("FUI[wm.kanban.column:empty] says an empty lane is empty and offers to add, and says a filtered one is hidden not empty", () => {
    const lanes = erpLanes().map((candidate, index) =>
      index === 0 ? { ...candidate, cards: [], total: 0 } : candidate,
    );
    const { onRequestCreate, unmount } = renderBoard({}, lanes);
    const empty = lanes[0]!;
    const region = lane(empty.name || copy.noStatus);
    expect(within(region).getByText(copy.emptyTitle)).toBeTruthy();
    fireEvent.click(
      within(region).getByRole("button", { name: copy.emptyAdd }),
    );
    expect(onRequestCreate).toHaveBeenCalledTimes(1);
    unmount();

    const filtered = erpLanes().map((candidate, index) =>
      index === 0 ? { ...candidate, cards: [], total: 4 } : candidate,
    );
    renderBoard({ filterActive: true }, filtered);
    const hiddenRegion = lane(filtered[0]!.name || copy.noStatus);
    expect(within(hiddenRegion).getByText(copy.emptyFiltered)).toBeTruthy();
    expect(
      within(hiddenRegion).getByText(
        copy.filteredHidden_other.replace("{{count}}", "4"),
      ),
    ).toBeTruthy();
  });
});
