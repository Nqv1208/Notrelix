import { useState } from "react";
import type { Board, CardDetail } from "@notrelix/work-management-core";
import { TaskDetailPanelSurface } from "../../card-detail/task-detail-panel-surface";
import type { ItemSheetCopy } from "../../card-detail/item-sheet.copy";
import { useItemSheetCopy } from "../../card-detail/use-item-sheet-copy";
import type {
  ItemSheetGroup,
  ItemSheetMark,
  TaskDetailCallbacks,
  TaskDetailCapabilities,
  TaskDetailData,
} from "../../card-detail/task-detail-types";

interface KanbanCardDetailPanelProps {
  board: Board;
  /** The selected card; the sheet stays closed while this is null. */
  card: CardDetail | null;
  detailData: TaskDetailData;
  callbacks: TaskDetailCallbacks;
  /**
   * Called as the sheet closes (unmounts). `preventDefault()` skips the
   * default focus return so the container can put focus on the opener.
   */
  onCloseAutoFocus?: (event: Event) => void;
  /**
   * The viewer's capabilities on this board. Required: the container decides
   * (a commenter gets read-only properties and keeps the composer).
   */
  capabilities: TaskDetailCapabilities;
  /** Copy for the active locale; resolved from the host locale when omitted. */
  copy?: ItemSheetCopy;
  /** Injected clock; read once on open when omitted. */
  now?: Date;
  departmentMark?: ItemSheetMark;
  groups?: readonly ItemSheetGroup[];
}

/**
 * Non-modal 560 px item sheet over the right edge of the board: no scrim, no
 * focus trap, the board behind stays interactive. It is absolutely placed, so
 * the Kanban view root must be the positioned ancestor.
 */
export function KanbanCardDetailPanel({
  board,
  card,
  detailData,
  callbacks,
  onCloseAutoFocus,
  capabilities,
  copy,
  now,
  departmentMark,
  groups,
}: KanbanCardDetailPanelProps) {
  const localeCopy = useItemSheetCopy();
  const [openedAt] = useState(() => new Date());
  if (!card) return null;

  return (
    <div className="absolute inset-y-0 right-0 z-20 flex w-full max-w-full border-l border-border bg-popover shadow-overlay sm:w-140">
      <TaskDetailPanelSurface
        status="ready"
        board={board}
        card={card}
        capabilities={capabilities}
        detailData={detailData}
        callbacks={callbacks}
        copy={copy ?? localeCopy}
        now={now ?? openedAt}
        departmentMark={departmentMark}
        groups={groups}
        onReturnFocus={onCloseAutoFocus}
      />
    </div>
  );
}
