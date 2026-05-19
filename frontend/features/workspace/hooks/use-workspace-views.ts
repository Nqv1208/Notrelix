"use client"

import { useQuery } from "@tanstack/react-query"
import { queryKeys } from "@/lib/query/query-keys"
import { workspacesApi } from "../api/workspaces-api"

export function useWorkspaceViews(slug: string) {
  return useQuery({
    queryKey: queryKeys.workspaces.views(slug),
    queryFn: () => workspacesApi.getViews(slug),
    enabled: Boolean(slug),
    staleTime: 30_000,
  })
}
