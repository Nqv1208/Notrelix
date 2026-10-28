import type { ReactNode } from "react";
import { ArrowUpDown, Columns3, Plus, Search, X } from "lucide-react";
import {
  Button,
  cn,
  DropdownMenu,
  DropdownMenuCheckboxItem,
  DropdownMenuContent,
  DropdownMenuLabel,
  DropdownMenuRadioGroup,
  DropdownMenuRadioItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@notrelix/ui-web";
import {
  TABLE_SORT_KEYS,
  type TableDensity,
  type TableSort,
} from "@notrelix/work-management-core";
import {
  emptyKanbanFilter,
  KANBAN_FILTER_PRIORITIES,
  type KanbanFilterState,
} from "../kanban/kanban-filter";
import {
  kanbanPillClass,
  kanbanPrimaryButtonClass,
} from "../kanban/kanban-chrome";
import {
  KanbanFilterMenu,
  type KanbanFilterOption,
  type KanbanToolbarFilterOptions,
} from "../kanban/kanban-filter-menu";
import type { KanbanGroupByField } from "../kanban/kanban-toolbar";
import { fillTableCopy, type MainTableCopy } from "./main-table.copy";
import { TABLE_COLUMNS, visibleTableColumns } from "./table-columns";

export interface MainTableToolbarProps {
  copy: MainTableCopy;
  searchQuery: string;
  onSearchChange: (query: string) => void;
  /** "Trạng thái: chưa xong" is on: finished items are left out. */
  hideDone: boolean;
  onShowDone: () => void;
  filter: KanbanFilterState;
  filterOptions: KanbanToolbarFilterOptions;
  /** Emits the whole next filter (serialise it with `serializeKanbanFilter`). */
  onFilterChange: (filter: KanbanFilterState) => void;
  groupByFields: readonly KanbanGroupByField[];
  /** The grouping field; null means the view default (the Status field). */
  groupByFieldId: string | null;
  onGroupByChange: (fieldId: string) => void;
  sort: TableSort;
  onSortChange: (sort: TableSort) => void;
  hiddenColumnIds: readonly string[];
  onColumnVisibleChange: (columnId: string, visible: boolean) => void;
  density: TableDensity;
  onDensityChange: (density: TableDensity) => void;
  /** False for a viewer who cannot add items: the add button is not shown. */
  canAdd: boolean;
  onAddItem: () => void;
  /** The saved-filter controls ("Lưu bộ lọc", "Bộ lọc đã lưu"), after "Lọc". */
  savedFilters?: ReactNode;
}

type Facet = keyof KanbanFilterState;

interface FilterChip {
  facet: Facet;
  text: string;
}

function namesOf(
  options: readonly KanbanFilterOption[],
  ids: readonly string[],
): string[] {
  return ids.flatMap((id) => {
    const option = options.find((candidate) => candidate.id === id);
    return option ? [option.name] : [];
  });
}

/** One chip per facet that has a value; ids the board no longer has are left out. */
function filterChips(
  copy: MainTableCopy["toolbar"],
  filter: KanbanFilterState,
  options: KanbanToolbarFilterOptions,
): FilterChip[] {
  const chips: FilterChip[] = [];
  const list = (facet: Facet, template: string, names: string[]) => {
    if (names.length > 0)
      chips.push({
        facet,
        text: fillTableCopy(template, { names: names.join(", ") }),
      });
  };
  list(
    "statusOptionIds",
    copy.chip.status,
    namesOf(options.statuses, filter.statusOptionIds),
  );
  list(
    "priorities",
    copy.chip.priority,
    KANBAN_FILTER_PRIORITIES.filter((p) => filter.priorities.includes(p)).map(
      (p) => copy.filter.priorities[p],
    ),
  );
  list(
    "assigneeIds",
    copy.chip.assignee,
    namesOf(options.assignees, filter.assigneeIds),
  );
  list("labelIds", copy.chip.label, namesOf(options.labels, filter.labelIds));
  if (filter.overdue) chips.push({ facet: "overdue", text: copy.chip.overdue });
  return chips;
}

function clearedFacet(filter: KanbanFilterState, facet: Facet) {
  const next = emptyKanbanFilter();
  return { ...filter, [facet]: next[facet] };
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
  filter,
  filterOptions,
  onFilterChange,
  groupByFields,
  groupByFieldId,
  onGroupByChange,
  sort,
  onSortChange,
  hiddenColumnIds,
  onColumnVisibleChange,
  density,
  onDensityChange,
  canAdd,
  onAddItem,
  savedFilters,
}: MainTableToolbarProps) {
  const t = copy.toolbar;
  const chips = filterChips(t, filter, filterOptions);
  const groupBy =
    groupByFields.find((field) => field.id === groupByFieldId) ??
    groupByFields.find((field) => field.kind === "status") ??
    groupByFields[0];
  const shownColumns = visibleTableColumns(hiddenColumnIds).length;
  const arrow = sort.direction === "asc" ? "↑" : "↓";

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
      {chips.map((chip) => (
        <RemovableChip
          key={chip.facet}
          text={chip.text}
          clearLabel={fillTableCopy(t.chipClear, { filter: chip.text })}
          onClear={() => onFilterChange(clearedFacet(filter, chip.facet))}
          data-chip={chip.facet}
        />
      ))}

      <KanbanFilterMenu
        copy={t.filter}
        filter={filter}
        options={filterOptions}
        onFilterChange={onFilterChange}
      />

      {savedFilters}

      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <Button variant="ghost" size="sm" className={kanbanPillClass}>
            <ArrowUpDown className="size-4" aria-hidden="true" />
            {t.sort.label}{" "}
            <b className="font-semibold leading-[1.45] text-foreground">
              {copy.columns[sort.key]} {arrow}
            </b>
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="start" className="w-56">
          <DropdownMenuLabel className="text-xs font-medium text-muted-foreground">
            {t.sort.byColumn}
          </DropdownMenuLabel>
          <DropdownMenuRadioGroup
            value={sort.key}
            onValueChange={(key) => {
              const next = TABLE_SORT_KEYS.find(
                (candidate) => candidate === key,
              );
              if (next && next !== sort.key)
                onSortChange({ key: next, direction: sort.direction });
            }}
          >
            {TABLE_COLUMNS.map((column) => (
              <DropdownMenuRadioItem key={column.id} value={column.id}>
                {copy.columns[column.id]}
              </DropdownMenuRadioItem>
            ))}
          </DropdownMenuRadioGroup>
          <DropdownMenuSeparator />
          <DropdownMenuRadioGroup
            value={sort.direction}
            onValueChange={(direction) => {
              if (direction === "asc" || direction === "desc")
                if (direction !== sort.direction)
                  onSortChange({ key: sort.key, direction });
            }}
          >
            <DropdownMenuRadioItem value="asc">
              {t.sort.ascendingLabel} ↑
            </DropdownMenuRadioItem>
            <DropdownMenuRadioItem value="desc">
              {t.sort.descendingLabel} ↓
            </DropdownMenuRadioItem>
          </DropdownMenuRadioGroup>
        </DropdownMenuContent>
      </DropdownMenu>

      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <Button
            variant="ghost"
            size="sm"
            className={cn("min-w-0", kanbanPillClass)}
            disabled={!groupBy}
          >
            {t.groupBy.label}
            {groupBy ? (
              <>
                {" "}
                <b className="max-w-48 truncate font-semibold leading-[1.45] text-foreground">
                  {groupBy.name}
                </b>
              </>
            ) : null}
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="start" className="w-56">
          <DropdownMenuRadioGroup
            value={groupBy?.id ?? ""}
            onValueChange={(fieldId) => {
              if (fieldId !== groupBy?.id) onGroupByChange(fieldId);
            }}
          >
            {groupByFields.map((field) => (
              <DropdownMenuRadioItem key={field.id} value={field.id}>
                <span className="min-w-0 truncate">{field.name}</span>
              </DropdownMenuRadioItem>
            ))}
          </DropdownMenuRadioGroup>
        </DropdownMenuContent>
      </DropdownMenu>

      <span aria-hidden="true" className="mx-1 h-5 w-px bg-border" />

      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <Button variant="ghost" size="sm" className={kanbanPillClass}>
            <Columns3 className="size-4" aria-hidden="true" />
            {t.columns.label}{" "}
            <b className="font-semibold leading-[1.45] text-foreground">
              {shownColumns}/{TABLE_COLUMNS.length}
            </b>
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="start" className="w-56">
          <DropdownMenuLabel className="text-xs font-medium text-muted-foreground">
            {t.columns.heading}
          </DropdownMenuLabel>
          {TABLE_COLUMNS.map((column) => (
            <DropdownMenuCheckboxItem
              key={column.id}
              checked={column.locked || !hiddenColumnIds.includes(column.id)}
              disabled={column.locked}
              onCheckedChange={(checked) =>
                onColumnVisibleChange(column.id, Boolean(checked))
              }
              onSelect={(event) => event.preventDefault()}
            >
              {copy.columns[column.id]}
            </DropdownMenuCheckboxItem>
          ))}
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
