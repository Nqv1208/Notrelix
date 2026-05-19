"use client"

import { useQuery } from "@tanstack/react-query"
import { queryKeys } from "@/lib/query/query-keys"
import { workspacesApi } from "../api/workspaces-api"

export function useWorkspace(slug: string) {
  return useQuery({
    queryKey: queryKeys.workspaces.detail(slug),
    queryFn: () => workspacesApi.getWorkspace(slug),
    enabled: Boolean(slug),
    staleTime: 60_000,
  })
}

export function useWorkspaceSnapshot(slug: string) {
  return useQuery({
    queryKey: queryKeys.workspaces.snapshot(slug),
    queryFn: () => workspacesApi.getSnapshot(slug),
    enabled: Boolean(slug),
    staleTime: 30_000,
  })
}
