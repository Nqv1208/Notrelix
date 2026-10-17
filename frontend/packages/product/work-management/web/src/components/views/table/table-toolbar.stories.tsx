import { useState } from "react";
import type { Meta, StoryObj } from "@storybook/react";
import { erpTableToolbarProps } from "../../../verification/table-fixtures";
import { MainTableToolbar, type MainTableToolbarProps } from "./table-toolbar";

const meta = {
  title: "Work Management/Table/Toolbar",
  parameters: { layout: "fullscreen" },
} satisfies Meta;
export default meta;
type Story = StoryObj;

/** The toolbar with the state the container would hold. */
function ToolbarStory({ base }: { base: MainTableToolbarProps }) {
  const [state, setState] = useState(base);
  return (
    <div className="px-6">
      <MainTableToolbar
        {...state}
        onSearchChange={(searchQuery) =>
          setState((s) => ({ ...s, searchQuery }))
        }
        onShowDone={() => setState((s) => ({ ...s, hideDone: false }))}
        onFilterChange={(filter) => setState((s) => ({ ...s, filter }))}
        onGroupByChange={(groupByFieldId) =>
          setState((s) => ({ ...s, groupByFieldId }))
        }
        onSortChange={(sort) => setState((s) => ({ ...s, sort }))}
        onColumnVisibleChange={(id, visible) =>
          setState((s) => ({
            ...s,
            hiddenColumnIds: visible
              ? s.hiddenColumnIds.filter((hidden) => hidden !== id)
              : [...s.hiddenColumnIds, id],
          }))
        }
        onDensityChange={(density) => setState((s) => ({ ...s, density }))}
      />
    </div>
  );
}

/** Board binh-thuong: the "Trạng thái: chưa xong" chip. */
export const Default: Story = {
  render: () => <ToolbarStory base={erpTableToolbarProps("default")} />,
  tags: ["fui-surface--wm.table.toolbar", "fui-state--Default"],
};

/** Priority, person and overdue filters next to the done chip. */
export const Filtered: Story = {
  render: () => <ToolbarStory base={erpTableToolbarProps("filtered")} />,
};

/** Board chi-xem: no "Thêm công việc". */
export const ReadOnly: Story = {
  render: () => <ToolbarStory base={erpTableToolbarProps("read-only")} />,
  tags: ["fui-surface--wm.table.toolbar", "fui-state--ReadOnly"],
};

export const EdgeData: Story = {
  render: () => <ToolbarStory base={erpTableToolbarProps("edge")} />,
  tags: ["fui-surface--wm.table.toolbar", "fui-state--EdgeData"],
};
