import { useEffect, useState } from "react";
import {
  useCard,
  useFullBoard,
  useItemConflict,
} from "@notrelix/work-management-state";
import type { Board, CardDetail } from "@notrelix/work-management-core";
import { usePickerActions } from "../../card-popup/pickers/use-picker-actions";
import { CardPopupSurface } from "../../card-popup/card-popup-surface";
import { useCardPopupModel } from "../../card-popup/use-card-popup-model";
import type { CardPopupSlots } from "../../card-popup/card-popup-context";
import { withBoardPriority } from "../../shared/item-priority";
import type {
  ItemSheetGroup,
  TaskDetailCallbacks,
  TaskDetailCapabilities,
  TaskDetailData,
} from "../../card-detail/task-detail-types";

interface KanbanCardDetailPanelProps {
  board: Board;
  /** The selected card. */
  card: CardDetail;
  detailData: TaskDetailData;
  callbacks: TaskDetailCallbacks;
  /**
   * Called as the popup closes. `preventDefault()` skips the default focus
   * return so the container can put focus on the opener.
   */
  onCloseAutoFocus?: (event: Event) => void;
  /** The viewer's capabilities on this board: the container decides. */
  capabilities: TaskDetailCapabilities;
  /** Injected clock; read once on open when omitted. */
  now?: Date;
  groups?: readonly ItemSheetGroup[];
  slots?: CardPopupSlots;
}

/**
 * The card popup of the board views and My work: a modal over the page with
 * the card's columns of detail and comments. The container reads and writes
 * the card; this part shapes them into the popup's model.
 */
export function KanbanCardDetailPanel({
  board,
  card,
  detailData,
  callbacks,
  onCloseAutoFocus,
  capabilities,
  now,
  groups,
  slots,
}: KanbanCardDetailPanelProps) {
  const [openedAt] = useState(() => new Date());
  const pickerActions = usePickerActions(card);
  const model = useCardPopupModel({
    mode: "view",
    board,
    card: withBoardPriority(card, board),
    capabilities,
    data: detailData,
    callbacks,
    groups,
    now: now ?? openedAt,
    slots,
    actions: {
      moveToGroup: (groupId) => callbacks.onMoveToGroup?.(card.id, groupId),
      ...pickerActions,
    },
  });
  const { refetch } = useCard(card.id, card.workspaceId);
  const { refetch: refetchBoard } = useFullBoard(
    card.boardId,
    card.workspaceId,
  );
  // A write started outside the popup's own tracking (dates, people, complete) refused as stale shows the same notice.
  const shared = useItemConflict(card.workspaceId, card.id);
  // A notice from an earlier opening of the card is not this opening's.
  const { clear: clearShared } = shared;
  useEffect(() => {
    clearShared();
  }, [card.id]);
  return (
    <CardPopupSurface
      status="ready"
      model={model}
      conflict={model.conflict || shared.conflict}
      staleSchema={shared.staleSchema}
      onReloadConflict={() => {
        refetch();
        void refetchBoard();
        model.clearConflict();
        shared.clear();
      }}
      onClose={callbacks.onClose}
      onCloseAutoFocus={onCloseAutoFocus}
    />
  );
}
