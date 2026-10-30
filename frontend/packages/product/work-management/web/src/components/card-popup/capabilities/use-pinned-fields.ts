import { useBoardView } from "@notrelix/work-management-state";
import { useCardPopup } from "../card-popup-context";

/** The fields the Kanban view pins on every card face, in the face's order; the popup draws them as chips. */
export function usePinnedFieldIds(): readonly string[] {
  const { board } = useCardPopup();
  const { viewConfig } = useBoardView(board.id, board.workspaceId);
  return viewConfig.cardFieldIds ?? [];
}
