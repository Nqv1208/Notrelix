import { useMemo, useState, type ReactNode } from "react";
import { toast } from "sonner";
import {
  classifyArchiveError,
  classifyMoveError,
  classifyRemindError,
  useBoardLabels,
  useArchiveCard,
  useBoardTrash,
  useDeleteCard,
  useDuplicateCard,
  useFullBoard,
  useItemLinks,
  useMoveItemToBoard,
  useRefreshItem,
  useWorkManagementServices,
  useWorkspaceBoards,
} from "@notrelix/work-management-state";
import {
  previewMoveToBoard,
  type Card,
  type FullBoardResponse,
} from "@notrelix/work-management-core";
import { useBoardViewerId } from "../shared/board-viewer";
import {
  DuplicateItemDialog,
  type DuplicateChoices,
} from "../item-actions/duplicate-item-dialog";
import {
  MoveBoardDialog,
  type MoveDialogStatus,
  type MovePreviewView,
} from "../item-actions/move-board-dialog";
import {
  RemindDialog,
  type RemindDialogState,
} from "../item-actions/remind-dialog";
import { useItemActionsCopy } from "../item-actions/use-item-actions-copy";
import {
  ArchiveItemDialog,
  type ArchiveDialogStatus,
} from "./archive-item-dialog";
import { DeleteItemDialog } from "./item-lifecycle-dialogs";
import { fillLifecycleCopy, pluralLifecycleCopy } from "./item-lifecycle.copy";
import { useItemLifecycleCopy } from "./use-item-lifecycle-copy";

interface Pending {
  readonly id: string;
  readonly title: string;
}

function findCard(
  board: FullBoardResponse | undefined,
  id: string,
): { card: Card; groupTitle: string } | undefined {
  for (const group of board?.groups ?? []) {
    const card = group.cards.find((candidate) => candidate.id === id);
    if (card) return { card, groupTitle: group.title };
  }
  return undefined;
}

/** The status field of a board read, with its options in order. */
function statusOptions(
  board:
    { fieldDefinitions: FullBoardResponse["fieldDefinitions"] } | undefined,
) {
  const field = board?.fieldDefinitions.find(
    (definition) => definition.semanticRole === "status",
  );
  return field?.options ?? [];
}

function hasValue(value: unknown): boolean {
  if (value === null || value === undefined || value === "") return false;
  return !(Array.isArray(value) && value.length === 0);
}

/**
 * What one item's actions need beyond the menu: archive and delete, move (with the
 * preview), duplicate (with options) and remind, each with its confirmation,
 * toasts and failure handling. `requestX` opens the dialog; `dialogs` is
 * rendered once by the host. `onGone` runs once the item has left the board
 * (the sheet or page closes).
 */
export function useItemRemoval({
  workspaceId,
  boardId,
  titleOf,
  onGone,
  onOpenItem,
}: {
  workspaceId: string;
  boardId: string;
  titleOf: (itemId: string) => string;
  onGone?: (itemId: string) => void;
  /** Opens another item in the host (the item a move landed on, a copy). */
  onOpenItem?: (itemId: string) => void;
}): {
  requestArchive: (itemId: string) => void;
  requestDelete: (itemId: string) => void;
  requestMove: (itemId: string) => void;
  requestDuplicate: (itemId: string) => void;
  requestRemind: (itemId: string) => void;
  completeItem: (itemId: string) => void;
  reopenItem: (itemId: string) => void;
  unarchiveItem: (itemId: string) => void;
  dialogs: ReactNode;
} {
  const copy = useItemLifecycleCopy();
  const actions = useItemActionsCopy();
  const { itemExtras } = useWorkManagementServices();
  const viewerId = useBoardViewerId();
  const [archiving, setArchiving] = useState<Pending | null>(null);
  const [archiveStatus, setArchiveStatus] =
    useState<ArchiveDialogStatus>("idle");
  const [deleting, setDeleting] = useState<Pending | null>(null);
  const [moving, setMoving] = useState<Pending | null>(null);
  const [moveTarget, setMoveTarget] = useState<string | null>(null);
  const [moveStatus, setMoveStatus] = useState<MoveDialogStatus>("idle");
  const [duplicating, setDuplicating] = useState<Pending | null>(null);
  const [reminding, setReminding] = useState<Pending | null>(null);
  const [remindState, setRemindState] = useState<RemindDialogState>("confirm");
  const [remindWhen, setRemindWhen] = useState<string | null>(null);
  const archiveCard = useArchiveCard(boardId, workspaceId);
  const archiveLinks = useItemLinks(archiving?.id ?? "", workspaceId, boardId);
  const deleteCard = useDeleteCard(boardId, workspaceId);
  const duplicateCard = useDuplicateCard(boardId, workspaceId);
  const trash = useBoardTrash(boardId, workspaceId, false);
  const boards = useWorkspaceBoards(workspaceId);
  const source = useFullBoard(boardId, workspaceId);
  const target = useFullBoard(moveTarget ?? undefined, workspaceId);
  const targetLabels = useBoardLabels(
    moveTarget ?? "",
    workspaceId,
    Boolean(moveTarget),
  );
  const sourceBoard = useMemo(
    () => ({
      groups: source.groups,
      fieldDefinitions: source.fieldDefinitions,
    }),
    [source.groups, source.fieldDefinitions],
  );

  const refreshItem = useRefreshItem(workspaceId, boardId);

  const move = useMoveItemToBoard(boardId, workspaceId, {
    onError: (error) => setMoveStatus(classifyMoveError(error)),
    onMoved: (result, { cardId, targetBoardId }) => {
      const targetBoard = boards.data?.find(
        (board) => board.id === targetBoardId,
      );
      closeMove();
      toast.success(
        fillLifecycleCopy(copy.moved, {
          title: titleOf(cardId),
          board: targetBoard?.title ?? "",
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

  const closeMove = () => {
    setMoving(null);
    setMoveTarget(null);
    setMoveStatus("idle");
  };

  const confirmArchive = () => {
    if (!archiving) return;
    const { id, title } = archiving;
    setArchiveStatus("busy");
    archiveCard.mutate(id, {
      onSuccess: () => {
        setArchiving(null);
        setArchiveStatus("idle");
        toast.success(fillLifecycleCopy(copy.archived, { title }), {
          action: {
            label: copy.archivedUndo,
            onClick: () =>
              quick(
                id,
                () => itemExtras.restoreItem(id),
                actions.unarchived,
                actions.archiveFailed,
              ),
          },
        });
        onGone?.(id);
      },
      // The board read is fetched again when the write ends, so a try after a
      // conflict carries the newer version.
      onError: (error) => setArchiveStatus(classifyArchiveError(error)),
    });
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
        toast.error(fillLifecycleCopy(copy.deleteFailed, { title }), {
          action: {
            label: actions.retry,
            onClick: () => setDeleting({ id, title }),
          },
        }),
    });
  };

  const confirmDuplicate = (name: string, choices: DuplicateChoices) => {
    if (!duplicating) return;
    const { id } = duplicating;
    duplicateCard.mutate(
      { cardId: id, options: { name, ...choices } },
      {
        onSuccess: (copyId) => {
          setDuplicating(null);
          toast.success(
            fillLifecycleCopy(actions.dupDone, { key: name }),
            copyId && onOpenItem
              ? {
                  action: {
                    label: actions.dupOpen,
                    onClick: () => onOpenItem(copyId),
                  },
                }
              : undefined,
          );
        },
      },
    );
  };

  const recipientsOf = (itemId: string): string[] =>
    (findCard(sourceBoard as FullBoardResponse, itemId)?.card.members ?? [])
      .filter((member) => member.userId !== viewerId)
      .map((member) => member.name);

  const confirmRemind = () => {
    if (!reminding) return;
    const { id, title } = reminding;
    setRemindState("busy");
    itemExtras.remindOwners(id).then(
      (result) => {
        setReminding(null);
        setRemindState("confirm");
        const names = recipientsOf(id);
        const when = result.nextRemindAt
          ? new Date(result.nextRemindAt).toLocaleString()
          : "";
        toast.success(
          names.length > 0
            ? fillLifecycleCopy(actions.remindSent, {
                who: names.join(", "),
                when,
              })
            : fillLifecycleCopy(actions.remindSentCount, {
                count: result.recipients,
                when,
              }),
        );
        refreshItem(id);
        void title;
      },
      (error: unknown) => {
        const failure = classifyRemindError(error);
        if (failure.kind === "tooSoon") {
          setRemindWhen(
            failure.nextRemindAt
              ? new Date(failure.nextRemindAt).toLocaleString()
              : null,
          );
          setRemindState("tooSoon");
        } else if (failure.kind === "nobody" && recipientsOf(id).length === 0) {
          setRemindState("nobody");
        } else {
          setRemindState("error");
        }
      },
    );
  };

  const quick = (
    itemId: string,
    run: () => Promise<void>,
    ok: string,
    failed: string,
  ) => {
    const title = titleOf(itemId);
    run()
      .then(
        () => toast.success(fillLifecycleCopy(ok, { title })),
        () =>
          toast.error(fillLifecycleCopy(failed, { title }), {
            action: {
              label: actions.retry,
              onClick: () => quick(itemId, run, ok, failed),
            },
          }),
      )
      .finally(() => refreshItem(itemId));
  };

  const targets = (boards.data ?? [])
    .filter(
      (candidate) =>
        candidate.id !== boardId &&
        !candidate.isArchived &&
        candidate.canAddItems !== false,
    )
    .map((candidate) => ({ id: candidate.id, title: candidate.title }));

  const preview: MovePreviewView | null = useMemo(() => {
    if (!moving || !moveTarget || !target.board) return null;
    const found = findCard(sourceBoard as FullBoardResponse, moving.id);
    if (!found) return null;
    const { card, groupTitle } = found;
    const sourceStatus = statusOptions(sourceBoard).find(
      (option) => option.id === card.statusOptionId,
    );
    const result = previewMoveToBoard(
      {
        groupTitle,
        statusName: sourceStatus?.label ?? null,
        fields: sourceBoard.fieldDefinitions
          .filter(
            (definition) =>
              !definition.isSystemField &&
              !definition.semanticRole &&
              hasValue(card.fieldValues[definition.id]),
          )
          .map((definition) => ({
            name: definition.name,
            type: definition.fieldType,
          })),
        labelNames: card.labels.map((label) => label.name),
        assignees: card.members.map((member) => ({
          id: member.userId,
          name: member.name,
        })),
      },
      {
        groupTitles: target.groups.map((group) => group.title),
        defaultGroupTitle: null,
        statusNames: statusOptions(target).map((option) => option.label),
        fields: target.fieldDefinitions
          .filter(
            (definition) =>
              !definition.isSystemField && !definition.semanticRole,
          )
          .map((definition) => ({
            name: definition.name,
            type: definition.fieldType,
          })),
        labelNames: (targetLabels.data ?? []).map((label) => label.name),
        visibleTo:
          target.board.visibility === "private"
            ? new Set(target.board.members.map((member) => member.userId))
            : null,
      },
    );
    return {
      boardTitle: target.board.title,
      group: result.group,
      status: result.status,
      losses: [
        ...result.lostFields.map((name) =>
          fillLifecycleCopy(actions.moveLoseField, { name }),
        ),
        ...result.lostLabels.map((name) =>
          fillLifecycleCopy(actions.moveLoseLabel, { name }),
        ),
        ...result.lostAssignees.map((name) =>
          fillLifecycleCopy(actions.moveLoseAssignee, { name }),
        ),
      ],
    };
  }, [
    moving,
    moveTarget,
    target.board,
    target.groups,
    target.fieldDefinitions,
    targetLabels.data,
    sourceBoard,
    actions,
  ]);

  const movingKey = moving
    ? (findCard(sourceBoard as FullBoardResponse, moving.id)?.card.itemKey ??
      null)
    : null;

  const archiveCardView = archiving
    ? findCard(sourceBoard as FullBoardResponse, archiving.id)?.card
    : undefined;
  const dependencyLinks = (archiveLinks.data ?? []).filter(
    (link) => link.relation !== "related",
  );

  return {
    requestArchive: (itemId) => {
      setArchiveStatus("idle");
      setArchiving({ id: itemId, title: titleOf(itemId) });
    },
    requestDelete: (itemId) =>
      setDeleting({ id: itemId, title: titleOf(itemId) }),
    requestMove: (itemId) => {
      setMoveStatus("idle");
      setMoveTarget(null);
      setMoving({ id: itemId, title: titleOf(itemId) });
    },
    requestDuplicate: (itemId) => {
      duplicateCard.reset();
      setDuplicating({ id: itemId, title: titleOf(itemId) });
    },
    requestRemind: (itemId) => {
      setRemindState("confirm");
      setRemindWhen(null);
      setReminding({ id: itemId, title: titleOf(itemId) });
    },
    completeItem: (itemId) =>
      quick(
        itemId,
        () => itemExtras.completeItem(itemId),
        actions.completed,
        actions.completeFailed,
      ),
    reopenItem: (itemId) =>
      quick(
        itemId,
        () => itemExtras.reopenItem(itemId),
        actions.reopened,
        actions.completeFailed,
      ),
    unarchiveItem: (itemId) =>
      quick(
        itemId,
        () => itemExtras.restoreItem(itemId),
        actions.unarchived,
        actions.archiveFailed,
      ),
    dialogs: (
      <>
        <ArchiveItemDialog
          copy={copy}
          title={archiving?.title ?? ""}
          impact={{
            subItems: archiveCardView?.childProgress?.total ?? 0,
            dependencies: {
              count: dependencyLinks.length,
              keys: dependencyLinks
                .map((link) => link.item.key)
                .filter((key): key is string => Boolean(key)),
            },
            checking: archiveLinks.isLoading,
          }}
          status={archiveStatus}
          open={archiving !== null}
          onCancel={() => setArchiving(null)}
          onConfirm={confirmArchive}
        />
        <DeleteItemDialog
          copy={copy}
          actions={actions}
          title={deleting?.title ?? ""}
          open={deleting !== null}
          onCancel={() => setDeleting(null)}
          onConfirm={confirmDelete}
        />
        <MoveBoardDialog
          copy={actions}
          title={moving?.title ?? ""}
          itemKey={movingKey}
          boards={targets}
          open={moving !== null}
          preview={preview}
          status={moveStatus}
          onPick={(id) => {
            setMoveStatus("idle");
            setMoveTarget(id);
          }}
          onCancel={closeMove}
          onConfirm={(targetBoardId) => {
            if (!moving) return;
            setMoveStatus("busy");
            move.mutate({ cardId: moving.id, targetBoardId });
          }}
          onUnarchive={() => {
            if (!moving) return;
            const { id } = moving;
            closeMove();
            quick(
              id,
              () => itemExtras.restoreItem(id),
              actions.unarchived,
              actions.archiveFailed,
            );
          }}
        />
        <DuplicateItemDialog
          copy={actions}
          defaultName={`${duplicating?.title ?? ""} (${copySuffix(actions.locale)})`}
          open={duplicating !== null}
          busy={duplicateCard.isPending}
          failed={duplicateCard.isError}
          onCancel={() => setDuplicating(null)}
          onConfirm={confirmDuplicate}
        />
        <RemindDialog
          copy={actions}
          itemKey={
            reminding
              ? (findCard(sourceBoard as FullBoardResponse, reminding.id)?.card
                  .itemKey ?? null)
              : null
          }
          recipients={reminding ? recipientsOf(reminding.id) : []}
          state={remindState}
          nextRemindAt={remindWhen}
          open={reminding !== null}
          onCancel={() => setReminding(null)}
          onConfirm={confirmRemind}
        />
      </>
    ),
  };
}

function copySuffix(locale: string): string {
  return locale === "vi" ? "bản sao" : "copy";
}
