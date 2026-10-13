import type { Meta, StoryObj } from "@storybook/react";
import { KanbanBoard } from "./kanban-board";
import { kanbanCardCopy } from "./kanban-card.copy";
import { kanbanLaneCopy } from "./kanban-lane.copy";
import {
  KANBAN_STORY_NOW,
  erpKanbanDenseLanes,
  erpKanbanEdgeLanes,
  erpKanbanEmptyLanes,
  erpKanbanLanes,
  erpKanbanNoResultLanes,
} from "../../../verification/kanban-board-fixtures";

const noOp = () => undefined;

const meta = {
  title: "Work Management/Kanban/Board",
  component: KanbanBoard,
  args: {
    lanes: erpKanbanLanes(),
    copy: kanbanLaneCopy("vi"),
    cardCopy: kanbanCardCopy("vi"),
    now: KANBAN_STORY_NOW,
    filterActive: false,
    canAdd: true,
    canDrag: true,
    onOpenDetails: noOp,
    onDropCard: noOp,
    onCreateItem: noOp,
  },
} satisfies Meta<typeof KanbanBoard>;
export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  tags: ["fui-surface--wm.kanban.board", "fui-state--Default"],
};
export const Empty: Story = {
  args: { lanes: erpKanbanEmptyLanes() },
  tags: ["fui-surface--wm.kanban.board", "fui-state--Empty"],
};
export const EdgeData: Story = {
  args: { lanes: erpKanbanEdgeLanes() },
  tags: ["fui-surface--wm.kanban.board", "fui-state--EdgeData"],
};
export const NoResults: Story = {
  args: { lanes: erpKanbanNoResultLanes(), filterActive: true, canAdd: false },
  tags: ["fui-surface--wm.kanban.board", "fui-state--EdgeData"],
};
export const HighDensity: Story = {
  args: { lanes: erpKanbanDenseLanes() },
  tags: ["fui-surface--wm.kanban.board", "fui-state--HighDensity"],
};
export const ReadOnly: Story = {
  args: { canAdd: false, canDrag: false },
  tags: ["fui-surface--wm.kanban.board", "fui-state--ReadOnly"],
};
export const English: Story = {
  args: { copy: kanbanLaneCopy("en"), cardCopy: kanbanCardCopy("en") },
  tags: ["fui-surface--wm.kanban.board", "fui-state--EdgeData"],
};
