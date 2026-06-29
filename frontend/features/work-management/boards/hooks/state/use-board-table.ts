"use client"

import { useCallback, useMemo, useState } from "react"
import { getCoreRowModel, useReactTable, type ColumnDef } from "@tanstack/react-table"
import type { Card } from "@/features/work-management/types"
import { useBoardColumns } from "@/features/work-management/fields/hooks/queries/use-board-columns"
import { useBoardGroups } from "@/features/work-management/groups/hooks/queries/use-board-groups"
import { useBoardView } from "@/features/work-management/boards/hooks/state/use-board-view"
import { useColumnResize } from "@/features/work-management/fields/hooks/state/use-column-resize"
import { useColumnVisibility } from "@/features/work-management/fields/hooks/state/use-column-visibility"
import { useFullBoard } from "@/features/work-management/boards/hooks/queries/use-full-board"
import { useTableFilters } from "@/features/work-management/boards/hooks/state/use-table-filters"
import { useTableSearch } from "@/features/work-management/boards/hooks/state/use-table-search"
import { useTableSort } from "@/features/work-management/boards/hooks/state/use-table-sort"

export function useBoardTable(boardId: string, workspaceId: string) {
  const fullBoard = useFullBoard(boardId, workspaceId)
  const { viewConfig, updateViewConfig } = useBoardView(boardId, workspaceId)
  const columns = useBoardColumns(fullBoard.fieldDefinitions, viewConfig)
  const { groups: allGroups, collapsedGroups, toggleGroup } = useBoardGroups(fullBoard.groups, viewConfig, updateViewConfig)
  const { query, setQuery, debouncedQuery, filteredGroups } = useTableSearch(allGroups)
  const { resizeColumn } = useColumnResize(viewConfig, updateViewConfig)
  const columnVisibility = useColumnVisibility(viewConfig, updateViewConfig)
  const tableFilters = useTableFilters(viewConfig, updateViewConfig)
  const tableSort = useTableSort(viewConfig, updateViewConfig)
  const [selectedCardIds, setSelectedCardIds] = useState<string[]>([])

  const groups = filteredGroups
  const cards = useMemo(() => groups.flatMap((group) => group.cards), [groups])
  const tableColumnDefs = useMemo<ColumnDef<Card>[]>(
    () =>
      columns.map((column) => ({
        id: column.id,
        accessorFn: (card) => card.fieldValues[column.field.id],
        header: column.field.name,
      })),
    [columns]
  )
  const tanstackTable = useReactTable({
    data: cards,
    columns: tableColumnDefs,
    getCoreRowModel: getCoreRowModel(),
  })
  const selectedCardIdSet = useMemo(() => new Set(selectedCardIds), [selectedCardIds])

  const toggleCardSelection = useCallback((cardId: string) => {
    setSelectedCardIds((current) =>
      current.includes(cardId) ? current.filter((id) => id !== cardId) : [...current, cardId]
    )
  }, [])

  const setCardSelected = useCallback((cardId: string, selected: boolean) => {
    setSelectedCardIds((current) => {
      if (selected && !current.includes(cardId)) return [...current, cardId]
      if (!selected) return current.filter((id) => id !== cardId)
      return current
    })
  }, [])

  const toggleAllSelection = useCallback(() => {
    setSelectedCardIds((current) => (current.length === cards.length ? [] : cards.map((card) => card.id)))
  }, [cards])

  const clearSelection = useCallback(() => setSelectedCardIds([]), [])

  return {
    ...fullBoard,
    columns,
    groups,
    viewConfig,
    updateViewConfig,
    resizeColumn,
    columnVisibility,
    tableFilters,
    tableSort,
    search: { query, setQuery, debouncedQuery },
    tanstackTable,
    collapsedGroups,
    toggleGroup,
    selectedCardIds,
    selectedCardIdSet,
    selectionState: {
      selectedCardIds,
      isAllSelected: cards.length > 0 && selectedCardIds.length === cards.length,
    },
    toggleCardSelection,
    setCardSelected,
    toggleAllSelection,
    clearSelection,
  }
}
