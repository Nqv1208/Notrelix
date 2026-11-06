import { useEffect, useState } from "react";
import { toast } from "sonner";
import {
  bulkFailedItemId,
  useBoardCardReader,
  useBulkUpdateItems,
  useMoveItemToBoard,
  useWorkspaceBoards,
  type BulkItemChange,
} from "@notrelix/work-management-state";
import {
  BULK_ITEM_LIMIT,
  findStatusField,
  orderByKey,
  type Board,
} from "@notrelix/work-management-core";
import { findItemFields } from "../../card-detail/item-field-editors";
import type { TaskDetailCapabilities } from "../../card-detail/task-detail-types";
import { fillTableCopy, pluralTableCopy } from "../table/main-table.copy";
import { useMainTableCopy } from "../table/use-main-table-copy";
import { tableStatusOptions } from "../table/table-model";
import {
  TableArchivedNotice,
  TableBulkBar,
  TableBulkDone,
} from "../table/table-bulk-bar";
import { useArchiveUndoWindow } from "../table/use-archive-undo-window";

/** "YYYY-MM-DD" as an instant at 17:00 of that local day. */
function dueInstant(day: string): string {
  return new Date(`${day}T17:00:00`).toISOString();
}

/**
 * The bulk bar of the Kanban: the Table's own bar over the cards picked on
 * the board. Status, assignee, due date and priority go as one request (all
 * or none); archive and delete go the same way; moving to another board
 * writes each card. The board is read again after each write, so a refusal
 * leaves exactly what the server holds. Archive waits for the Table's undo
 * window: the cards leave the board at once (`onPendingArchiveChange`) and
 * the one request goes out when the window ends.
 */
export function KanbanBulkBar({
  boardId,
  workspaceId,
  board,
  capabilities,
  selectedIds,
  onClear,
  onPendingArchiveChange,
}: {
  boardId: string;
  workspaceId: string;
  board: Board;
  capabilities: TaskDetailCapabilities;
  selectedIds: ReadonlySet<string>;
  onClear: () => void;
  /** The cards waiting in the undo window, which the board hides meanwhile. */
  onPendingArchiveChange?: (ids: readonly string[]) => void;
}) {
  const copy = useMainTableCopy();
  const bulk = useBulkUpdateItems(boardId, workspaceId);
  const readCard = useBoardCardReader(boardId, workspaceId);
  const titleOf = (id: string) => readCard(id)?.title;
  // What the last change did, for a few seconds, with "Hoàn tác" where one request takes it back (BC-Bulk `bk-ok`).
  const [done, setDone] = useState<{
    text: string;
    onUndo?: () => void;
  } | null>(null);
  useEffect(() => {
    if (!done) return;
    const timer = setTimeout(() => setDone(null), 6000);
    return () => clearTimeout(timer);
  }, [done]);
  const archiveWindow = useArchiveUndoWindow((itemIds, settled) => {
    bulk.mutate(
      { itemIds, change: { type: "archive" } },
      {
        onError: (error) => {
          const failed = bulkFailedItemId(error);
          const title = failed ? titleOf(failed) : undefined;
          toast.error(
            title
              ? fillTableCopy(copy.failed.bulkItem, { title })
              : copy.failed.bulk,
            { id: "kanban-bulk" },
          );
        },
        onSettled: settled,
      },
    );
  });
  const pendingArchive = archiveWindow.pendingIds;
  useEffect(() => {
    onPendingArchiveChange?.(pendingArchive);
  }, [pendingArchive, onPendingArchiveChange]);
  const moveItem = useMoveItemToBoard(boardId, workspaceId, {
    onError: () => toast.error(copy.failed.bulk, { id: "kanban-bulk-move" }),
  });
  const workspaceBoards = useWorkspaceBoards(workspaceId);
  const itemFields = findItemFields(board);
  const statusField = findStatusField(board.fieldDefinitions);
  const ids = [...selectedIds];

  // The change that puts the picked cards back, when they all held the same value before it.
  const inverseOf = (change: BulkItemChange): BulkItemChange | null => {
    const cards = ids.map(readCard);
    if (cards.some((card) => !card)) return null;
    const same = <T,>(
      read: (card: NonNullable<(typeof cards)[number]>) => T,
    ) => {
      const first = read(cards[0]!);
      return cards.every((card) => read(card!) === first) ? first : undefined;
    };
    if (change.type === "set-status") {
      const prev = same((card) => card.statusOptionId ?? null);
      return prev ? { type: "set-status", statusOptionId: prev } : null;
    }
    if (change.type === "set-due-date" || change.type === "clear-due-date") {
      const prev = same((card) => card.dueAt ?? card.dueDate ?? null);
      if (prev === undefined) return null;
      return prev
        ? { type: "set-due-date", dueDate: prev }
        : { type: "clear-due-date" };
    }
    return null;
  };
  const doneText = (change: BulkItemChange, count: number): string | null => {
    const b = copy.bulk;
    return change.type === "set-status"
      ? pluralTableCopy(
          { one: b.doneStatus_one, other: b.doneStatus_other },
          count,
        )
      : change.type === "set-due-date" || change.type === "clear-due-date"
        ? pluralTableCopy({ one: b.doneDue_one, other: b.doneDue_other }, count)
        : change.type === "set-priority"
          ? pluralTableCopy(
              { one: b.donePriority_one, other: b.donePriority_other },
              count,
            )
          : null;
  };
  const write = (change: BulkItemChange, done?: string) => {
    const picked = [...ids];
    const inverse = inverseOf(change);
    bulk.mutate(
      { itemIds: picked, change },
      {
        // The pick is spent only once the server took the change; a refusal keeps it and names the item.
        onSuccess: () => {
          if (done) toast.success(done, { id: "kanban-bulk" });
          const text = doneText(change, picked.length);
          if (text)
            setDone({
              text,
              onUndo: inverse
                ? () => {
                    setDone(null);
                    bulk.mutate(
                      { itemIds: picked, change: inverse },
                      {
                        onError: () =>
                          toast.error(copy.failed.bulk, { id: "kanban-bulk" }),
                      },
                    );
                  }
                : undefined,
            });
          onClear();
        },
        onError: (error) => {
          const failed = bulkFailedItemId(error);
          const title = failed ? titleOf(failed) : undefined;
          toast.error(
            title
              ? fillTableCopy(copy.failed.bulkItem, { title })
              : copy.failed.bulk,
            { id: "kanban-bulk" },
          );
        },
      },
    );
  };
  const settle = write;

  const moveTargets = (workspaceBoards.data ?? [])
    .filter(
      (candidate) =>
        candidate.id !== boardId &&
        !candidate.isArchived &&
        candidate.canAddItems !== false,
    )
    .map((candidate) => ({ id: candidate.id, title: candidate.title }));

  if (ids.length === 0)
    return pendingArchive.length > 0 ? (
      <TableArchivedNotice
        copy={copy}
        count={pendingArchive.length}
        onUndo={archiveWindow.undo}
        onClose={archiveWindow.flush}
      />
    ) : done ? (
      <TableBulkDone
        text={done.text}
        undoLabel={copy.archived.undo}
        onUndo={done.onUndo}
      />
    ) : null;

  return (
    <TableBulkBar
      copy={copy}
      count={ids.length}
      saving={bulk.isPending}
      limit={{ max: BULK_ITEM_LIMIT, available: ids.length }}
      statusOptions={
        statusField
          ? tableStatusOptions(
              orderByKey(statusField.options, (option) => option.position),
            )
          : []
      }
      canEdit={capabilities.canEditFields}
      canArchive={capabilities.canDelete}
      onStatus={(optionId) =>
        settle({ type: "set-status", statusOptionId: optionId })
      }
      onDue={(day) =>
        settle(
          day
            ? { type: "set-due-date", dueDate: dueInstant(day) }
            : { type: "clear-due-date" },
        )
      }
      onArchive={() => {
        setDone(null);
        archiveWindow.start(ids);
        onClear();
      }}
      {...(capabilities.canDelete
        ? { onDelete: () => settle({ type: "delete" }) }
        : {})}
      onClear={onClear}
      {...(itemFields.priority
        ? {
            priority: {
              options: itemFields.priority.options.map((option) => ({
                id: option.id,
                label: option.label,
              })),
              onPriority: (optionId: string) =>
                settle({ type: "set-priority", priorityOptionId: optionId }),
            },
          }
        : {})}
      {...(itemFields.person
        ? {
            assign: {
              members: board.members.map((member) => ({
                userId: member.userId,
                name: member.name,
                initials: member.initials,
              })),
              onAssign: (userId: string | null) =>
                settle(
                  userId
                    ? { type: "assign", userId }
                    : { type: "unassign-all" },
                ),
            },
          }
        : {})}
      {...(capabilities.canEditFields
        ? {
            move: {
              boards: moveTargets,
              onMove: (targetBoardId: string) => {
                for (const cardId of ids)
                  moveItem.mutate({ cardId, targetBoardId });
                onClear();
              },
            },
          }
        : {})}
    />
  );
}
