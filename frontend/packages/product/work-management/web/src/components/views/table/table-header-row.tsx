import { ArrowDown, ArrowUp } from "lucide-react";
import { cn } from "@notrelix/ui-web";
import type { TableSort } from "@notrelix/work-management-core";
import type { MainTableCopy } from "./main-table.copy";
import {
  stickyOffsets,
  tableGridTemplate,
  type TableColumnSpec,
} from "./table-columns";
import { TableCheckbox } from "./table-parts";

const HEAD_CELL =
  "relative box-border flex h-9 min-w-0 items-center gap-1.5 border-b border-line-strong bg-table-group px-2.5 text-caption font-semibold whitespace-nowrap text-meta-foreground";

/**
 * The sticky header row: the select-all checkbox, then one cell per column.
 * The sorted column reads in the ink colour with an arrow; the key, title and
 * selection cells stay in place while the rest scrolls sideways.
 */
export function TableHeaderRow({
  copy,
  columns,
  sort,
  allChecked,
  canSelect,
  onToggleAll,
}: {
  copy: MainTableCopy;
  columns: readonly TableColumnSpec[];
  sort: TableSort;
  allChecked: boolean | "mixed";
  canSelect: boolean;
  onToggleAll: (selectAll: boolean) => void;
}) {
  const offsets = stickyOffsets(columns);
  return (
    <div
      role="row"
      className="sticky top-0 z-3 grid min-w-full"
      style={{ gridTemplateColumns: tableGridTemplate(columns) }}
    >
      <div
        role="columnheader"
        className={cn(HEAD_CELL, "sticky left-0 z-2 pr-0 pl-3.5")}
      >
        <TableCheckbox
          checked={allChecked}
          label={copy.header.selectAll}
          disabled={!canSelect}
          onChange={onToggleAll}
        />
      </div>
      {columns.map((column) => {
        const sorted = sort.key === column.id;
        const sticky =
          column.id === "key"
            ? { left: offsets.key }
            : column.id === "title"
              ? { left: offsets.title }
              : undefined;
        const Icon = column.Icon;
        const Arrow = sort.direction === "asc" ? ArrowUp : ArrowDown;
        return (
          <div
            key={column.id}
            role="columnheader"
            aria-sort={
              sorted
                ? sort.direction === "asc"
                  ? "ascending"
                  : "descending"
                : undefined
            }
            style={sticky}
            className={cn(
              HEAD_CELL,
              sticky && "sticky z-2",
              column.id === "title" && "border-r border-r-line-strong",
              sorted && "text-foreground",
            )}
          >
            {Icon ? (
              <Icon
                className="size-3.25 text-meta-foreground"
                aria-hidden="true"
              />
            ) : null}
            {copy.columns[column.id]}
            {sorted ? (
              <Arrow
                className="size-3.25 text-state-selected-foreground"
                aria-hidden="true"
              />
            ) : null}
          </div>
        );
      })}
    </div>
  );
}
