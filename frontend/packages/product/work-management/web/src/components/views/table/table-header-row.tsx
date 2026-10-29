import { ArrowDown, ArrowUp } from "lucide-react";
import { cn } from "@notrelix/ui-web";
import type { TableSort } from "@notrelix/work-management-core";
import { fillTableCopy, type MainTableCopy } from "./main-table.copy";
import { TableColumnResizeHandle } from "./table-column-resize";
import {
  columnWidth,
  type TableColumnSpec,
  type TableColumnWidths,
} from "./table-columns";
import { TableCheckbox } from "./table-parts";

const HEAD_CELL =
  "relative box-border flex h-9 min-w-0 items-center gap-1.5 border-b border-line-strong bg-table-group px-2.5 text-caption font-semibold whitespace-nowrap text-meta-foreground";

/**
 * The header row, sticky at the top while the rows scroll: the select-all
 * checkbox, then one cell per column. The sorted column reads in the ink
 * colour with an arrow. Every column ends in a resize handle.
 */
export function TableHeaderRow({
  copy,
  columns,
  widths,
  sort,
  allChecked,
  canSelect,
  onToggleAll,
  onWidthPreview,
  onWidthCommit,
  onWidthReset,
}: {
  copy: MainTableCopy;
  columns: readonly TableColumnSpec[];
  /** Effective widths (saved widths with the live drag applied). */
  widths: TableColumnWidths;
  /** The effective sort the header marks; none when the member sorts by a field without a column. */
  sort: TableSort | null;
  allChecked: boolean | "mixed";
  canSelect: boolean;
  onToggleAll: (selectAll: boolean) => void;
  onWidthPreview: (columnId: string, width: number | null) => void;
  onWidthCommit: (columnId: string, width: number) => void;
  onWidthReset: (columnId: string) => void;
}) {
  return (
    <div
      role="row"
      className="sticky top-0 z-3 grid min-w-full"
      style={{ gridTemplateColumns: "var(--table-grid)" }}
    >
      <div role="columnheader" className={cn(HEAD_CELL, "pr-0 pl-3.5")}>
        <TableCheckbox
          checked={allChecked}
          label={copy.header.selectAll}
          disabled={!canSelect}
          onChange={onToggleAll}
        />
      </div>
      {columns.map((column) => {
        const sorted = sort?.key === column.id;
        const Icon = column.Icon;
        const Arrow = sort?.direction === "asc" ? ArrowUp : ArrowDown;
        const name = copy.columns[column.id];
        return (
          <div
            key={column.id}
            role="columnheader"
            aria-sort={
              sorted
                ? sort?.direction === "asc"
                  ? "ascending"
                  : "descending"
                : undefined
            }
            className={cn(HEAD_CELL, sorted && "text-foreground")}
          >
            {Icon ? (
              <Icon
                className="size-3.25 flex-none text-meta-foreground"
                aria-hidden="true"
              />
            ) : null}
            <span className="min-w-0 truncate">{name}</span>
            {sorted ? (
              <Arrow
                className="size-3.25 flex-none text-state-selected-foreground"
                aria-hidden="true"
              />
            ) : null}
            <TableColumnResizeHandle
              label={fillTableCopy(copy.header.resize, { name })}
              width={columnWidth(column, widths)}
              onPreview={(width) => onWidthPreview(column.id, width)}
              onCommit={(width) => onWidthCommit(column.id, width)}
              onReset={() => onWidthReset(column.id)}
            />
          </div>
        );
      })}
    </div>
  );
}
