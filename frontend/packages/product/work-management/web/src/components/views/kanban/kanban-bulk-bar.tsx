import { toast } from "sonner";
import {
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
import { pluralTableCopy } from "../table/main-table.copy";
import { useMainTableCopy } from "../table/use-main-table-copy";
import { tableStatusOptions } from "../table/table-model";
import { TableBulkBar } from "../table/table-bulk-bar";

/** "YYYY-MM-DD" as an instant at 17:00 of that local day. */
function dueInstant(day: string): string {
  return new Date(`${day}T17:00:00`).toISOString();
}

/**
 * The bulk bar of the Kanban: the Table's own bar over the cards picked on
 * the board. Status, assignee, due date and priority go as one request (all
 * or none); archive and delete go the same way; moving to another board
 * writes each card. The board is read again after each write, so a refusal
 * leaves exactly what the server holds.
 */
export function KanbanBulkBar({
  boardId,
  workspaceId,
  board,
  capabilities,
  selectedIds,
  onClear,
}: {
  boardId: string;
  workspaceId: string;
  board: Board;
  capabilities: TaskDetailCapabilities;
  selectedIds: ReadonlySet<string>;
  onClear: () => void;
}) {
  const copy = useMainTableCopy();
  const bulk = useBulkUpdateItems(boardId, workspaceId);
  const moveItem = useMoveItemToBoard(boardId, workspaceId, {
    onError: () => toast.error(copy.failed.bulk, { id: "kanban-bulk-move" }),
  });
  const workspaceBoards = useWorkspaceBoards(workspaceId);
  const itemFields = findItemFields(board);
  const statusField = findStatusField(board.fieldDefinitions);
  const ids = [...selectedIds];

  const write = (change: BulkItemChange, done?: string) =>
    bulk.mutate(
      { itemIds: ids, change },
      {
        onSuccess: () => {
          if (done) toast.success(done, { id: "kanban-bulk" });
        },
        onError: () => toast.error(copy.failed.bulk, { id: "kanban-bulk" }),
      },
    );
  const settle = (change: BulkItemChange, done?: string) => {
    write(change, done);
    onClear();
  };

  const moveTargets = (workspaceBoards.data ?? [])
    .filter(
      (candidate) =>
        candidate.id !== boardId &&
        !candidate.isArchived &&
        candidate.canAddItems !== false,
    )
    .map((candidate) => ({ id: candidate.id, title: candidate.title }));

  return (
    <TableBulkBar
      copy={copy}
      count={ids.length}
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
      onArchive={() =>
        settle(
          { type: "archive" },
          pluralTableCopy(
            { one: copy.archived.done_one, other: copy.archived.done_other },
            ids.length,
          ),
        )
      }
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
