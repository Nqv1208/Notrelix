import { ArrowDown, ArrowUp, Lock } from "lucide-react";
import { cn } from "@notrelix/ui-web";
import type { TableSort } from "@notrelix/work-management-core";
import { specOf } from "@notrelix/work-management-core";
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
  canEditFields = false,
  onToggleAll,
  onWidthPreview,
  onWidthCommit,
  onWidthReset,
}: {
  copy: MainTableCopy;
  columns: readonly TableColumnSpec[];
  /** Effective widths (saved widths with the live drag applied). */
  widths: TableColumnWidths;
  sort: TableSort;
  allChecked: boolean | "mixed";
  canSelect: boolean;
  /** The viewer may write field values; without it every field column carries the lock. */
  canEditFields?: boolean;
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
        const sorted = sort.key === column.id;
        const Icon = column.Icon;
        const Arrow = sort.direction === "asc" ? ArrowUp : ArrowDown;
        const field = column.field;
        const computed = field ? specOf(field).computed : false;
        const name =
          field?.name ?? copy.columns[column.id as keyof typeof copy.columns];
        // One lock per read-only column, in the header: calculated, a type the
        // table cannot edit, or a viewer without edit rights.
        const lock = !field
          ? null
          : computed
            ? copy.fields.lockComputed
            : !specOf(field).editable
              ? copy.fields.lockType
              : canEditFields
                ? null
                : copy.fields.lockViewer;
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
            className={cn(HEAD_CELL, sorted && "text-foreground")}
          >
            {computed ? (
              <span
                aria-hidden="true"
                className="inline-flex h-4.5 flex-none items-center rounded-[5px] bg-state-hover-strong px-1.25 font-mono text-micro font-medium text-muted-foreground"
              >
                ƒx
              </span>
            ) : Icon ? (
              <Icon
                className="size-3.25 flex-none text-meta-foreground"
                aria-hidden="true"
              />
            ) : null}
            <span className="min-w-0 truncate">{name}</span>
            {lock ? (
              <span
                role="img"
                aria-label={`${name}: ${lock}`}
                title={lock}
                className="inline-flex flex-none text-meta-foreground"
              >
                <Lock className="size-3" aria-hidden="true" />
              </span>
            ) : null}
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
