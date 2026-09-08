import type { Meta, StoryObj } from "@storybook/react";
import { KanbanUnavailableState } from "./kanban-unavailable-state";

const meta = {
  title: "Work Management/Kanban/Unavailable",
  component: KanbanUnavailableState,
} satisfies Meta<typeof KanbanUnavailableState>;
export default meta;
type Story = StoryObj<typeof meta>;
export const Error: Story = {
  tags: ["fui-surface--wm.kanban.unavailable", "fui-state--Error"],
};
