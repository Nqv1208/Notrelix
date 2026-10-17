import type { Meta, StoryObj } from "@storybook/react";
import { KanbanColumn } from "./kanban-column";
import { kanbanCardCopy } from "./kanban-card.copy";
import { kanbanLaneCopy } from "./kanban-lane.copy";
import {
  KANBAN_STORY_NOW,
  erpKanbanEdgeLanes,
  erpKanbanLanes,
} from "../../../verification/kanban-board-fixtures";

const noOp = () => undefined;
const todo = erpKanbanLanes()[0]!;

const meta = {
  title: "Work Management/Kanban/Column",
  component: KanbanColumn,
  args: {
    lane: todo,
    name: todo.name,
    copy: kanbanLaneCopy("vi"),
    cardCopy: kanbanCardCopy("vi"),
    now: KANBAN_STORY_NOW,
    filterActive: false,
    canAdd: true,
    canDrag: true,
    collapsed: false,
    onCollapsedChange: noOp,
    onHide: noOp,
    onOpenDetails: noOp,
    onCreateItem: noOp,
  },
} satisfies Meta<typeof KanbanColumn>;
export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  tags: ["fui-surface--wm.kanban.column", "fui-state--Default"],
};
export const Empty: Story = {
  args: { lane: { ...todo, cards: [], total: 0 } },
  tags: ["fui-surface--wm.kanban.column", "fui-state--Empty"],
};
const edge = erpKanbanEdgeLanes()[1]!;
export const EdgeData: Story = {
  args: { lane: edge, name: edge.name },
  tags: ["fui-surface--wm.kanban.column", "fui-state--EdgeData"],
};
export const Collapsed: Story = {
  args: { collapsed: true },
  tags: ["fui-surface--wm.kanban.column", "fui-state--EdgeData"],
};
