import { useMemo } from "react"
import { useFullBoard } from "./use-full-board"
import { useKanbanSearch } from "./use-kanban-search"
import { useKanbanFilters } from "./use-kanban-filters"
import { useMoveCard } from "./use-move-card"
import { useReorderKanbanColumns } from "./use-reorder-kanban-columns"
import { useCreateKanbanColumn } from "./use-create-kanban-column"
import { useUpdateKanbanColumn } from "./use-update-kanban-column"
import { useDeleteKanbanColumn } from "./use-delete-kanban-column"
import type { BoardGroup } from "../types"

export function useBoardKanban(boardId: string, workspaceId: string) {
  const { board, groups: allGroups, isLoading, error } = useFullBoard(boardId, workspaceId)

  // 1. Search (Title-based)
  const { query, setQuery, debouncedQuery, searchedGroups } = useKanbanSearch(allGroups || [])

  // 2. Filters (Status, Priority, Label, Assignee)
  const { filters, setFilterValues, clearFilters, filteredGroups } = useKanbanFilters(searchedGroups)

  // 3. Mutations
  const moveCard = useMoveCard(boardId, workspaceId)
  const reorderColumns = useReorderKanbanColumns(boardId, workspaceId)
  const createColumn = useCreateKanbanColumn(boardId, workspaceId)
  const updateColumn = useUpdateKanbanColumn(boardId, workspaceId)
  const deleteColumn = useDeleteKanbanColumn(boardId, workspaceId)

  const columns = useMemo(() => {
    return filteredGroups.sort((a, b) => a.position - b.position)
  }, [filteredGroups])

  return {
    board,
    columns,
    isLoading,
    error,
    search: {
      query,
      setQuery,
      debouncedQuery,
    },
    filters: {
      values: filters,
      setValues: setFilterValues,
      clear: clearFilters,
    },
    moveCard,
    reorderColumns,
    createColumn,
    updateColumn,
    deleteColumn,
  }
}
