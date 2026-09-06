import type { Meta, StoryObj } from "@storybook/react";
import {
  kanbanDefaultScenario,
  kanbanDenseScenario,
  kanbanEdgeScenario,
  kanbanEmptyScenario,
} from "@notrelix/work-management-testing";
import { KanbanBoard } from "./kanban-board";

const noOp = () => undefined;
const defaults = {
  onOpenDetails: noOp,
  onMoveCard: noOp,
  onReorderColumns: noOp,
  onAdd: noOp,
  onRenameColumn: noOp,
  onColorChangeColumn: noOp,
  onDeleteColumn: noOp,
  onDuplicateCard: noOp,
  onDeleteCard: noOp,
  onCreateCard: noOp,
};

const meta = {
  title: "Work Management/Kanban/Board",
  component: KanbanBoard,
} satisfies Meta<typeof KanbanBoard>;
export default meta;
type Story = StoryObj<typeof meta>;

function argsFor(scenario: ReturnType<typeof kanbanDefaultScenario>) {
  return { ...defaults, board: scenario.board, columns: scenario.columns };
}

export const Default: Story = {
  args: argsFor(kanbanDefaultScenario()),
  tags: ["fui-surface--wm.kanban.board", "fui-state--Default"],
};
export const Empty: Story = {
  args: argsFor(kanbanEmptyScenario()),
  tags: ["fui-surface--wm.kanban.board", "fui-state--Empty"],
};
export const EdgeData: Story = {
  args: argsFor(kanbanEdgeScenario()),
  tags: ["fui-surface--wm.kanban.board", "fui-state--EdgeData"],
};
export const HighDensity: Story = {
  args: argsFor(kanbanDenseScenario()),
  tags: ["fui-surface--wm.kanban.board", "fui-state--HighDensity"],
};
