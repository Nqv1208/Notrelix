import { toast } from "sonner";
import { useBoardArchive } from "@notrelix/work-management-state";
import { BoardTrashSheet } from "./board-trash-sheet";
import { fillLifecycleCopy } from "./item-lifecycle.copy";
import { useItemLifecycleCopy } from "./use-item-lifecycle-copy";

/** The board's archived-items sheet wired to the archive read and unarchive. */
export function BoardArchiveHost({
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
  const archive = useBoardArchive(boardId, workspaceId, "", open);
  return (
    <BoardTrashSheet
      copy={copy}
      labels={{
        title: copy.archiveListTitle,
        body: copy.archiveListBody,
        empty: copy.archiveListEmpty,
        loading: copy.archiveListLoading,
        error: copy.archiveListError,
        action: copy.archiveListAction,
      }}
      open={open}
      loading={archive.isLoading}
      failed={archive.isError}
      restoringId={archive.unarchivingId}
      rows={archive.entries.map((entry) => ({
        id: entry.id,
        name: entry.name,
        itemKey: entry.itemKey,
        note: fillLifecycleCopy(copy.archiveListSince, {
          day: entry.archivedAt.slice(0, 10),
        }),
      }))}
      onRestore={(row) =>
        void archive.unarchive(row.id).then(
          () =>
            toast.success(
              fillLifecycleCopy(copy.unarchived, { title: row.name }),
            ),
          () =>
            toast.error(
              fillLifecycleCopy(copy.unarchiveFailed, { title: row.name }),
            ),
        )
      }
      onClose={onClose}
    />
  );
}
