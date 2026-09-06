import type { Meta, StoryObj } from "@storybook/react";
import {
  kanbanDefaultScenario,
  kanbanEdgeScenario,
} from "@notrelix/work-management-testing";
import { KanbanCard } from "./kanban-card";

const noOp = () => undefined;
const normal = kanbanDefaultScenario();
const meta = {
  title: "Work Management/Kanban/Card",
  component: KanbanCard,
} satisfies Meta<typeof KanbanCard>;
export default meta;
type Story = StoryObj<typeof meta>;
const callbacks = { onOpenDetails: noOp, onDuplicate: noOp, onDelete: noOp };

export const Default: Story = {
  args: {
    ...callbacks,
    board: normal.board,
    card: normal.columns[0]!.cards[0]!,
  },
  tags: ["fui-surface--wm.kanban.card", "fui-state--Default"],
};
const edge = kanbanEdgeScenario();
export const EdgeData: Story = {
  args: { ...callbacks, board: edge.board, card: edge.columns[0]!.cards[0]! },
  tags: ["fui-surface--wm.kanban.card", "fui-state--EdgeData"],
};
