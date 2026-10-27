import { useState, type ReactNode } from "react";
import { toast } from "sonner";
import {
  useBoardTrash,
  useDeleteCard,
  useMoveItemToBoard,
  useWorkspaceBoards,
} from "@notrelix/work-management-state";
import { DeleteItemDialog, MoveItemDialog } from "./item-lifecycle-dialogs";
import { fillLifecycleCopy, pluralLifecycleCopy } from "./item-lifecycle.copy";
import { useItemLifecycleCopy } from "./use-item-lifecycle-copy";

interface Pending {
  readonly id: string;
  readonly title: string;
}

/**
 * Deleting and moving one item for the surfaces that show a single item (card
 * menu, side sheet, item page). `requestDelete` and `requestMove` open the
 * confirmation and the board picker; `dialogs` is rendered once by the host.
 * Delete answers with a toast that restores it; both name what went wrong.
 * `onGone` runs once the item has left the board (the sheet or page closes).
 */
export function useItemRemoval({
  workspaceId,
  boardId,
  titleOf,
  onGone,
}: {
  workspaceId: string;
  boardId: string;
  titleOf: (itemId: string) => string;
  onGone?: (itemId: string) => void;
}): {
  requestDelete: (itemId: string) => void;
  requestMove: (itemId: string) => void;
  dialogs: ReactNode;
} {
  const copy = useItemLifecycleCopy();
  const [deleting, setDeleting] = useState<Pending | null>(null);
  const [moving, setMoving] = useState<Pending | null>(null);
  const deleteCard = useDeleteCard(boardId, workspaceId);
  const trash = useBoardTrash(boardId, workspaceId, false);
  const boards = useWorkspaceBoards(workspaceId);
  const move = useMoveItemToBoard(boardId, workspaceId, {
    onError: (_error, { cardId }) =>
      toast.error(
        fillLifecycleCopy(copy.moveFailed, { title: titleOf(cardId) }),
      ),
    onMoved: (result, { cardId, targetBoardId }) => {
      const target = boards.data?.find((board) => board.id === targetBoardId);
      toast.success(
        fillLifecycleCopy(copy.moved, {
          title: titleOf(cardId),
          board: target?.title ?? "",
          key: result.itemKey,
        }),
        {
          description: droppedNotice(
            result.droppedFields,
            result.droppedLabels,
            result.droppedAssignees,
          ),
        },
      );
      onGone?.(cardId);
    },
  });

  const droppedNotice = (
    fields: readonly string[],
    labels: number,
    assignees: number,
  ): string | undefined => {
    const parts = [
      ...fields,
      ...(labels > 0
        ? [
            pluralLifecycleCopy(
              {
                one: copy.droppedLabels_one,
                other: copy.droppedLabels_other,
              },
              labels,
            ),
          ]
        : []),
      ...(assignees > 0
        ? [
            pluralLifecycleCopy(
              {
                one: copy.droppedAssignees_one,
                other: copy.droppedAssignees_other,
              },
              assignees,
            ),
          ]
        : []),
    ];
    return parts.length > 0
      ? fillLifecycleCopy(copy.movedDropped, { what: parts.join(", ") })
      : undefined;
  };

  const confirmDelete = () => {
    if (!deleting) return;
    const { id, title } = deleting;
    setDeleting(null);
    deleteCard.mutate(id, {
      onSuccess: () => {
        toast.success(fillLifecycleCopy(copy.deleted, { title }), {
          action: {
            label: copy.deletedUndo,
            onClick: () =>
              void trash.restore(id).then(
                () =>
                  toast.success(fillLifecycleCopy(copy.restored, { title })),
                () =>
                  toast.error(fillLifecycleCopy(copy.restoreFailed, { title })),
              ),
          },
        });
        onGone?.(id);
      },
      onError: () =>
        toast.error(fillLifecycleCopy(copy.deleteFailed, { title })),
    });
  };

  const targets = (boards.data ?? [])
    .filter(
      (candidate) =>
        candidate.id !== boardId &&
        !candidate.isArchived &&
        candidate.canAddItems !== false,
    )
    .map((candidate) => ({ id: candidate.id, title: candidate.title }));

  return {
    requestDelete: (itemId) =>
      setDeleting({ id: itemId, title: titleOf(itemId) }),
    requestMove: (itemId) => setMoving({ id: itemId, title: titleOf(itemId) }),
    dialogs: (
      <>
        <DeleteItemDialog
          copy={copy}
          title={deleting?.title ?? ""}
          open={deleting !== null}
          onCancel={() => setDeleting(null)}
          onConfirm={confirmDelete}
        />
        <MoveItemDialog
          copy={copy}
          title={moving?.title ?? ""}
          boards={targets}
          open={moving !== null}
          onCancel={() => setMoving(null)}
          onConfirm={(targetBoardId) => {
            if (!moving) return;
            const { id } = moving;
            setMoving(null);
            move.mutate({ cardId: id, targetBoardId });
          }}
        />
      </>
    ),
  };
}
