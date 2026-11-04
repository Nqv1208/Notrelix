import type { ReactNode } from "react";
import { Plus, Search, Undo2, X } from "lucide-react";
import { Button, useMediaQuery } from "@notrelix/ui-web";
import { CompactSearch } from "../../filters/compact-search";
import type {
  FieldDefinition,
  TableDensity,
  TableSort,
} from "@notrelix/work-management-core";
import { kanbanPrimaryButtonClass } from "../kanban/kanban-chrome";
import { fillTableCopy, type MainTableCopy } from "./main-table.copy";
import { TableColumnsMenu, type TableColumnState } from "./table-columns-menu";

export type { TableColumnState };

export interface MainTableToolbarProps {
  copy: MainTableCopy;
  searchQuery: string;
  onSearchChange: (query: string) => void;
  /** "Trạng thái: chưa xong" is on: finished items are left out. */
  hideDone: boolean;
  onShowDone: () => void;
  /** The member's filter, sort, group and saved-filter controls (`ViewStateBar`), after the search. */
  viewBar: ReactNode;
  /** The effective sort the table header marks: the member's first level, else the shared view's. */
  sort: TableSort | null;
  /** Custom fields shown as columns (hide, order and width apply to them like the built-in ones). */
  customFields?: readonly FieldDefinition[];
  /** The column order in effect (personal, else the shared view's); empty = the default order. */
  columnOrder?: readonly string[];
  onColumnOrderChange?: (order: string[]) => void;
  /** The member's own columns; absent = the toolbar offers no personal state. */
  columnState?: TableColumnState;
  hiddenColumnIds: readonly string[];
  onColumnVisibleChange: (columnId: string, visible: boolean) => void;
  density: TableDensity;
  onDensityChange: (density: TableDensity) => void;
  /** False for a viewer who cannot add items: the add button is not shown. */
  canAdd: boolean;
  onAddItem: () => void;
}

const CHIP_CLASS =
  "inline-flex h-7 min-w-0 max-w-full items-center gap-1 rounded-[7px] bg-state-selected pr-1.5 pl-2.5 text-crumb font-semibold text-state-selected-foreground";

function RemovableChip({
  text,
  clearLabel,
  onClear,
  ...rest
}: {
  text: string;
  clearLabel: string;
  onClear: () => void;
} & React.HTMLAttributes<HTMLSpanElement>) {
  return (
    <span className={CHIP_CLASS} {...rest}>
      <span className="min-w-0 truncate">{text}</span>
      <button
        type="button"
        aria-label={clearLabel}
        onClick={onClear}
        className="grid size-5 shrink-0 cursor-pointer place-items-center rounded-[5px] border-0 bg-transparent p-0 text-inherit hover:bg-state-hover-strong focus-visible:outline-2 focus-visible:outline-ring"
      >
        <X className="size-3.25" aria-hidden="true" />
      </button>
    </span>
  );
}

/**
 * The table toolbar (pure surface): search, the removable filter chips
 * ("Trạng thái: chưa xong" first), "Lọc", "Sắp xếp", "Nhóm theo", "Cột"
 * with the column list and the row height, and "Thêm công việc".
 */
export function MainTableToolbar({
  copy,
  searchQuery,
  onSearchChange,
  hideDone,
  onShowDone,
  viewBar,
  customFields = [],
  columnOrder = [],
  onColumnOrderChange,
  columnState,
  hiddenColumnIds,
  onColumnVisibleChange,
  density,
  onDensityChange,
  canAdd,
  onAddItem,
}: MainTableToolbarProps) {
  const t = copy.toolbar;
  // A phone: the search is an icon button, so the bar keeps Lọc, Cách xem and the saved filters on one row.
  const compact = useMediaQuery("(max-width: 639px)");
  return (
    <div
      data-table-toolbar=""
      className="flex flex-wrap items-center gap-1.5 py-2.5"
    >
      {compact ? (
        <CompactSearch
          value={searchQuery}
          onChange={onSearchChange}
          label={t.search.label}
          placeholder={t.search.placeholder}
        />
      ) : (
        <label className="flex h-8 w-50 max-w-full items-center gap-2 rounded-[7px] border border-line-strong bg-popover px-2.5 text-meta-foreground focus-within:ring-2 focus-within:ring-focus-ring dark:bg-card">
          <Search className="size-4 shrink-0" aria-hidden="true" />
          <input
            type="search"
            value={searchQuery}
            onChange={(event) => onSearchChange(event.target.value)}
            placeholder={t.search.placeholder}
            aria-label={t.search.label}
            className="min-w-0 flex-1 bg-transparent text-compact text-foreground outline-none placeholder:text-meta-foreground"
          />
        </label>
      )}

      {hideDone ? (
        <RemovableChip
          text={t.doneChip}
          clearLabel={fillTableCopy(t.chipClear, { filter: t.doneChip })}
          onClear={onShowDone}
          data-chip="done"
        />
      ) : null}
      {viewBar}

      {columnState?.personal ? (
        <button
          type="button"
          aria-label={t.columns.indicator}
          title={t.columns.indicator}
          onClick={columnState.onReset}
          className="relative grid size-8 flex-none cursor-pointer place-items-center rounded-[7px] border border-transparent bg-state-selected p-0 text-state-selected-foreground focus-visible:outline-2 focus-visible:outline-ring max-md:size-11"
        >
          <Undo2 className="size-4" aria-hidden="true" />
          <span
            aria-hidden="true"
            className="absolute top-1 right-1 size-2 rounded-full bg-primary ring-2 ring-state-selected"
          />
        </button>
      ) : null}

      <span
        aria-hidden="true"
        // The personal-columns toolbar draws 8 px between boxes: 4 px of margin more beside the separator.
        className={`h-5 w-px bg-border max-md:hidden ${columnState?.personal ? "mx-1.5" : "mx-1"}`}
      />

      <TableColumnsMenu
        copy={copy}
        customFields={customFields}
        columnOrder={columnOrder}
        onColumnOrderChange={onColumnOrderChange}
        columnState={columnState}
        hiddenColumnIds={hiddenColumnIds}
        onColumnVisibleChange={onColumnVisibleChange}
        density={density}
        onDensityChange={onDensityChange}
      />

      <span className="flex-1" />

      {canAdd ? (
        <Button
          size="sm"
          className={kanbanPrimaryButtonClass}
          onClick={onAddItem}
        >
          <Plus className="size-4" aria-hidden="true" />
          {t.addItem}
        </Button>
      ) : null}
    </div>
  );
}
