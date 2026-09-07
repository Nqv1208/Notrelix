import type { Meta, StoryObj } from "@storybook/react";
import { BoardWorkspaceSurface } from "./board-workspace-surface";

const meta = {
  title: "Work Management/Board Workspace/Surface",
  component: BoardWorkspaceSurface,
} satisfies Meta<typeof BoardWorkspaceSurface>;
export default meta;
type Story = StoryObj<typeof meta>;
export const Default: Story = {
  args: {
    status: "ready",
    viewContent: (
      <div className="text-sm text-muted-foreground">
        Board content placeholder
      </div>
    ),
  },
  tags: ["fui-surface--wm.board-workspace.surface", "fui-state--Default"],
};
export const Loading: Story = {
  args: {
    status: "loading",
    skeletonRows: 4,
  },
  tags: ["fui-surface--wm.board-workspace.surface", "fui-state--Loading"],
};
export const ErrorState: Story = {
  args: {
    status: "error",
    error: new Error("Board unavailable"),
  },
  tags: ["fui-surface--wm.board-workspace.surface", "fui-state--Error"],
};
export const UnsupportedView: Story = {
  args: {
    status: "unsupported",
    unsupportedViewName: "Docs",
  },
  tags: ["fui-surface--wm.board-workspace.surface", "fui-state--EdgeData"],
};
