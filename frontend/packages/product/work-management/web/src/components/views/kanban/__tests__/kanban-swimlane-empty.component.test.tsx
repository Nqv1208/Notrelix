import { describe, expect, it } from "vitest";
import { renderPureUi, screen } from "@notrelix/testing";
import { erpSwimlaneScenario } from "../../../../verification/kanban-swimlane-fixtures";
import { KanbanSwimlaneBoard } from "../kanban-swimlane-board";

const noop = () => undefined;

describe("KanbanSwimlaneBoard lane with no match", () => {
  it("says nothing matches in a lane the filter emptied, and keeps the cells of a lane when no filter is on", () => {
    const scenario = erpSwimlaneScenario();
    const first = scenario.swimlanes[0]!;
    const emptied = { ...first, cards: [] };
    const props = {
      ...scenario,
      swimlanes: [emptied],
      onOpenDetails: noop,
      onDrop: noop,
      onRefused: noop,
    };
    const view = renderPureUi(<KanbanSwimlaneBoard {...props} filterActive />);
    expect(screen.getByText(scenario.copy.states.emptyLane)).toBeTruthy();
    view.unmount();
    renderPureUi(<KanbanSwimlaneBoard {...props} />);
    expect(screen.queryByText(scenario.copy.states.emptyLane)).toBeNull();
  });
});
