import type { Meta, StoryObj } from "@storybook/react";
import { BoardToolbar } from "./board-toolbar";

const noOp = () => undefined;
const meta = {
  title: "Work Management/Board Layout/Toolbar",
  component: BoardToolbar,
} satisfies Meta<typeof BoardToolbar>;
export default meta;
type Story = StoryObj<typeof meta>;
export const Default: Story = {
  args: {
    boardTitle: "Operating plan",
    activeView: "kanban",
    onViewChange: noOp,
    searchQuery: "",
    onSearchChange: noOp,
    onFilter: noOp,
  },
  tags: ["fui-surface--wm.board-layout.toolbar", "fui-state--Default"],
};
