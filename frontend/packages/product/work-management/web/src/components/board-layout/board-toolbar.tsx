import { useContext } from "react";
import { cn } from "@notrelix/ui-web";
import type { BoardHeaderCopy } from "./board-header.copy";
import { BoardHeaderCopyContext } from "./board-header-copy-context";
import { BoardHeaderDataContext } from "./board-header-data-context";
import {
  BoardHeaderSurface,
  type BoardHeaderData,
} from "./board-header-surface";
import { ViewTabs, AddViewMenu } from "./view-tabs";

interface BoardToolbarProps {
  boardTitle: string;
  activeView: string;
  onViewChange: (viewType: string) => void;
  /**
   * Header data (Space crumb, access summary, actions). Defaults to the data
   * the board route provides through `BoardHeaderDataContext`.
   */
  header?: BoardHeaderData;
  /**
   * Header copy. Defaults to the copy provided through
   * `BoardHeaderCopyContext` by the board route.
   */
  copy?: BoardHeaderCopy;
}

/**
 * The board header: the title row (see `BoardHeaderSurface`), then the view
 * tabs on their own row with the icon-only Add view menu. Search and filters
 * belong to each view's own toolbar. One DOM order at every width, so focus
 * order equals reading order.
 */
export function BoardToolbar({
  boardTitle,
  activeView,
  onViewChange,
  header: headerProp,
  copy: copyProp,
}: BoardToolbarProps) {
  const contextCopy = useContext(BoardHeaderCopyContext);
  const contextHeader = useContext(BoardHeaderDataContext);
  const copy = copyProp ?? contextCopy;
  const header = headerProp ?? contextHeader;
  return (
    <div className="flex flex-col">
      <BoardHeaderSurface title={boardTitle} copy={copy} {...header} />
      <div
        className={cn(
          "flex items-end gap-0.5 border-b border-line px-4 pt-2.5 sm:px-6",
          // Final-Board sizes the tab strip 2 px taller than the Table,
          // Calendar and Timeline mockups do (its tab is content-box).
          activeView === "kanban" ? "min-h-12" : "min-h-11.5",
        )}
      >
        <div className="-mb-px min-w-0 overflow-x-auto">
          <ViewTabs
            activeView={activeView}
            onViewChange={onViewChange}
            copy={copy}
          />
        </div>
        <AddViewMenu onAddView={onViewChange} copy={copy} />
      </div>
    </div>
  );
}
