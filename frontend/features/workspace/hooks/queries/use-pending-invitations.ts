"use client"

import { useQuery } from "@tanstack/react-query"
import { useAuthUser } from "@/features/auth"
import { queryKeys } from "@/lib/query/query-keys"
import { invitationsApi } from "../../api/invitations.api"

export function usePendingInvitations() {
  const { isAuthenticated } = useAuthUser()

  return useQuery({
    queryKey: queryKeys.workspaces.pendingInvitations,
    queryFn: () => invitationsApi.getPendingInvitations(),
    enabled: isAuthenticated,
    staleTime: 60 * 1000,
    refetchInterval: 30 * 1000,
  })
}
