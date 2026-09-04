import type { Meta, StoryObj } from "@storybook/react";
import { KanbanToolbar } from "./kanban-toolbar";

const noOp = () => undefined;
const meta = {
  title: "Work Management/Kanban/Toolbar",
  component: KanbanToolbar,
} satisfies Meta<typeof KanbanToolbar>;
export default meta;
type Story = StoryObj<typeof meta>;
export const Default: Story = {
  args: {
    searchQuery: "",
    onSearchChange: noOp,
    onClearFilters: noOp,
    activeSort: "position",
    onSortChange: noOp,
    onCreateCard: noOp,
    onAddColumn: noOp,
  },
  tags: ["fui-surface--wm.kanban.toolbar", "fui-state--Default"],
};
