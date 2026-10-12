import { Filter } from "lucide-react";
import {
  Button,
  DropdownMenu,
  DropdownMenuCheckboxItem,
  DropdownMenuContent,
  DropdownMenuGroup,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@notrelix/ui-web";
import {
  KANBAN_FILTER_PRIORITIES,
  type KanbanFilterState,
} from "./kanban-filter";
import type { KanbanToolbarCopy } from "./kanban-toolbar.copy";

/** One choosable filter value: a status option, a person or a label. */
export interface KanbanFilterOption {
  id: string;
  name: string;
}

export interface KanbanToolbarFilterOptions {
  statuses: readonly KanbanFilterOption[];
  assignees: readonly KanbanFilterOption[];
  labels: readonly KanbanFilterOption[];
}

type ListFacet = "statusOptionIds" | "assigneeIds" | "labelIds" | "priorities";

function toggled<T extends string>(values: readonly T[], value: T): T[] {
  return values.includes(value)
    ? values.filter((current) => current !== value)
    : [...values, value];
}

/**
 * The "Lọc" menu: checkable values grouped by Trạng thái, Ưu tiên, Người phụ
 * trách and Nhãn, then Quá hạn. Each toggle emits the whole next filter and
 * keeps the menu open for the next choice.
 */
export function KanbanFilterMenu({
  copy,
  filter,
  options,
  onFilterChange,
}: {
  copy: KanbanToolbarCopy["filter"];
  filter: KanbanFilterState;
  options: KanbanToolbarFilterOptions;
  onFilterChange: (filter: KanbanFilterState) => void;
}) {
  const toggle = (facet: ListFacet, value: string) => {
    if (facet === "priorities") {
      const priority = KANBAN_FILTER_PRIORITIES.find((p) => p === value);
      if (!priority) return;
      onFilterChange({
        ...filter,
        priorities: toggled(filter.priorities, priority),
      });
      return;
    }
    onFilterChange({ ...filter, [facet]: toggled(filter[facet], value) });
  };

  const group = (
    heading: string,
    facet: ListFacet,
    values: readonly KanbanFilterOption[],
  ) => (
    <DropdownMenuGroup>
      <DropdownMenuLabel className="text-xs font-medium text-muted-foreground">
        {heading}
      </DropdownMenuLabel>
      {values.length === 0 ? (
        <p className="px-2 py-1.5 text-sm text-muted-foreground">
          {copy.noOptions}
        </p>
      ) : (
        values.map((option) => (
          <DropdownMenuCheckboxItem
            key={option.id}
            checked={(filter[facet] as readonly string[]).includes(option.id)}
            onCheckedChange={() => toggle(facet, option.id)}
            onSelect={(event) => event.preventDefault()}
          >
            <span className="min-w-0 truncate">{option.name}</span>
          </DropdownMenuCheckboxItem>
        ))
      )}
    </DropdownMenuGroup>
  );

  const priorities = KANBAN_FILTER_PRIORITIES.map((priority) => ({
    id: priority,
    name: copy.priorities[priority],
  }));

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button variant="ghost" size="sm" className="text-muted-foreground">
          <Filter className="size-4" aria-hidden="true" />
          {copy.trigger}
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="start" className="w-64">
        {group(copy.status, "statusOptionIds", options.statuses)}
        <DropdownMenuSeparator />
        {group(copy.priority, "priorities", priorities)}
        <DropdownMenuSeparator />
        {group(copy.assignee, "assigneeIds", options.assignees)}
        <DropdownMenuSeparator />
        {group(copy.label, "labelIds", options.labels)}
        <DropdownMenuSeparator />
        <DropdownMenuCheckboxItem
          checked={filter.overdue}
          onCheckedChange={() =>
            onFilterChange({ ...filter, overdue: !filter.overdue })
          }
          onSelect={(event) => event.preventDefault()}
        >
          {copy.overdue}
        </DropdownMenuCheckboxItem>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
