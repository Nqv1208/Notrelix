import type { Meta, StoryObj } from "@storybook/react";
import { KanbanSettingsPanel } from "./kanban-settings-panel";
import { kanbanLaneCopy } from "./kanban-lane.copy";

const meta = {
  title: "Work Management/Kanban/Settings Panel",
  component: KanbanSettingsPanel,
  args: {
    copy: kanbanLaneCopy("vi"),
    settings: {
      laneCardLimit: 4,
      statusManageRoles: ["manager", "editor"],
      keyboardMove: true,
      wipLimits: {},
    },
    onChange: () => undefined,
  },
} satisfies Meta<typeof KanbanSettingsPanel>;
export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  tags: ["fui-surface--wm.kanban.board", "fui-state--EdgeData"],
};
