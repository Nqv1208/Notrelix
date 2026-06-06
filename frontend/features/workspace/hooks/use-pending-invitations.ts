"use client"

import { useQuery } from "@tanstack/react-query"
import { queryKeys } from "@/lib/query/query-keys"
import { workspaceService } from "../api/workspace.service"
import { useAuthUser } from "@/features/auth/hooks/useAuthUser"

export function usePendingInvitations() {
  const { isAuthenticated } = useAuthUser()

  return useQuery({
    queryKey: queryKeys.workspaces.pendingInvitations,
    queryFn: () => workspaceService.getPendingInvitations(),
    enabled: isAuthenticated,
    staleTime: 60 * 1000, // 1 minute
    refetchInterval: 30 * 1000, // Poll every 30 seconds for real-time invitation notification
  })
}
