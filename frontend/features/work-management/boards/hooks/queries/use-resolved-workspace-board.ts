"use client"

import { useMemo } from "react"
import type { Board } from "@/features/work-management/types"
import { useWorkspaceBoards } from "@/features/work-management/boards/hooks/queries/use-workspace-boards"

const emptyBoards: Board[] = []

export function useResolvedWorkspaceBoard({
  workspaceId,
  requestedBoardId,
}: {
  workspaceId: string
  requestedBoardId?: string | null
}) {
  const boardsQuery = useWorkspaceBoards(workspaceId)
  const boards = boardsQuery.data ?? emptyBoards

  const board = useMemo(() => {
    if (requestedBoardId) return boards.find((item) => item.id === requestedBoardId) ?? boards[0]
    return boards[0]
  }, [boards, requestedBoardId])

  return {
    boards,
    board,
    boardId: board?.id,
    isLoading: boardsQuery.isLoading,
    isFetching: boardsQuery.isFetching,
    error: boardsQuery.error,
    isEmpty: !boardsQuery.isLoading && boards.length === 0,
  }
}
