import { useCallback, useState } from "react";
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
  onSelectedItemChange?: (id: string | undefined) => void;
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
    onSelectedItemChange?.(undefined);
  }, [onSelectedItemChange]);

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

  const handleCreateColumn = () => {
    const lastPos = columns.at(-1)?.position;
    createColumn.mutate({
      title: "New Column",
      // Default column colour is persisted backend data (BoardGroup colour),
      // not a design token; changing it is a contract change.
      color: "#6161ff",
      position: generatePosition(lastPos, undefined),
    });
  };

  return (
    <div className="h-full min-h-0 overflow-hidden bg-card font-body select-none flex flex-col">
      <div className="flex h-full min-h-0 flex-col overflow-hidden bg-card px-4 sm:px-6">
        <KanbanToolbar
          searchQuery={search.query}
          onSearchChange={search.setQuery}
          onClearFilters={filters.clear}
          activeSort="position"
          onSortChange={() => {}}
          onCreateCard={handleCreateCard}
          onAddColumn={handleCreateColumn}
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
 * pure sheet. An id that does not resolve to a card on this board renders
 * nothing; the URL is not rewritten here.
 */
function KanbanSelectedItem({
  board,
  cardId,
  onClose,
}: {
  board: Board;
  cardId: string;
  onClose: () => void;
}) {
  const { card } = useCard(cardId, board.workspaceId);
  if (!card || card.boardId !== board.id) return null;
  return (
    <KanbanSelectedItemReady
      board={board}
      card={card as CardDetail}
      onClose={onClose}
    />
  );
}

function KanbanSelectedItemReady({
  board,
  card,
  onClose,
}: {
  board: Board;
  card: CardDetail;
  onClose: () => void;
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
