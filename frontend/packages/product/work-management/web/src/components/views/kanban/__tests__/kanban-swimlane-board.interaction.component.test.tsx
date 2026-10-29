import { beforeAll, describe, expect, it, vi } from "vitest";
import { fireEvent, renderPureUi, screen, within } from "@notrelix/testing";
import { erpKanbanEmptyLanes } from "../../../../verification/kanban-board-fixtures";
import {
  erpSwimlaneScenario,
  type SwimlaneScenario,
} from "../../../../verification/kanban-swimlane-fixtures";
import { KanbanSwimlaneBoard } from "../kanban-swimlane-board";

beforeAll(() => {
  Element.prototype.scrollIntoView = () => undefined;
});

function renderBoard(scenario: SwimlaneScenario) {
  const spies = {
    onOpenDetails: vi.fn(),
    onDrop: vi.fn(),
    onRefused: vi.fn(),
  };
  const view = renderPureUi(<KanbanSwimlaneBoard {...scenario} {...spies} />);
  return { spies, ...view };
}

const lanesOf = (container: HTMLElement) =>
  Array.from(container.querySelectorAll<HTMLElement>("[data-swimlane]"));

/** The first card of a status column inside the first lane that has one. */
function firstCard(container: HTMLElement) {
  const card = container.querySelector<HTMLElement>("[data-card-id]");
  if (!card) throw new Error("no card on the board");
  return card;
}

describe("KanbanSwimlaneBoard", () => {
  it("FUI[wm.kanban.swimlanes:board] draws the status columns once and one lane per person, the unassigned lane last", () => {
    const scenario = erpSwimlaneScenario();
    const { container } = renderBoard(scenario);
    const headers = screen.getAllByRole("columnheader");
    expect(headers.map((h) => h.textContent)).toEqual(
      scenario.columns.map((c) => `${c.name}${c.cards.length}`),
    );
    const lanes = lanesOf(container);
    expect(lanes.length).toBeGreaterThan(1);
    const names = lanes.map((lane) => lane.getAttribute("aria-label"));
    expect(names.at(-1)).toBe("Chưa giao");
    // Every card appears exactly once.
    const total = scenario.columns.reduce((n, c) => n + c.cards.length, 0);
    expect(container.querySelectorAll("[data-card-id]").length).toBe(total);
  });

  it("FUI[wm.kanban.swimlanes:collapse] collapses one lane to its per-column counts and opens it again", () => {
    const { container } = renderBoard(erpSwimlaneScenario());
    const lane = lanesOf(container)[0]!;
    const name = lane.getAttribute("aria-label")!;
    const toggle = within(lane).getByRole("button", {
      name: `Thu gọn làn ${name}`,
    });
    expect(toggle.getAttribute("aria-expanded")).toBe("true");
    const before = container.querySelectorAll("[data-card-id]").length;
    const inLane = lane.querySelectorAll("[data-card-id]").length;
    fireEvent.click(toggle);
    const open = within(lane).getByRole("button", {
      name: `Mở rộng làn ${name}`,
    });
    expect(open.getAttribute("aria-expanded")).toBe("false");
    expect(lane.querySelectorAll("[data-card-id]").length).toBe(0);
    expect(container.querySelectorAll("[data-card-id]").length).toBe(
      before - inLane,
    );
    // The collapsed header still says how many items each column holds.
    expect(lane.textContent).toMatch(/Cần làm\s*\d/);
    fireEvent.click(open);
    expect(lane.querySelectorAll("[data-card-id]").length).toBe(inLane);
  });

  it("FUI[wm.kanban.swimlanes:move] moves a card to the next lane with Alt+ArrowDown and reports the lane write", () => {
    const { container, spies } = renderBoard(erpSwimlaneScenario());
    const card = firstCard(container);
    fireEvent.keyDown(card, { key: "ArrowDown", altKey: true });
    expect(spies.onDrop).toHaveBeenCalledOnce();
    const drop = spies.onDrop.mock.calls[0]![0] as {
      card: { id: string };
      plan: { ok: boolean; laneChanged: boolean; columnChanged: boolean };
    };
    expect(drop.card.id).toBe(card.getAttribute("data-card-id"));
    expect(drop.plan).toMatchObject({
      ok: true,
      laneChanged: true,
      columnChanged: false,
    });
    expect(spies.onRefused).not.toHaveBeenCalled();
  });

  it("FUI[wm.kanban.swimlanes:both] changes the column and the lane together when the card moves diagonally", () => {
    const { container, spies } = renderBoard(erpSwimlaneScenario());
    const card = firstCard(container);
    fireEvent.keyDown(card, { key: "ArrowRight", altKey: true });
    fireEvent.keyDown(card, { key: "ArrowDown", altKey: true });
    expect(spies.onDrop).toHaveBeenCalledTimes(2);
    const [column, lane] = spies.onDrop.mock.calls.map(
      (call) =>
        (
          call[0] as {
            plan: { columnChanged: boolean; laneChanged: boolean };
          }
        ).plan,
    );
    expect(column).toMatchObject({ columnChanged: true, laneChanged: false });
    expect(lane).toMatchObject({ columnChanged: false, laneChanged: true });
  });

  it("FUI[wm.kanban.swimlanes:refused] refuses a lane move on a read-only lane field with its reason", () => {
    const { container, spies } = renderBoard(
      erpSwimlaneScenario({ lane: "overdue" }),
    );
    fireEvent.keyDown(firstCard(container), {
      key: "ArrowDown",
      altKey: true,
    });
    expect(spies.onDrop).not.toHaveBeenCalled();
    expect(spies.onRefused).toHaveBeenCalledOnce();
    expect(spies.onRefused.mock.calls[0]![0]).toMatchObject({
      reason: "lane_readonly",
    });
  });

  it("FUI[wm.kanban.swimlanes:wip] refuses a drop into a column that is at its limit", () => {
    const scenario = erpSwimlaneScenario();
    const target = scenario.columns[1]!;
    const { container, spies } = renderBoard({
      ...scenario,
      wipLimits: { [target.optionId!]: target.total },
    });
    const card = Array.from(
      container.querySelectorAll<HTMLElement>("[data-card-id]"),
    ).find((element) => {
      const id = element.getAttribute("data-card-id");
      return scenario.columns[0]!.cards.some((c) => c.id === id);
    })!;
    fireEvent.keyDown(card, { key: "ArrowRight", altKey: true });
    expect(spies.onRefused.mock.calls[0]![0]).toMatchObject({ reason: "wip" });
    expect(spies.onDrop).not.toHaveBeenCalled();
  });

  it("FUI[wm.kanban.swimlanes:read-only] gives a viewer no move keys but keeps the lanes collapsible", () => {
    const { container, spies } = renderBoard(
      erpSwimlaneScenario({ canDrag: false }),
    );
    fireEvent.keyDown(firstCard(container), {
      key: "ArrowDown",
      altKey: true,
    });
    expect(spies.onDrop).not.toHaveBeenCalled();
    expect(spies.onRefused).not.toHaveBeenCalled();
    const lane = lanesOf(container)[0]!;
    fireEvent.click(within(lane).getAllByRole("button")[0]!);
    expect(lane.querySelectorAll("[data-card-id]").length).toBe(0);
  });

  it("FUI[wm.kanban.swimlanes:empty] keeps the columns and has no lane when no item is left", () => {
    const { container } = renderBoard(
      erpSwimlaneScenario({ columns: erpKanbanEmptyLanes() }),
    );
    expect(screen.getAllByRole("columnheader").length).toBe(4);
    expect(lanesOf(container).length).toBe(0);
  });

  it("speaks English with the en copy", () => {
    const { container } = renderBoard(erpSwimlaneScenario({ locale: "en" }));
    expect(lanesOf(container).at(-1)!.getAttribute("aria-label")).toBe(
      "Unassigned",
    );
    expect(
      screen.getAllByRole("button", { name: /^Collapse lane / }).length,
    ).toBeGreaterThan(0);
  });
});
