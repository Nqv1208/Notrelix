import { toast } from "sonner";
import { useBoardTrash } from "@notrelix/work-management-state";
import { BoardTrashSheet } from "./board-trash-sheet";
import { fillLifecycleCopy } from "./item-lifecycle.copy";
import { useItemLifecycleCopy } from "./use-item-lifecycle-copy";

/** The board's deleted-items sheet wired to the trash read and restore. */
export function BoardTrashHost({
  workspaceId,
  boardId,
  open,
  onClose,
}: {
  workspaceId: string;
  boardId: string;
  open: boolean;
  onClose: () => void;
}) {
  const copy = useItemLifecycleCopy();
  const trash = useBoardTrash(boardId, workspaceId, open);
  return (
    <BoardTrashSheet
      copy={copy}
      open={open}
      loading={trash.isLoading}
      failed={trash.isError}
      restoringId={trash.restoringId}
      rows={trash.entries.map((entry) => ({
        id: entry.id,
        name: entry.name,
        itemKey: entry.itemKey,
        restorableUntilDay: entry.restorableUntil.slice(0, 10),
      }))}
      onRestore={(row) =>
        void trash.restore(row.id).then(
          () =>
            toast.success(
              fillLifecycleCopy(copy.restored, { title: row.name }),
            ),
          () =>
            toast.error(
              fillLifecycleCopy(copy.restoreFailed, { title: row.name }),
            ),
        )
      }
      onClose={onClose}
    />
  );
}
