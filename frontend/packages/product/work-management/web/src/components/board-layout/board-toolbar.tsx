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
    // One DOM order at every width (title, views, search, actions): on a
    // narrow screen the title and the view strip take a row each and the
    // search and actions wrap below, so focus order equals reading order.
    <div className="flex flex-wrap items-center gap-x-3 gap-y-1 border-b px-3 py-2 sm:px-4">
      <h2 className="min-w-0 basis-full truncate text-sm font-semibold text-foreground sm:basis-auto">
        {boardTitle}
      </h2>

      <div className="hidden h-4 w-px bg-border sm:block" />

      <div className="w-full min-w-0 overflow-x-auto sm:w-auto">
        <ViewTabs
          activeView={activeView}
          onViewChange={onViewChange}
          copy={copy}
        />
      </div>

      {onSearchChange && (
        <div className="relative min-w-32 flex-1 sm:ml-auto sm:w-48 sm:flex-none">
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
        className={`flex items-center gap-1 ${onSearchChange ? "" : "ml-auto"}`}
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
