"use client"

import { useQuery } from "@tanstack/react-query"
import { queryKeys } from "@/lib/query/query-keys"
import { workspaceService } from "../api/workspace.service"

export function useWorkspaceList() {
  return useQuery({
    queryKey: queryKeys.workspaces.all,
    queryFn: () => workspaceService.listWorkspaces(),
    staleTime: 60_000,
  })
}

export function useWorkspace(workspaceId: string) {
  return useQuery({
    queryKey: queryKeys.workspaces.detail(workspaceId),
    queryFn: () => workspaceService.getWorkspace(workspaceId),
    enabled: Boolean(workspaceId),
    staleTime: 60_000,
  })
}

export function useWorkspaceSnapshot(workspaceId: string) {
  return useQuery({
    queryKey: queryKeys.workspaces.snapshot(workspaceId),
    queryFn: () => workspaceService.getSnapshot(workspaceId),
    enabled: Boolean(workspaceId),
    staleTime: 30_000,
  })
}
