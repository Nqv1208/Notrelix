import type { Meta, StoryObj } from "@storybook/react";
import {
  kanbanDefaultScenario,
  kanbanEdgeScenario,
} from "@notrelix/work-management-testing";
import { KanbanColumn } from "./kanban-column";

const noOp = () => undefined;
const base = kanbanDefaultScenario();
const meta = {
  title: "Work Management/Kanban/Column",
  component: KanbanColumn,
} satisfies Meta<typeof KanbanColumn>;
export default meta;
type Story = StoryObj<typeof meta>;
const callbacks = {
  onOpenDetails: noOp,
  onRename: noOp,
  onColorChange: noOp,
  onDelete: noOp,
  onDuplicateCard: noOp,
  onDeleteCard: noOp,
  onCreateCard: noOp,
};

export const Default: Story = {
  args: { ...callbacks, board: base.board, group: base.columns[0]! },
  tags: ["fui-surface--wm.kanban.column", "fui-state--Default"],
};
export const Empty: Story = {
  args: {
    ...callbacks,
    board: base.board,
    group: { ...base.columns[0]!, cards: [] },
  },
  tags: ["fui-surface--wm.kanban.column", "fui-state--Empty"],
};
const edge = kanbanEdgeScenario();
export const EdgeData: Story = {
  args: { ...callbacks, board: edge.board, group: edge.columns[0]! },
  tags: ["fui-surface--wm.kanban.column", "fui-state--EdgeData"],
};
