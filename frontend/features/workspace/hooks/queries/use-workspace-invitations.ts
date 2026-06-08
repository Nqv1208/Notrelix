"use client"

import { useQuery } from "@tanstack/react-query"
import { queryKeys } from "@/lib/query/query-keys"
import { invitationsApi } from "../../api/invitations.api"

export function useWorkspaceInvitations(workspaceId: string) {
  return useQuery({
    queryKey: queryKeys.workspaces.invitations(workspaceId),
    queryFn: () => invitationsApi.getInvitations(workspaceId),
    enabled: Boolean(workspaceId),
    staleTime: 30_000,
  })
}
