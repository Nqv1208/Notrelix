import {
  useBoardLabelWrites,
  useCardAttachmentWrites,
  useItemConflict,
} from "@notrelix/work-management-state";
import type { CardDetail } from "@notrelix/work-management-core";
import type { CardPopupActions } from "../card-popup-context";

/** The writes behind the pickers: label edit and delete, web link, Documents page. */
export function usePickerActions(
  card: Pick<CardDetail, "id" | "boardId" | "workspaceId">,
): Pick<
  CardPopupActions,
  "updateLabel" | "removeLabel" | "addWebLink" | "linkPage"
> {
  const labels = useBoardLabelWrites(card.boardId, card.workspaceId);
  const attach = useCardAttachmentWrites(card.id, card.workspaceId);
  const conflict = useItemConflict(card.workspaceId, card.id);
  // A label refused as stale tells the open card that someone else changed it.
  const told = (result: Awaited<ReturnType<typeof labels.update>>) => {
    if (!result.ok && result.reason === "conflict") conflict.report();
    return result.ok;
  };
  return {
    updateLabel: async (labelId, patch) =>
      told(await labels.update(labelId, patch)),
    removeLabel: async (labelId) => told(await labels.remove(labelId)),
    addWebLink: attach.addWebLink,
    linkPage: attach.linkPage,
  };
}
