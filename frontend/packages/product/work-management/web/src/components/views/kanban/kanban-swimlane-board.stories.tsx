import type { Meta, StoryObj } from "@storybook/react";
import {
  erpKanbanEdgeLanes,
  erpKanbanEmptyLanes,
} from "../../../verification/kanban-board-fixtures";
import { erpSwimlaneScenario } from "../../../verification/kanban-swimlane-fixtures";
import { KanbanSwimlaneBoard } from "./kanban-swimlane-board";

const noOp = () => undefined;
const callbacks = { onOpenDetails: noOp, onDrop: noOp, onRefused: noOp };

const meta: Meta = {
  title: "Work Management/Kanban/Swimlanes",
  parameters: { layout: "fullscreen" },
  decorators: [
    (Story) => (
      <div className="overflow-auto bg-background p-4">
        <Story />
      </div>
    ),
  ],
};
export default meta;
type Story = StoryObj;

/** Columns by status, lanes by person; the unassigned lane comes last. */
export const Default: Story = {
  render: () => (
    <KanbanSwimlaneBoard {...erpSwimlaneScenario()} {...callbacks} />
  ),
  tags: ["fui-surface--wm.kanban.swimlanes", "fui-state--Default"],
};

/** Lanes by priority, a select field the drag can write. */
export const ByPriority: Story = {
  render: () => (
    <KanbanSwimlaneBoard
      {...erpSwimlaneScenario({ lane: "priority" })}
      {...callbacks}
    />
  ),
  tags: ["fui-surface--wm.kanban.swimlanes", "fui-state--EdgeData"],
};

/** A filter that leaves no item: the columns stay, the lanes are gone. */
export const Empty: Story = {
  render: () => (
    <KanbanSwimlaneBoard
      {...erpSwimlaneScenario({ columns: erpKanbanEmptyLanes() })}
      {...callbacks}
    />
  ),
  tags: ["fui-surface--wm.kanban.swimlanes", "fui-state--Empty"],
};

/** A very long title, an item without status and a column at its limit. */
export const EdgeData: Story = {
  render: () => (
    <KanbanSwimlaneBoard
      {...erpSwimlaneScenario({
        columns: erpKanbanEdgeLanes(),
        wipLimits: { "status-todo": 2 },
      })}
      {...callbacks}
    />
  ),
  tags: ["fui-surface--wm.kanban.swimlanes", "fui-state--EdgeData"],
};

/** A viewer collapses lanes but cannot drag. */
export const ReadOnly: Story = {
  render: () => (
    <KanbanSwimlaneBoard
      {...erpSwimlaneScenario({ canDrag: false })}
      {...callbacks}
    />
  ),
  tags: ["fui-surface--wm.kanban.swimlanes", "fui-state--ReadOnly"],
};
