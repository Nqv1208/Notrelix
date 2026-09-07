import type { Meta, StoryObj } from "@storybook/react";
import { WorkspaceContextualToolbar } from "./workspace-contextual-toolbar";

const noOp = () => undefined;
const meta = {
  title: "Workspace/Contextual Toolbar",
  component: WorkspaceContextualToolbar,
} satisfies Meta<typeof WorkspaceContextualToolbar>;
export default meta;
type Story = StoryObj<typeof meta>;

export const KanbanMode: Story = {
  args: {
    activeType: "kanban",
    searchQuery: "",
    onSearchChange: noOp,
    onAction: noOp,
  },
  tags: ["fui-surface--workspace.contextual-toolbar", "fui-state--Default"],
};
