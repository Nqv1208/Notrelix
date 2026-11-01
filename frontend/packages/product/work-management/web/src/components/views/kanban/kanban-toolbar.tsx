import type { ReactNode } from "react";
import { Plus, Search } from "lucide-react";
import { Button } from "@notrelix/ui-web";
import {
  kanbanOutlineButtonClass,
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
  /** The view controls: filter, sort, group, swimlane, personal-state chip and saved filters. */
  viewBar: ReactNode;
  /** Extra controls before the add button (the face fields popover). */
  endActions?: ReactNode;
  /** False for a viewer who cannot add items: the add button is not shown. */
  canAdd: boolean;
  onAddItem: () => void;
  /**
   * Set when search and filter leave no item on the board: renders the
   * no-results bar with the number of items they hide.
   */
  noResults?: { hiddenCount: number } | null;
  /** Clears the filter conditions (the no-results bar also clears the search). */
  onClearFilter: () => void;
}

function fill(
  message: string,
  values: Readonly<Record<string, string | number>>,
) {
  return message.replace(/\{\{(\w+)\}\}/g, (match, key: string) =>
    key in values ? String(values[key]) : match,
  );
}

/**
 * Kanban toolbar (pure surface): search, the view controls slot and "Thêm
 * công việc", plus the no-results bar under it when nothing matches.
 */
export function KanbanToolbar({
  copy,
  searchQuery,
  onSearchChange,
  viewBar,
  endActions,
  canAdd,
  onAddItem,
  noResults,
  onClearFilter,
}: KanbanToolbarProps) {
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

        {viewBar}

        <span className="flex-1" />

        {endActions}

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
              onClearFilter();
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
