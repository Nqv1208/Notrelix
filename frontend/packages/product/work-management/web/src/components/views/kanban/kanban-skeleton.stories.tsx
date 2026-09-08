import type { Meta, StoryObj } from "@storybook/react";
import { KanbanSkeleton } from "./kanban-skeleton";

const meta = {
  title: "Work Management/Kanban/Loading",
  component: KanbanSkeleton,
  parameters: {
    a11y: { disable: true },
  },
} satisfies Meta<typeof KanbanSkeleton>;
export default meta;
type Story = StoryObj<typeof meta>;
export const Loading: Story = {
  tags: ["fui-surface--wm.kanban.loading", "fui-state--Loading"],
};
