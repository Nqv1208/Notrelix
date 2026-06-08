"use client"

import { useQuery } from "@tanstack/react-query"
import { queryKeys } from "@/lib/query/query-keys"
import { activityApi } from "../../api/activity.api"

export function useWorkspaceActivity(workspaceId: string, page = 1, pageSize = 20) {
  return useQuery({
    queryKey: [...queryKeys.workspaces.activity(workspaceId), page, pageSize],
    queryFn: () => activityApi.getActivityLogs(workspaceId, page, pageSize),
    enabled: Boolean(workspaceId),
    staleTime: 30_000,
  })
}
