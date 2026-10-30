import {
  useBoardLabelWrites,
  useCardAttachmentWrites,
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
  return {
    updateLabel: async (labelId, patch) =>
      (await labels.update(labelId, patch)).ok,
    removeLabel: async (labelId) => (await labels.remove(labelId)).ok,
    addWebLink: attach.addWebLink,
    linkPage: attach.linkPage,
  };
}
