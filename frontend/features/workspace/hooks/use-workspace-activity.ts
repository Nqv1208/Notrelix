"use client"

import { useQuery } from "@tanstack/react-query"
import { queryKeys } from "@/lib/query/query-keys"
import { workspaceService } from "../api/workspace.service"

export function useWorkspaceActivity(workspaceId: string, page = 1, pageSize = 20) {
  return useQuery({
    queryKey: [...queryKeys.workspaces.activity(workspaceId), page, pageSize],
    queryFn: () => workspaceService.getActivityLogs(workspaceId, page, pageSize),
    enabled: Boolean(workspaceId),
    staleTime: 30_000,
  })
}
