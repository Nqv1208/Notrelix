import { toast } from "sonner";
import {
  useArchiveCard,
  useBoardKanban,
  useDuplicateCard,
} from "@notrelix/work-management-state";
import {
  generatePosition,
  type UpdateFieldValueInput,
} from "@notrelix/work-management-core";
import { buildCardLink } from "../views/kanban/kanban-view-helpers";
import { useKanbanLaneCopy } from "../views/kanban/use-kanban-lane-copy";

type Kanban = ReturnType<typeof useBoardKanban>;

/**
 * What the item side sheet does when a view other than the Kanban opens it:
 * the Status pill writes through the status endpoint, "move to group" puts
 * the item last in the group, "copy link" copies the board path with the
 * `item` parameter, duplicate and archive. The view owns the board read and
 * passes it in.
 */
export function useItemSheetActions({
  boardId,
  workspaceId,
  kanban,
}: {
  boardId: string;
  workspaceId: string;
  kanban: Kanban;
}) {
  const laneCopy = useKanbanLaneCopy();
  const duplicateCard = useDuplicateCard(boardId, workspaceId);
  const archiveCard = useArchiveCard(boardId, workspaceId);

  const onUpdateFieldValue = (payload: UpdateFieldValueInput) => {
    if (
      payload.fieldDefinitionId === kanban.statusField?.id &&
      typeof payload.value === "string"
    ) {
      kanban.updateStatus.mutate({
        cardId: payload.cardId,
        statusOptionId: payload.value,
      });
      return;
    }
    kanban.updateFieldValue.mutate(payload);
  };

  const onMoveToGroup = (cardId: string, groupId: string) => {
    const target = kanban.groups.find((group) => group.id === groupId);
    if (!target) return;
    const last = [...target.cards]
      .filter((card) => card.id !== cardId)
      .sort((a, b) => a.position - b.position)
      .at(-1);
    kanban.moveCard.mutate({
      cardId,
      listId: groupId,
      position: generatePosition(last?.position, undefined),
      previousSiblingId: last?.id ?? null,
      nextSiblingId: null,
    });
  };

  const onCopyLink = (cardId: string) => {
    const link = buildCardLink(window.location, cardId);
    void Promise.resolve()
      .then(() => navigator.clipboard.writeText(link))
      .then(
        () => toast.success(laneCopy.linkCopied),
        () => toast.error(laneCopy.linkCopyFailed),
      );
  };

  return {
    onUpdateFieldValue,
    onMoveToGroup,
    onCopyLink,
    onDuplicate: (cardId: string) => duplicateCard.mutate(cardId),
    onArchive: (cardId: string) => archiveCard.mutate(cardId),
  };
}
