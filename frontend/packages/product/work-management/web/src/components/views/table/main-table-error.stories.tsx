import type { Meta, StoryObj } from "@storybook/react";
import { MainTableError } from "./main-table-error";

const meta = {
  title: "Work Management/Table/Unavailable",
  component: MainTableError,
} satisfies Meta<typeof MainTableError>;
export default meta;
type Story = StoryObj<typeof meta>;

export const Error: Story = {
  args: { message: "The board table could not be loaded." },
  tags: ["fui-surface--wm.table.unavailable", "fui-state--Error"],
};
