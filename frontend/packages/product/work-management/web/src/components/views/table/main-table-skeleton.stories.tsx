import type { Meta, StoryObj } from "@storybook/react";
import { MainTableSkeleton } from "./main-table-skeleton";

const meta = {
  title: "Work Management/Table/Loading",
  component: MainTableSkeleton,
} satisfies Meta<typeof MainTableSkeleton>;
export default meta;
type Story = StoryObj<typeof meta>;

export const Loading: Story = {
  tags: ["fui-surface--wm.table.loading", "fui-state--Loading"],
};
