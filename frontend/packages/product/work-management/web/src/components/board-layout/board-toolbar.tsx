import { useContext } from "react";
import { cn } from "@notrelix/ui-web";
import type { BoardHeaderCopy } from "./board-header.copy";
import { BoardHeaderCopyContext } from "./board-header-copy-context";
import { BoardHeaderDataContext } from "./board-header-data-context";
import {
  BoardHeaderSurface,
  type BoardHeaderData,
} from "./board-header-surface";
import { ViewTabs, AddViewMenu, type ViewTabSavedView } from "./view-tabs";

interface BoardToolbarProps {
  boardTitle: string;
  activeView: string;
  onViewChange: (viewKey: string) => void;
  /** The board's saved views, one tab each. */
  savedViews?: readonly ViewTabSavedView[];
  /** Makes a saved view of a layout; absent: the layout is only shown. */
  onAddView?: (viewType: string) => void;
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
  /**
   * Opens the settings of the active view. Given: the header's Settings
   * button is enabled and calls it (it wins over the header data's own).
   */
  onOpenSettings?: () => void;
  /** Opens the board's deleted items from the More options menu. */
  onOpenTrash?: () => void;
  onOpenArchive?: () => void;
  /** A notice between the title row and the view tabs (the live connection). */
  notice?: React.ReactNode;
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
  savedViews,
  onAddView,
  header: headerProp,
  copy: copyProp,
  onOpenSettings,
  onOpenTrash,
  onOpenArchive,
  notice,
}: BoardToolbarProps) {
  const contextCopy = useContext(BoardHeaderCopyContext);
  const contextHeader = useContext(BoardHeaderDataContext);
  const copy = copyProp ?? contextCopy;
  const header = headerProp ?? contextHeader;
  // Only Final-Board's header carries the "More options" button; the
  // Table, Calendar and Timeline headers end at Settings and Share.
  const activeLayout =
    savedViews?.find((view) => view.id === activeView)?.viewMode ?? activeView;
  const headerForView =
    activeLayout === "kanban" ? header : { ...header, onCopyLink: undefined };
  const headerWithTrash =
    onOpenTrash || onOpenArchive
      ? { ...headerForView, onOpenTrash, onOpenArchive }
      : headerForView;
  return (
    <div className="flex flex-col">
      <BoardHeaderSurface
        title={boardTitle}
        copy={copy}
        {...headerWithTrash}
        {...(onOpenSettings ? { onOpenSettings } : {})}
      />
      {notice}
      <div
        className={cn(
          // One height for every view, so switching views moves nothing: the
          // mockups differ by 2 px (Final-Board's tab is content-box, the
          // others' border-box) and the Board's 48 px is the one used.
          "flex min-h-12 items-end gap-0.5 border-b border-line px-4 pt-2.5 sm:px-6",
        )}
      >
        <div className="-mb-px min-w-0 overflow-x-auto">
          <ViewTabs
            activeView={activeView}
            onViewChange={onViewChange}
            copy={copy}
            savedViews={savedViews}
          />
        </div>
        <AddViewMenu onAddView={onAddView ?? onViewChange} copy={copy} />
      </div>
    </div>
  );
}
