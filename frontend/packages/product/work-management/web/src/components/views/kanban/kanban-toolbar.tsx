import type { ReactNode } from "react";
import { Plus, Search } from "lucide-react";
import { Button, useMediaQuery } from "@notrelix/ui-web";
import { CompactSearch } from "../../filters/compact-search";
import { kanbanPrimaryButtonClass } from "./kanban-chrome";
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
}

/**
 * Kanban toolbar (pure surface): search, the view controls slot and "Thêm
 * công việc". When nothing matches the filter the board shows `FilterNoResults`.
 */
export function KanbanToolbar({
  copy,
  searchQuery,
  onSearchChange,
  viewBar,
  endActions,
  canAdd,
  onAddItem,
}: KanbanToolbarProps) {
  // A phone: the search is an icon button, so the bar keeps Lọc, Cách xem and the saved filters on one row.
  const compact = useMediaQuery("(max-width: 639px)");
  return (
    <div data-kanban-toolbar="">
      <div className="flex flex-wrap items-center gap-1.5 py-2.5">
        {compact ? (
          <CompactSearch
            value={searchQuery}
            onChange={onSearchChange}
            label={copy.search.label}
            placeholder={copy.search.placeholder}
          />
        ) : (
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
        )}

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
    </div>
  );
}
