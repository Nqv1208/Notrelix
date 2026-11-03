import { useState } from "react";
import { toast } from "sonner";
import {
  classifyRestoreError,
  useBoardTrash,
} from "@notrelix/work-management-state";
import { BoardTrashSheet } from "./board-trash-sheet";
import { fillLifecycleCopy } from "./item-lifecycle.copy";
import { useItemLifecycleCopy } from "./use-item-lifecycle-copy";

const DAY_MS = 24 * 60 * 60 * 1000;

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
  // Why the last try on a row was refused; it stays until the next try on that row.
  const [refused, setRefused] = useState<Readonly<Record<string, string>>>({});
  const dayFormat = new Intl.DateTimeFormat(copy.locale, {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
  });
  const reasonOf = (error: unknown): string | null => {
    switch (classifyRestoreError(error)) {
      case "windowClosed":
        return copy.restoreWindowClosed;
      case "parentDeleted":
        return copy.restoreParentDeleted;
      case "gone":
        return copy.restoreGone;
      default:
        return null;
    }
  };
  return (
    <BoardTrashSheet
      copy={copy}
      open={open}
      loading={trash.isLoading}
      failed={trash.isError}
      total={trash.total}
      hasMore={trash.hasMore}
      loadingMore={trash.loadingMore}
      restoringId={trash.restoringId}
      onRetry={trash.retry}
      onLoadMore={trash.loadMore}
      rows={trash.entries.map((entry) => {
        const day = dayFormat.format(new Date(entry.deletedAt));
        const days = Math.max(
          0,
          Math.ceil((Date.parse(entry.restorableUntil) - Date.now()) / DAY_MS),
        );
        return {
          id: entry.id,
          name: entry.name,
          itemKey: entry.itemKey,
          subItemCount: entry.subItemCount,
          note: entry.deletedByName
            ? fillLifecycleCopy(copy.trashBy, {
                who: entry.deletedByName,
                day,
                days,
              })
            : fillLifecycleCopy(copy.trashByNobody, { day, days }),
          refusal: refused[entry.id],
        };
      })}
      onRestore={(row) => {
        setRefused(({ [row.id]: _gone, ...rest }) => rest);
        void trash.restore(row.id).then(
          () =>
            toast.success(
              fillLifecycleCopy(copy.restored, { title: row.name }),
            ),
          (error: unknown) => {
            const reason = reasonOf(error);
            if (reason) setRefused((now) => ({ ...now, [row.id]: reason }));
            else
              toast.error(
                fillLifecycleCopy(copy.restoreFailed, { title: row.name }),
              );
          },
        );
      }}
      onClose={onClose}
    />
  );
}
