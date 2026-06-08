"use client"

import { useQuery } from "@tanstack/react-query"
import { queryKeys } from "@/lib/query/query-keys"
import { workspaceApi } from "../../api/workspace.api"

export function useWorkspaceList() {
  return useQuery({
    queryKey: queryKeys.workspaces.all,
    queryFn: () => workspaceApi.listWorkspaces(),
    staleTime: 60_000,
  })
}

export function useWorkspace(workspaceId: string) {
  return useQuery({
    queryKey: queryKeys.workspaces.detail(workspaceId),
    queryFn: () => workspaceApi.getWorkspace(workspaceId),
    enabled: Boolean(workspaceId),
    staleTime: 60_000,
  })
}
