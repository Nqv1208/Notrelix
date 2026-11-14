import { useState } from "react";
import { toast } from "sonner";
import { useBoardArchive } from "@notrelix/work-management-state";
import { BoardTrashSheet } from "./board-trash-sheet";
import { fillLifecycleCopy } from "./item-lifecycle.copy";
import { useItemLifecycleCopy } from "./use-item-lifecycle-copy";

/** The board's archived-items sheet wired to the archive read (a page at a time, searchable) and unarchive. */
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
  const [search, setSearch] = useState("");
  const archive = useBoardArchive(boardId, workspaceId, search.trim(), open);
  const whenFormat = new Intl.DateTimeFormat(copy.locale, {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
    hour12: false,
  });
  return (
    <BoardTrashSheet
      copy={copy}
      labels={{
        title: copy.archiveListTitle,
        body: copy.archiveListBody,
        empty: copy.listEmptyArchiveBody,
        emptyTitle: copy.listEmptyArchiveTitle,
        loading: copy.archiveListLoading,
        error: copy.archiveListError,
        action: copy.archiveListAction,
        searchPlaceholder: copy.listSearchArchive,
      }}
      open={open}
      search={search}
      onSearch={setSearch}
      loading={archive.isLoading}
      failed={archive.isError}
      total={archive.total}
      hasMore={archive.hasMore}
      loadingMore={archive.loadingMore}
      restoringId={archive.unarchivingId}
      onRetry={archive.retry}
      onLoadMore={archive.loadMore}
      rows={archive.entries.map((entry) => {
        const when = whenFormat.format(new Date(entry.archivedAt));
        return {
          id: entry.id,
          name: entry.name,
          itemKey: entry.itemKey,
          subItemCount: entry.subItemCount,
          note: entry.archivedByName
            ? fillLifecycleCopy(copy.archiveBy, {
                who: entry.archivedByName,
                when,
              })
            : fillLifecycleCopy(copy.archiveByNobody, { when }),
        };
      })}
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
