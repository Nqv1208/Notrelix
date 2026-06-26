"use client"

import { useCallback } from "react"
import { useBoardKanban, useSelectedCardPanel } from "@/features/work-management"
import { useDuplicateCard, useDeleteCard } from "@/features/work-management"
import { generatePosition } from "@/features/work-management"
import { KanbanToolbar } from "@/features/work-management/boards/components/views/kanban/kanban-toolbar"
import { KanbanBoard } from "@/features/work-management/boards/components/views/kanban/kanban-board"
import { KanbanCardDetailPanel } from "@/features/work-management/boards/components/views/kanban/kanban-card-detail-panel"
import { KanbanSkeleton } from "@/features/work-management/boards/components/views/kanban/kanban-skeleton"
import { AlertCircle } from "lucide-react"

export function KanbanView({
  boardId,
  workspaceId,
}: {
  boardId: string
  workspaceId: string
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
  } = useBoardKanban(boardId, workspaceId)

  const { selectedCardId, openCard, closePanel } = useSelectedCardPanel()
  const duplicateCard = useDuplicateCard(boardId, workspaceId)
  const deleteCard = useDeleteCard(boardId, workspaceId)

  const handleOpenDetail = useCallback(
    (cardId: string) => {
      openCard(cardId)
    },
    [openCard]
  )

  const handleClosePanel = useCallback(() => {
    closePanel()
  }, [closePanel])

  if (isLoading) return <KanbanSkeleton />
  if (error || !board) {
    return (
      <div className="p-4 sm:p-6">
        <div className="rounded-2xl border border-border bg-card p-8 text-center shadow-xs">
          <AlertCircle className="mx-auto mb-3 size-8 text-destructive" />
          <h2 className="text-lg font-semibold text-foreground font-display">Board unavailable</h2>
          <p className="mt-2 text-sm text-muted-foreground">The Kanban board could not be loaded.</p>
        </div>
      </div>
    )
  }

  const handleCreateCard = () => {
    const firstCol = columns[0]
    if (firstCol) {
      document.getElementById(`add-card-${firstCol.id}`)?.focus()
    }
  }

  const handleCreateColumn = () => {
    const lastPos = columns.at(-1)?.position
    createColumn.mutate({
      title: "New Column",
      color: "#6161ff",
      position: generatePosition(lastPos, undefined),
    })
  }

  return (
    <div className="h-full min-h-0 overflow-hidden bg-card font-body select-none flex flex-col">
      <div className="flex h-full min-h-0 flex-col overflow-hidden bg-card px-4 sm:px-6">
        <KanbanToolbar
          searchQuery={search.query}
          onSearchChange={search.setQuery}
          filters={filters.values}
          onFilterChange={filters.setValues}
          onClearFilters={filters.clear}
          activeSort="position"
          onSortChange={() => {}}
          onCreateCard={handleCreateCard}
          onCreateColumn={handleCreateColumn}
        />

        <div className="min-h-0 flex-1 pt-4 overflow-hidden flex flex-col">
          <KanbanBoard
            board={board}
            columns={columns}
            workspaceId={workspaceId}
            onOpenDetails={handleOpenDetail}
            onMoveCard={(cardId, listId, position) => {
              moveCard.mutate({ cardId, listId, position })
            }}
            onReorderColumns={(updated) => {
              reorderColumns.mutate(updated)
            }}
            onCreateColumn={(title) => {
              const lastPos = columns.at(-1)?.position
              createColumn.mutate({ title, position: generatePosition(lastPos, undefined) })
            }}
            onRenameColumn={(listId, title) => {
              updateColumn.mutate({ listId, title })
            }}
            onColorChangeColumn={(listId, color) => {
              updateColumn.mutate({ listId, color })
            }}
            onDeleteColumn={(listId) => {
              deleteColumn.mutate(listId)
            }}
            onDuplicateCard={(cardId) => {
              duplicateCard.mutate(cardId)
            }}
            onDeleteCard={(cardId) => {
              deleteCard.mutate(cardId)
            }}
          />
        </div>
      </div>

      <KanbanCardDetailPanel
        board={board}
        cardId={selectedCardId}
        open={Boolean(selectedCardId)}
        onClose={handleClosePanel}
      />
    </div>
  )
}
