import type { Meta, StoryObj } from "@storybook/react";
import { ViewTabs } from "./view-tabs";
import { en } from "./board-header.copy";

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
    copy: en,
  },
  tags: ["fui-surface--wm.board-layout.view-tabs", "fui-state--Default"],
};
