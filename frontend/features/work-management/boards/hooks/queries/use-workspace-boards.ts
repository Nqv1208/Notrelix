"use client"

import { useQuery } from "@tanstack/react-query"
import { queryKeys } from "@/lib/query/query-keys"
import { boardApi } from "@/features/work-management/boards/api/board.api"

export function useWorkspaceBoards(workspaceId: string) {
  return useQuery({
    queryKey: queryKeys.boards.workspaceList(workspaceId),
    queryFn: () => boardApi.getBoardsByWorkspaceId(workspaceId),
    enabled: Boolean(workspaceId),
    staleTime: 30_000,
  })
}
