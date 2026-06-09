import { useMemo } from "react"
import { useFullBoard } from "./use-full-board"
import type { BoardGroup } from "../types"

export function useKanbanColumns(boardId: string, workspaceId: string) {
  const { groups, isLoading, error } = useFullBoard(boardId, workspaceId)

  const sortedColumns = useMemo(() => {
    if (!groups) return []
    return [...groups].sort((a, b) => a.position - b.position)
  }, [groups])

  return {
    columns: sortedColumns,
    isLoading,
    error,
  }
}
