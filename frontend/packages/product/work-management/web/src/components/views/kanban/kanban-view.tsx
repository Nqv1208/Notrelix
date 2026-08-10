import { useCallback } from "react";
import {
  useBoardKanban,
  useSelectedCardPanel,
} from "@notrelix/work-management-state";
import {
  useDuplicateCard,
  useDeleteCard,
} from "@notrelix/work-management-state";
import { generatePosition } from "@notrelix/work-management-core";
import { KanbanToolbar } from "./kanban-toolbar";
import { KanbanBoard } from "./kanban-board";
import { KanbanCardDetailPanel } from "./kanban-card-detail-panel";
import { KanbanSkeleton } from "./kanban-skeleton";
import { AlertCircle } from "lucide-react";

export function KanbanView({
  boardId,
  workspaceId,
}: {
  boardId: string;
  workspaceId: string;
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

  const {
    selectedCardId: _selectedCardId,
    openCard,
    closePanel,
  } = useSelectedCardPanel();
  const duplicateCard = useDuplicateCard(boardId, workspaceId);
  const deleteCard = useDeleteCard(boardId, workspaceId);

  const handleOpenDetail = useCallback(
    (cardId: string) => {
      openCard(cardId);
    },
    [openCard],
  );

  const handleClosePanel = useCallback(() => {
    closePanel();
  }, [closePanel]);

  if (isLoading) return <KanbanSkeleton />;
  if (error || !board) {
    return (
      <div className="p-4 sm:p-6">
        <div className="rounded-2xl border border-border bg-card p-8 text-center shadow-xs">
          <AlertCircle className="mx-auto mb-3 size-8 text-destructive" />
          <h2 className="text-lg font-semibold text-foreground font-display">
            Board unavailable
          </h2>
          <p className="mt-2 text-sm text-muted-foreground">
            The Kanban board could not be loaded.
          </p>
        </div>
      </div>
    );
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
            workspaceId={workspaceId}
            onOpenDetails={handleOpenDetail}
            onMoveCard={(cardId: any, listId: any, position: any) => {
              moveCard.mutate({ cardId, listId, position });
            }}
            onReorderColumns={(updated: any) => {
              reorderColumns.mutate(updated);
            }}
            onAdd={(title: any) => {
              const lastPos = columns.at(-1)?.position;
              createColumn.mutate({
                title,
                position: generatePosition(lastPos, undefined),
              });
            }}
            onRenameColumn={(listId: any, title: any) => {
              updateColumn.mutate({ listId, title });
            }}
            onColorChangeColumn={(listId: any, color: any) => {
              updateColumn.mutate({ listId, color });
            }}
            onDeleteColumn={(listId: any) => {
              deleteColumn.mutate(listId);
            }}
            onDuplicateCard={(cardId: any) => {
              duplicateCard.mutate(cardId);
            }}
            onDeleteCard={(cardId: any) => {
              deleteCard.mutate(cardId);
            }}
          />
        </div>
      </div>

      <KanbanCardDetailPanel
        board={board}
        card={null}
        workspaceId={workspaceId}
        onClose={handleClosePanel}
      />
    </div>
  );
}
