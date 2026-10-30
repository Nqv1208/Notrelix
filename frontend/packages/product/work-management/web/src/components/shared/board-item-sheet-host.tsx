import { useMemo } from "react";
import { useBoardKanban } from "@notrelix/work-management-state";
import type {
  ItemSheetGroup,
  ItemSheetMark,
} from "../card-detail/task-detail-types";
import { emptyKanbanFilter } from "../views/kanban/kanban-filter";
import { KanbanSelectedItem } from "../views/kanban/kanban-view";
import { BoardViewerContext } from "./board-viewer";
import { useBoardCapabilities } from "./board-capabilities";
import { useItemSheetActions } from "./use-item-sheet-actions";

const NEVER_OVERDUE = () => false;

export interface BoardItemSheetHostProps {
  readonly workspaceId: string;
  readonly boardId: string;
  readonly itemId: string;
  /** The department (Space) mark before the Board name in the sheet's crumb. */
  readonly departmentMark?: ItemSheetMark;
  readonly onClose: () => void;
  /** Gives focus back to the opener as the sheet closes. */
  readonly onRestoreFocus: (itemId: string) => void;
  readonly onOpenFullPage?: (itemId: string) => void;
  /** The signed-in member, for the approval buttons of the sheet. */
  readonly viewerUserId?: string | null;
}

/**
 * The item side sheet for a page that is not a Board view (My work): it reads
 * the Board of the item, what the viewer may do there, and wires the sheet's
 * actions the same way the Calendar and Timeline do. The sheet itself is the
 * shared one; an id that does not resolve closes it.
 */
export function BoardItemSheetHost({
  workspaceId,
  boardId,
  itemId,
  departmentMark,
  onClose,
  onRestoreFocus,
  onOpenFullPage,
  viewerUserId,
}: BoardItemSheetHostProps) {
  const filter = useMemo(() => emptyKanbanFilter(), []);
  const kanban = useBoardKanban(boardId, workspaceId, {
    filter,
    isOverdue: NEVER_OVERDUE,
  });
  const { capabilities } = useBoardCapabilities(boardId, workspaceId);
  const sheet = useItemSheetActions({ boardId, workspaceId, kanban });
  const { board } = kanban;
  const groups = useMemo<ItemSheetGroup[]>(
    () =>
      [...kanban.groups]
        .sort((a, b) => a.position - b.position)
        .map((group) => ({ id: group.id, title: group.title })),
    [kanban.groups],
  );

  if (!board) return null;
  return (
    <BoardViewerContext.Provider value={viewerUserId ?? null}>
      <KanbanSelectedItem
        board={board}
        cardId={itemId}
        capabilities={capabilities}
        departmentMark={departmentMark}
        groups={groups}
        onClose={onClose}
        onRestoreFocus={onRestoreFocus}
        onCopyLink={sheet.onCopyLink}
        onOpenFullPage={onOpenFullPage}
        onMoveToGroup={sheet.onMoveToGroup}
        onUpdateFieldValue={sheet.onUpdateFieldValue}
        onDuplicate={sheet.onDuplicate}
      />
    </BoardViewerContext.Provider>
  );
}
