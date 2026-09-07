import type { Meta, StoryObj } from "@storybook/react";
import { ViewTabs } from "./view-tabs";

const noOp = () => undefined;
const meta = {
  title: "Work Management/Board Layout/View Tabs",
  component: ViewTabs,
} satisfies Meta<typeof ViewTabs>;
export default meta;
type Story = StoryObj<typeof meta>;
export const Default: Story = {
  args: {
    activeView: "kanban",
    onViewChange: noOp,
  },
  tags: ["fui-surface--wm.board-layout.view-tabs", "fui-state--Default"],
};
