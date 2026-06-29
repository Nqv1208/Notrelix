"use client"

import { useMemo } from "react"
import { useWorkspaceBoards } from "@/features/work-management"
import { useQuery } from "@tanstack/react-query"
import { api } from "@/lib/api/api-client"
import { endpoints } from "@/lib/api/endpoints"
import { queryKeys } from "@/lib/query/query-keys"
import { resolveWorkspaceViews } from "../../utils/workspace-views"
import { useWorkspace } from "./use-workspace"

export function useWorkspaceViews(workspaceId: string) {
  const workspaceQuery = useWorkspace(workspaceId)
  const boardsQuery = useWorkspaceBoards(workspaceId)
  const pagesQuery = useQuery({
    queryKey: queryKeys.docs.list(workspaceId),
    queryFn: () => api.get<Array<{ id: string }>>(endpoints.pages.list(workspaceId)),
  })

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
