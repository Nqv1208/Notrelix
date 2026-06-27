"use client"

import { useMemo } from "react"
import { useWorkspaceBoards } from "@/features/work-management"
import { usePageList } from "@/features/docs"
import { resolveWorkspaceViews } from "../../utils/workspace-views"
import { useWorkspace } from "./use-workspace"

export function useWorkspaceViews(workspaceId: string) {
  const workspaceQuery = useWorkspace(workspaceId)
  const boardsQuery = useWorkspaceBoards(workspaceId)
  const pagesQuery = usePageList(workspaceId)

  const data = useMemo(
    () =>
      resolveWorkspaceViews(
        workspaceId,
        boardsQuery.data ?? [],
        pagesQuery.data ?? [],
        workspaceQuery.data
      ),
    [boardsQuery.data, pagesQuery.data, workspaceId, workspaceQuery.data]
  )

  return {
    data,
    isLoading: workspaceQuery.isLoading || boardsQuery.isLoading || pagesQuery.isLoading,
    isFetching: workspaceQuery.isFetching || boardsQuery.isFetching || pagesQuery.isFetching,
    isError: workspaceQuery.isError || boardsQuery.isError || pagesQuery.isError,
    error: workspaceQuery.error ?? boardsQuery.error ?? pagesQuery.error,
    refetch: async () => {
      await Promise.all([
        workspaceQuery.refetch(),
        boardsQuery.refetch(),
        pagesQuery.refetch(),
      ])
    },
  }
}
