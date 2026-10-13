import { useState } from "react";
import type { Meta, StoryObj } from "@storybook/react";
import {
  erpTableArchivedProps,
  erpTableDenseProps,
  erpTableEdgeProps,
  erpTableEditingProps,
  erpTableEmptyProps,
  erpTableNoResultsProps,
  erpTableProps,
  erpTableReadOnlyProps,
  erpTableSelectedProps,
} from "../../../verification/table-fixtures";
import {
  MainTableSurface,
  type MainTableSurfaceProps,
} from "./main-table-surface";

const meta = {
  title: "Work Management/Table/Main Table",
  parameters: { layout: "fullscreen" },
} satisfies Meta;
export default meta;
type Story = StoryObj;

/**
 * The surface with the state the container would hold (selection, collapsed
 * groups, "hide finished"), so the story is usable by pointer and keyboard.
 */
function TableSurfaceStory({ base }: { base: MainTableSurfaceProps }) {
  const [selected, setSelected] = useState<ReadonlySet<string>>(
    base.selectedIds,
  );
  const [collapsed, setCollapsed] = useState<ReadonlySet<string>>(
    base.collapsedGroupIds,
  );
  const [notice, setNotice] = useState(base.archivedNotice ?? null);
  const allIds = base.groups.flatMap((group) => group.rows.map((r) => r.id));

  return (
    <div className="h-[820px] min-h-0 bg-background">
      <MainTableSurface
        {...base}
        selectedIds={selected}
        collapsedGroupIds={collapsed}
        archivedNotice={
          notice ? { ...notice, onClose: () => setNotice(null) } : null
        }
        onRowSelectedChange={(id, isSelected) =>
          setSelected((current) => {
            const next = new Set(current);
            if (isSelected) next.add(id);
            else next.delete(id);
            return next;
          })
        }
        onSelectAllChange={(selectAll) =>
          setSelected(selectAll ? new Set(allIds) : new Set())
        }
        onClearSelection={() => setSelected(new Set())}
        onGroupToggle={(id) =>
          setCollapsed((current) => {
            const next = new Set(current);
            if (next.has(id)) next.delete(id);
            else next.add(id);
            return next;
          })
        }
      />
    </div>
  );
}

/** Board binh-thuong: finished items hidden, sorted by due date. */
export const Default: Story = {
  render: () => <TableSurfaceStory base={erpTableProps()} />,
  tags: ["fui-surface--wm.table.main", "fui-state--Default"],
};

/** Board chon-nhieu: two rows selected, the bulk bar over the bottom edge. */
export const SelectedRows: Story = {
  render: () => <TableSurfaceStory base={erpTableSelectedProps()} />,
  tags: ["fui-surface--wm.table.main", "fui-state--Default"],
};

/** Board sua-o: the status list of one row is open. */
export const EditingCell: Story = {
  render: () => <TableSurfaceStory base={erpTableEditingProps()} />,
  tags: ["fui-surface--wm.table.main", "fui-state--Default"],
};

/** Board da-luu-tru: two items archived, "Hoàn tác" offered. */
export const Archived: Story = {
  render: () => <TableSurfaceStory base={erpTableArchivedProps()} />,
  tags: ["fui-surface--wm.table.main", "fui-state--Default"],
};

/** Board khong-ket-qua: the filters leave nothing to show. */
export const NoResults: Story = {
  render: () => <TableSurfaceStory base={erpTableNoResultsProps()} />,
  tags: ["fui-surface--wm.table.main", "fui-state--Empty"],
};

/** A new board: the groups exist and hold no item. */
export const Empty: Story = {
  render: () => <TableSurfaceStory base={erpTableEmptyProps()} />,
  tags: ["fui-surface--wm.table.main", "fui-state--Empty"],
};

/** Density gon: 32 px rows. */
export const Compact: Story = {
  render: () => (
    <TableSurfaceStory base={erpTableSelectedProps({ density: "compact" })} />
  ),
  tags: ["fui-surface--wm.table.main", "fui-state--HighDensity"],
};

export const EdgeData: Story = {
  render: () => <TableSurfaceStory base={erpTableEdgeProps()} />,
  tags: ["fui-surface--wm.table.main", "fui-state--EdgeData"],
};

export const HighDensity: Story = {
  render: () => <TableSurfaceStory base={erpTableDenseProps()} />,
  tags: ["fui-surface--wm.table.main", "fui-state--HighDensity"],
};

/** Board chi-xem: no status edit, no add, no archive. */
export const ReadOnly: Story = {
  render: () => (
    <TableSurfaceStory base={erpTableReadOnlyProps({ hideDone: false })} />
  ),
  tags: ["fui-surface--wm.table.main", "fui-state--ReadOnly"],
};
