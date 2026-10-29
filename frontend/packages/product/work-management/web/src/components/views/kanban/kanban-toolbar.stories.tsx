import type { Meta, StoryObj } from "@storybook/react";
import {
  kanbanToolbarDefaultScenario,
  kanbanToolbarEdgeDataScenario,
  kanbanToolbarFilteredScenario,
  kanbanToolbarNoResultsScenario,
  kanbanToolbarReadOnlyScenario,
} from "../../../verification/kanban-toolbar-fixtures";
import { KanbanToolbar } from "./kanban-toolbar";

const noop = () => undefined;
const callbacks = {
  onSearchChange: noop,
  onClearFilter: noop,
  onAddItem: noop,
};

const meta: Meta = {
  title: "Work Management/Kanban/Toolbar",
  parameters: { layout: "fullscreen" },
};
export default meta;
type Story = StoryObj;

/** Board binh-thuong. */
export const Default: Story = {
  render: () => (
    <KanbanToolbar {...kanbanToolbarDefaultScenario()} {...callbacks} />
  ),
  tags: ["fui-surface--wm.kanban.toolbar", "fui-state--Default"],
};

/** A status and a label filter: the summary chip with its remove button. */
export const Filtered: Story = {
  render: () => (
    <KanbanToolbar {...kanbanToolbarFilteredScenario()} {...callbacks} />
  ),
};

/** Board khong-ket-qua: "Quá hạn · Ưu tiên Thấp" hides all 16 items. */
export const NoResults: Story = {
  render: () => (
    <KanbanToolbar {...kanbanToolbarNoResultsScenario()} {...callbacks} />
  ),
};

/** Board chi-xem: a commenter has no "Thêm công việc". */
export const ReadOnly: Story = {
  render: () => (
    <KanbanToolbar {...kanbanToolbarReadOnlyScenario()} {...callbacks} />
  ),
  tags: ["fui-surface--wm.kanban.toolbar", "fui-state--ReadOnly"],
};

export const EdgeData: Story = {
  render: () => (
    <KanbanToolbar {...kanbanToolbarEdgeDataScenario()} {...callbacks} />
  ),
  tags: ["fui-surface--wm.kanban.toolbar", "fui-state--EdgeData"],
};
