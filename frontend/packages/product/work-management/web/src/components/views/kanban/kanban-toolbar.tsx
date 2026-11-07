import type { ReactNode } from "react";
import { Plus, Search } from "lucide-react";
import {
  Button,
  TABLET_SLOT,
  TabletPortal,
  TabletSlot,
  useMediaQuery,
} from "@notrelix/ui-web";
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
      <div className="flex flex-wrap items-center gap-1.5 py-2.5 tablet:min-h-15 tablet:flex-nowrap tablet:gap-2 tablet:overflow-x-auto tablet:py-2 tablet:[&>*]:shrink-0">
        {/* The tablet row starts with the access chip (the board header portals it in). */}
        <TabletSlot
          slot={TABLET_SLOT.toolbarLead}
          className="hidden items-center gap-2 tablet:flex"
        />
        {compact ? (
          <CompactSearch
            value={searchQuery}
            onChange={onSearchChange}
            label={copy.search.label}
            placeholder={copy.search.placeholder}
          />
        ) : (
          <label className="flex h-8 w-50 tablet:hidden max-w-full items-center gap-2 rounded-[7px] border border-line-strong bg-popover px-2.5 text-meta-foreground focus-within:ring-2 focus-within:ring-focus-ring">
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

        <span className="flex-1 tablet:hidden" />

        {endActions}
        {/* The tablet row ends with the view menu, Add view and the avatar stack (the board header portals them in). */}
        <TabletSlot
          slot={TABLET_SLOT.toolbarTrail}
          className="hidden items-center gap-2 tablet:flex"
        />

        {canAdd ? (
          <>
            <Button
              size="sm"
              className={`${kanbanPrimaryButtonClass} tablet:hidden`}
              onClick={onAddItem}
            >
              <Plus className="size-4" aria-hidden="true" />
              {copy.addItem}
            </Button>
            <TabletPortal slot={TABLET_SLOT.headerAdd}>
              <Button
                size="icon"
                aria-label={copy.addItem}
                className="size-11 shrink-0 rounded-[10px]"
                onClick={onAddItem}
              >
                <Plus className="size-5" aria-hidden="true" />
              </Button>
            </TabletPortal>
          </>
        ) : null}
      </div>
    </div>
  );
}
