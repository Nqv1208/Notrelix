import { useContext } from "react";
import { Search, Filter } from "lucide-react";
import { Button, Input } from "@notrelix/ui-web";
import type { BoardHeaderCopy } from "./board-header.copy";
import { BoardHeaderCopyContext } from "./board-header-copy-context";
import { ViewTabs, AddViewMenu } from "./view-tabs";

interface BoardToolbarProps {
  boardTitle: string;
  activeView: string;
  onViewChange: (viewType: string) => void;
  searchQuery?: string;
  onSearchChange?: (query: string) => void;
  /** The filter control is rendered only when a handler is supplied. */
  onFilter?: () => void;
  /**
   * Header copy. Defaults to the copy provided through
   * `BoardHeaderCopyContext` by the board route.
   */
  copy?: BoardHeaderCopy;
}

export function BoardToolbar({
  boardTitle,
  activeView,
  onViewChange,
  searchQuery = "",
  onSearchChange,
  onFilter,
  copy: copyProp,
}: BoardToolbarProps) {
  const contextCopy = useContext(BoardHeaderCopyContext);
  const copy = copyProp ?? contextCopy;
  return (
    // Wraps on narrow screens: title and actions share the first row and the
    // view tabs take the next one, so nothing is pushed past the viewport.
    <div className="flex flex-wrap items-center gap-x-3 gap-y-1 border-b px-3 py-2 sm:px-4">
      <h2 className="order-1 min-w-0 flex-1 truncate text-sm font-semibold text-foreground sm:flex-none">
        {boardTitle}
      </h2>

      <div className="order-2 hidden h-4 w-px bg-border sm:block" />

      <div className="order-4 w-full min-w-0 overflow-x-auto sm:order-3 sm:w-auto">
        <ViewTabs
          activeView={activeView}
          onViewChange={onViewChange}
          copy={copy}
        />
      </div>

      {onSearchChange && (
        <div className="relative order-5 w-full sm:order-4 sm:ml-auto sm:w-48">
          <Search
            className="absolute left-2.5 top-1/2 size-3.5 -translate-y-1/2 text-muted-foreground"
            aria-hidden="true"
          />
          <Input
            placeholder={copy.search}
            aria-label={copy.searchLabel}
            value={searchQuery}
            onChange={(e) => onSearchChange(e.target.value)}
            className="h-7 pl-8 text-xs"
          />
        </div>
      )}

      <div
        className={`order-3 flex items-center gap-1 sm:order-5 ${
          onSearchChange ? "" : "sm:ml-auto"
        }`}
      >
        {onFilter && (
          <Button
            variant="ghost"
            size="icon"
            className="size-7 text-muted-foreground hover:text-foreground"
            aria-label={copy.filter}
            onClick={onFilter}
          >
            <Filter className="size-3.5" aria-hidden="true" />
          </Button>
        )}
        <AddViewMenu onAddView={onViewChange} copy={copy} />
      </div>
    </div>
  );
}
