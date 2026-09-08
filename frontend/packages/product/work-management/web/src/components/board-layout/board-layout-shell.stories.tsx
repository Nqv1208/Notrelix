import type { Meta, StoryObj } from "@storybook/react";
import { BoardLayoutShell } from "./board-layout-shell";

const noOp = () => undefined;
const meta = {
  title: "Work Management/Board Layout/Shell",
  component: BoardLayoutShell,
} satisfies Meta<typeof BoardLayoutShell>;
export default meta;
type Story = StoryObj<typeof meta>;
export const Default: Story = {
  args: {
    workspaceId: "ws-1",
    boardId: "board-1",
    boardTitle: "Operating plan",
    activeView: "kanban",
    onViewChange: noOp,
    children: (
      <div className="p-4 text-sm text-muted-foreground">
        Board content placeholder
      </div>
    ),
  },
  tags: ["fui-surface--wm.board-layout.shell", "fui-state--Default"],
};
