import type { ReactNode } from "react";
import { Columns3, Plus, Search, X } from "lucide-react";
import {
  Button,
  DropdownMenu,
  DropdownMenuCheckboxItem,
  DropdownMenuContent,
  DropdownMenuLabel,
  DropdownMenuRadioGroup,
  DropdownMenuRadioItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@notrelix/ui-web";
import type {
  FieldDefinition,
  TableDensity,
  TableSort,
} from "@notrelix/work-management-core";
import {
  kanbanPillClass,
  kanbanPrimaryButtonClass,
} from "../kanban/kanban-chrome";
import { fillTableCopy, type MainTableCopy } from "./main-table.copy";
import {
  allTableColumns,
  moveColumnId,
  visibleTableColumns,
} from "./table-columns";

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
  hiddenColumnIds,
  onColumnVisibleChange,
  density,
  onDensityChange,
  canAdd,
  onAddItem,
}: MainTableToolbarProps) {
  const t = copy.toolbar;
  const columnName = (column: ReturnType<typeof allTableColumns>[number]) =>
    column.field?.name ?? copy.columns[column.id as keyof typeof copy.columns];
  const shownColumns = visibleTableColumns(
    hiddenColumnIds,
    customFields,
    columnOrder,
  ).length;
  // The menu lists the columns in their display order.
  const orderedColumns = visibleTableColumns([], customFields, columnOrder);

  return (
    <div
      data-table-toolbar=""
      className="flex flex-wrap items-center gap-1.5 py-2.5"
    >
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

      {hideDone ? (
        <RemovableChip
          text={t.doneChip}
          clearLabel={fillTableCopy(t.chipClear, { filter: t.doneChip })}
          onClear={onShowDone}
          data-chip="done"
        />
      ) : null}
      {viewBar}

      <span aria-hidden="true" className="mx-1 h-5 w-px bg-border" />

      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <Button variant="ghost" size="sm" className={kanbanPillClass}>
            <Columns3 className="size-4" aria-hidden="true" />
            {t.columns.label}{" "}
            <b className="font-semibold leading-[1.45] text-foreground">
              {shownColumns}/{orderedColumns.length}
            </b>
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="start" className="w-56">
          <DropdownMenuLabel className="text-xs font-medium text-muted-foreground">
            {t.columns.heading}
          </DropdownMenuLabel>
          {orderedColumns.map((column) => (
            <DropdownMenuCheckboxItem
              key={column.id}
              checked={column.locked || !hiddenColumnIds.includes(column.id)}
              disabled={column.locked}
              aria-keyshortcuts={
                column.locked ? undefined : "Alt+ArrowUp Alt+ArrowDown"
              }
              onCheckedChange={(checked) =>
                onColumnVisibleChange(column.id, Boolean(checked))
              }
              onSelect={(event) => event.preventDefault()}
              onKeyDown={(event) => {
                // Alt+Up / Alt+Down move the column; the order is saved in the view.
                if (
                  !event.altKey ||
                  (event.key !== "ArrowUp" && event.key !== "ArrowDown")
                )
                  return;
                event.preventDefault();
                const ids = orderedColumns
                  .filter((entry) => entry.id !== "key" && entry.id !== "title")
                  .map((entry) => entry.id);
                const next = moveColumnId(
                  ids,
                  column.id,
                  event.key === "ArrowUp" ? -1 : 1,
                );
                if (next) onColumnOrderChange?.(next);
              }}
            >
              {columnName(column)}
            </DropdownMenuCheckboxItem>
          ))}
          <p className="m-0 px-2 pt-1.5 pb-1 text-crumb text-meta-foreground">
            {t.columns.reorderHint}
          </p>
          <DropdownMenuSeparator />
          <DropdownMenuLabel className="text-xs font-medium text-muted-foreground">
            {t.columns.density}
          </DropdownMenuLabel>
          <DropdownMenuRadioGroup
            value={density}
            onValueChange={(value) => {
              if (value === "comfortable" || value === "compact")
                if (value !== density) onDensityChange(value);
            }}
          >
            <DropdownMenuRadioItem value="comfortable">
              {t.columns.comfortable}
            </DropdownMenuRadioItem>
            <DropdownMenuRadioItem value="compact">
              {t.columns.compact}
            </DropdownMenuRadioItem>
          </DropdownMenuRadioGroup>
        </DropdownMenuContent>
      </DropdownMenu>

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
