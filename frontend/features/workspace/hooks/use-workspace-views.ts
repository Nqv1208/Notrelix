"use client"

import { useQuery } from "@tanstack/react-query"
import { queryKeys } from "@/lib/query/query-keys"
import { workspaceService } from "../api/workspace.service"

export function useWorkspaceViews(workspaceId: string) {
  return useQuery({
    queryKey: queryKeys.workspaces.views(workspaceId),
    queryFn: () => workspaceService.getViews(workspaceId),
    enabled: Boolean(workspaceId),
    staleTime: 30_000,
  })
}
