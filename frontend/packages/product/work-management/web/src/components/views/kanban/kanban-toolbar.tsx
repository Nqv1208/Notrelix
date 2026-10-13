import { Plus, Search, X } from "lucide-react";
import {
  Button,
  cn,
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuRadioGroup,
  DropdownMenuRadioItem,
  DropdownMenuTrigger,
} from "@notrelix/ui-web";
import {
  KANBAN_FILTER_PRIORITIES,
  emptyKanbanFilter,
  type KanbanFilterState,
} from "./kanban-filter";
import {
  KanbanFilterMenu,
  type KanbanFilterOption,
  type KanbanToolbarFilterOptions,
} from "./kanban-filter-menu";
import {
  kanbanOutlineButtonClass,
  kanbanPillClass,
  kanbanPrimaryButtonClass,
} from "./kanban-chrome";
import type { KanbanToolbarCopy } from "./kanban-toolbar.copy";

/** A field the board can be grouped by: the Status field or a Select field. */
export interface KanbanGroupByField {
  id: string;
  name: string;
  kind: "status" | "select";
}

export interface KanbanToolbarProps {
  copy: KanbanToolbarCopy;
  searchQuery: string;
  onSearchChange: (query: string) => void;
  filter: KanbanFilterState;
  filterOptions: KanbanToolbarFilterOptions;
  /** Emits the whole next filter (serialise it with `serializeKanbanFilter`). */
  onFilterChange: (filter: KanbanFilterState) => void;
  groupByFields: readonly KanbanGroupByField[];
  /** The grouping field; null means the view default (the Status field). */
  groupByFieldId: string | null;
  onGroupByChange: (fieldId: string) => void;
  /** False for a viewer who cannot add items: the add button is not shown. */
  canAdd: boolean;
  onAddItem: () => void;
  /**
   * Set when search and filter leave no item on the board: renders the
   * no-results bar with the number of items they hide.
   */
  noResults?: { hiddenCount: number } | null;
}

function fill(
  message: string,
  values: Readonly<Record<string, string | number>>,
) {
  return message.replace(/\{\{(\w+)\}\}/g, (match, key: string) =>
    key in values ? String(values[key]) : match,
  );
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

/**
 * One readable segment per selected value, Quá hạn first as in the mockup
 * ("Quá hạn · Ưu tiên Thấp"). Ids the board no longer has are left out.
 */
function filterSegments(
  copy: KanbanToolbarCopy["filter"],
  filter: KanbanFilterState,
  options: KanbanToolbarFilterOptions,
): string[] {
  const { chip } = copy;
  return [
    ...(filter.overdue ? [chip.overdue] : []),
    ...namesOf(options.statuses, filter.statusOptionIds).map((name) =>
      fill(chip.status, { name }),
    ),
    ...KANBAN_FILTER_PRIORITIES.filter((priority) =>
      filter.priorities.includes(priority),
    ).map((priority) =>
      fill(chip.priority, { name: copy.priorities[priority] }),
    ),
    ...namesOf(options.assignees, filter.assigneeIds).map((name) =>
      fill(chip.assignee, { name }),
    ),
    ...namesOf(options.labels, filter.labelIds).map((name) =>
      fill(chip.label, { name }),
    ),
  ];
}

function currentGroupByField(
  fields: readonly KanbanGroupByField[],
  fieldId: string | null,
): KanbanGroupByField | undefined {
  return (
    fields.find((field) => field.id === fieldId) ??
    fields.find((field) => field.kind === "status") ??
    fields[0]
  );
}

/**
 * Kanban toolbar (pure surface): search, the "Lọc" menu with a removable
 * summary chip, the "Nhóm theo" field menu and "Thêm công việc", plus the
 * no-results bar under it when nothing matches.
 */
export function KanbanToolbar({
  copy,
  searchQuery,
  onSearchChange,
  filter,
  filterOptions,
  onFilterChange,
  groupByFields,
  groupByFieldId,
  onGroupByChange,
  canAdd,
  onAddItem,
  noResults,
}: KanbanToolbarProps) {
  const segments = filterSegments(copy.filter, filter, filterOptions);
  const groupBy = currentGroupByField(groupByFields, groupByFieldId);

  return (
    <div data-kanban-toolbar="">
      <div className="flex flex-wrap items-center gap-1.5 py-2.5">
        <label className="flex h-8 w-50 max-w-full items-center gap-2 rounded-[7px] border border-line-strong bg-popover px-2.5 text-meta-foreground focus-within:ring-2 focus-within:ring-focus-ring">
          <Search className="size-4 shrink-0" aria-hidden="true" />
          <input
            type="search"
            value={searchQuery}
            onChange={(event) => onSearchChange(event.target.value)}
            placeholder={copy.search.placeholder}
            aria-label={copy.search.label}
            className="min-w-0 flex-1 bg-transparent text-compact text-foreground outline-none placeholder:text-meta-foreground"
          />
        </label>

        <KanbanFilterMenu
          copy={copy.filter}
          filter={filter}
          options={filterOptions}
          onFilterChange={onFilterChange}
        />

        {segments.length > 0 ? (
          <span className="inline-flex h-8 min-w-0 max-w-full items-center rounded-[7px] bg-state-selected pl-2.5 text-compact font-semibold text-state-selected-foreground">
            <span className="min-w-0 truncate">
              {segments.join(copy.separator)}
            </span>
            <button
              type="button"
              className="grid h-8 w-7 shrink-0 place-items-center rounded-r-[7px] hover:bg-state-hover-strong focus-visible:outline-2 focus-visible:outline-ring"
              aria-label={fill(copy.filter.clear, {
                filters: segments.join(copy.filter.listSeparator),
              })}
              onClick={() => onFilterChange(emptyKanbanFilter())}
            >
              <X className="size-3.25" aria-hidden="true" />
            </button>
          </span>
        ) : null}

        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button
              variant="ghost"
              size="sm"
              className={cn("min-w-0", kanbanPillClass)}
              disabled={!groupBy}
            >
              {copy.groupBy.label}
              {groupBy ? (
                <>
                  {" "}
                  <b className="max-w-48 truncate font-semibold text-foreground">
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

        <span className="flex-1" />

        {canAdd ? (
          <Button
            size="sm"
            className={kanbanPrimaryButtonClass}
            onClick={onAddItem}
          >
            <Plus className="size-4" aria-hidden="true" />
            {copy.addItem}
          </Button>
        ) : null}
      </div>

      {noResults ? (
        <div
          role="status"
          className="mb-2 flex flex-wrap items-center gap-3 rounded-[10px] border border-line bg-popover px-3 py-2.5 dark:bg-card text-compact text-muted-foreground"
        >
          <span>
            <b className="font-semibold text-foreground">
              {copy.noResults.title}
            </b>
            {copy.separator}
            {fill(
              noResults.hiddenCount === 1
                ? copy.noResults.hidden_one
                : copy.noResults.hidden_other,
              { count: noResults.hiddenCount },
            )}
          </span>
          <span className="flex-1" />
          <Button
            variant="outline"
            size="sm"
            className={kanbanOutlineButtonClass}
            onClick={() => {
              onFilterChange(emptyKanbanFilter());
              if (searchQuery !== "") onSearchChange("");
            }}
          >
            {copy.noResults.clear}
          </Button>
        </div>
      ) : null}
    </div>
  );
}
