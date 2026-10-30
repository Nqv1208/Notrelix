import { useState } from "react";
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
  return (
    <CardPopupSurface
      status="ready"
      model={model}
      onClose={callbacks.onClose}
      onCloseAutoFocus={onCloseAutoFocus}
    />
  );
}
