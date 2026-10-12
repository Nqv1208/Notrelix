import { useCallback, useEffect, useRef, useState } from "react";
import { toast } from "sonner";
import {
  useBoardKanban,
  useCard,
  useCardActivity,
  useCardComments,
  useCardFiles,
  useCreateCard,
  useCreateCardUpdate,
  useDeleteCard,
  useDeleteCardUpdate,
  useDuplicateCard,
  useUpdateCard,
  useUpdateCardUpdate,
  useUpdateFieldValue,
} from "@notrelix/work-management-state";
import {
  generatePosition,
  type Board,
  type CardDetail,
} from "@notrelix/work-management-core";
import { KanbanToolbar } from "./kanban-toolbar";
import { KanbanBoard } from "./kanban-board";
import { KanbanCardDetailPanel } from "./kanban-card-detail-panel";
import { KanbanSkeleton } from "./kanban-skeleton";
import { KanbanUnavailableState } from "./kanban-unavailable-state";
import { useKanbanCardCopy } from "./use-kanban-card-copy";
import { useKanbanToolbarCopy } from "./use-kanban-toolbar-copy";
import { emptyKanbanFilter, type KanbanFilterState } from "./kanban-filter";

export function KanbanView({
  boardId,
  workspaceId,
  selectedItemId,
  onSelectedItemChange,
}: {
  boardId: string;
  workspaceId: string;
  /** Card id from the `item` search parameter; unknown ids show nothing. */
  selectedItemId?: string;
  /**
   * Opening passes the id (push). Closing or clearing a stale id passes
   * undefined with `replace` so Back does not reopen the sheet.
   */
  onSelectedItemChange?: (
    id: string | undefined,
    options?: { replace?: boolean },
  ) => void;
}) {
  const {
    board,
    columns,
    isLoading,
    error,
    search,
    filters,
    moveCard,
    reorderColumns,
    createColumn,
    updateColumn,
    deleteColumn,
  } = useBoardKanban(boardId, workspaceId);

  // Read once so every card judges "overdue" against the same instant.
  const [now] = useState(() => new Date());
  const cardCopy = useKanbanCardCopy();
  const toolbarCopy = useKanbanToolbarCopy();
  // Held here until the filter moves to the route `filter` parameter; the
  // current hook applies the priority, assignee and label facets.
  const [toolbarFilter, setToolbarFilter] =
    useState<KanbanFilterState>(emptyKanbanFilter);
  const duplicateCard = useDuplicateCard(boardId, workspaceId);
  const deleteCard = useDeleteCard(boardId, workspaceId);
  const createCard = useCreateCard(boardId, workspaceId);

  const handleOpenDetail = useCallback(
    (cardId: string) => {
      onSelectedItemChange?.(cardId);
    },
    [onSelectedItemChange],
  );

  const handleClosePanel = useCallback(() => {
    onSelectedItemChange?.(undefined, { replace: true });
  }, [onSelectedItemChange]);

  const boardRef = useRef<HTMLDivElement>(null);
  // Radix returns focus to the element focused before the sheet opened, which
  // is lost on a deep link or a re-render. Put it back on the opening card, or
  // on the board region when that card is gone.
  const restoreFocus = useCallback((cardId: string) => {
    const board = boardRef.current;
    if (!board) return;
    const opener = Array.from(
      board.querySelectorAll<HTMLElement>("[data-card-id]"),
    ).find((element) => element.dataset.cardId === cardId);
    (opener ?? board).focus();
  }, []);

  if (isLoading) return <KanbanSkeleton />;
  if (error || !board) {
    return <KanbanUnavailableState />;
  }

  const handleCreateCard = () => {
    const firstCol = columns[0];
    if (firstCol) {
      document.getElementById(`add-card-${firstCol.id}`)?.focus();
    }
  };

  const handleFilterChange = (next: KanbanFilterState) => {
    setToolbarFilter(next);
    filters.setValues("priority", next.priorities);
    filters.setValues("assigneeId", next.assigneeIds);
    filters.setValues("labelId", next.labelIds);
  };

  return (
    <div
      ref={boardRef}
      data-kanban-board=""
      tabIndex={-1}
      className="h-full min-h-0 overflow-hidden bg-card font-body select-none flex flex-col focus:outline-hidden"
    >
      <div className="flex h-full min-h-0 flex-col overflow-hidden bg-card px-4 sm:px-6">
        <KanbanToolbar
          copy={toolbarCopy}
          searchQuery={search.query}
          onSearchChange={search.setQuery}
          filter={toolbarFilter}
          filterOptions={{
            statuses: [],
            assignees: board.members.map((member) => ({
              id: member.userId,
              name: member.name,
            })),
            labels: [],
          }}
          onFilterChange={handleFilterChange}
          groupByFields={[]}
          groupByFieldId={null}
          onGroupByChange={() => undefined}
          canAdd
          onAddItem={handleCreateCard}
        />

        <div className="min-h-0 flex-1 pt-4 overflow-hidden flex flex-col">
          <KanbanBoard
            board={board}
            columns={columns}
            now={now}
            cardCopy={cardCopy}
            onOpenDetails={handleOpenDetail}
            onMoveCard={(cardId, listId, position) => {
              moveCard.mutate({ cardId, listId, position });
            }}
            onReorderColumns={(updated) => {
              reorderColumns.mutate(updated);
            }}
            onCopyCardLink={(cardId) => {
              void navigator.clipboard.writeText(
                buildCardLink(window.location, cardId),
              );
              toast.success("Card link copied to clipboard.");
            }}
            onAdd={(title) => {
              const lastPos = columns.at(-1)?.position;
              createColumn.mutate({
                title,
                position: generatePosition(lastPos, undefined),
              });
            }}
            onRenameColumn={(listId, title) => {
              updateColumn.mutate({ listId, title });
            }}
            onColorChangeColumn={(listId, color) => {
              updateColumn.mutate({ listId, color });
            }}
            onDeleteColumn={(listId) => {
              deleteColumn.mutate(listId);
            }}
            onDuplicateCard={(cardId) => {
              duplicateCard.mutate(cardId);
            }}
            onDeleteCard={(cardId) => {
              deleteCard.mutate(cardId);
            }}
            onCreateCard={(listId, title, position) => {
              createCard.mutate({ listId, title, position });
            }}
          />
        </div>
      </div>

      {selectedItemId ? (
        <KanbanSelectedItem
          board={board}
          cardId={selectedItemId}
          onClose={handleClosePanel}
          onRestoreFocus={restoreFocus}
        />
      ) : null}
    </div>
  );
}

/**
 * Card link for the clipboard: the current board path with the `item`
 * parameter set. Other parameters, `view` included, are kept.
 */
function buildCardLink(
  location: Pick<Location, "origin" | "pathname" | "search">,
  cardId: string,
): string {
  const params = new URLSearchParams(location.search);
  if (!params.has("view")) params.set("view", "kanban");
  params.set("item", cardId);
  return `${location.origin}${location.pathname}?${params.toString()}`;
}

/**
 * Resolves the selected card and its detail state, then hands them to the
 * pure sheet. An id that fails to load, or resolves to a card outside this
 * board and workspace, is stale: nothing renders and the `item` parameter is
 * cleared with history replace. A cached card is never trusted once the query
 * has errored (a deleted card stays in the query cache).
 */
function KanbanSelectedItem({
  board,
  cardId,
  onClose,
  onRestoreFocus,
}: {
  board: Board;
  cardId: string;
  onClose: () => void;
  onRestoreFocus: (cardId: string) => void;
}) {
  const { card, error } = useCard(cardId, board.workspaceId);
  const notFound =
    Boolean(error) ||
    (card !== undefined &&
      (card.boardId !== board.id || card.workspaceId !== board.workspaceId));

  const onCloseRef = useRef(onClose);
  useEffect(() => {
    onCloseRef.current = onClose;
  });
  useEffect(() => {
    if (notFound) onCloseRef.current();
  }, [notFound, cardId]);

  if (notFound || !card) return null;
  return (
    <KanbanSelectedItemReady
      board={board}
      card={card}
      onClose={onClose}
      onCloseAutoFocus={(event) => {
        event.preventDefault();
        onRestoreFocus(card.id);
      }}
    />
  );
}

function KanbanSelectedItemReady({
  board,
  card,
  onClose,
  onCloseAutoFocus,
}: {
  board: Board;
  card: CardDetail;
  onClose: () => void;
  onCloseAutoFocus: (event: Event) => void;
}) {
  const files = useCardFiles(card.id, card.workspaceId);
  const activity = useCardActivity(card.id, card.workspaceId);
  const updates = useCardComments(card.id, card.workspaceId);
  const updateCard = useUpdateCard(card.boardId, card.workspaceId);
  const deleteCard = useDeleteCard(card.boardId, card.workspaceId);
  const duplicateCard = useDuplicateCard(card.boardId, card.workspaceId);
  const updateFieldValue = useUpdateFieldValue(card.boardId, card.workspaceId);
  const createUpdate = useCreateCardUpdate(card.id, card.workspaceId);
  const updateUpdate = useUpdateCardUpdate(card.id, card.workspaceId);
  const deleteUpdate = useDeleteCardUpdate(card.id, card.workspaceId);

  return (
    <KanbanCardDetailPanel
      board={board}
      card={card}
      onCloseAutoFocus={onCloseAutoFocus}
      detailData={{
        files: files.data ?? card.files,
        filesLoading: files.isLoading,
        activity: activity.data ?? card.activity,
        activityLoading: activity.isLoading,
        activityFetching: activity.isFetching,
        updates: updates.data ?? card.updates,
        updatesLoading: updates.isLoading,
      }}
      callbacks={{
        onClose,
        onRenameTitle: (cardId, patch) => updateCard.mutate({ cardId, patch }),
        onToggleWatch: () => undefined,
        onDuplicate: (cardId) => duplicateCard.mutate(cardId),
        onArchive: (cardId) => {
          deleteCard.mutate(cardId);
          onClose();
        },
        onUpdateFieldValue: (payload) => updateFieldValue.mutate(payload),
        onRefreshActivity: () => {
          void activity.refetch();
        },
        onCreateUpdate: (input, options) =>
          createUpdate.mutate(input, { onSuccess: options?.onSuccess }),
        onUpdateUpdate: (updateId, body) =>
          updateUpdate.mutate({ updateId, body }),
        onDeleteUpdate: (updateId) => deleteUpdate.mutate(updateId),
        onSelectTab: () => undefined,
      }}
    />
  );
}
