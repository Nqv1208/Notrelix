"use client"

import { useQuery } from "@tanstack/react-query"
import { queryKeys } from "@/lib/query/query-keys"
import { workspaceService } from "../api/workspace.service"

export function useInvitationByToken(token: string) {
  return useQuery({
    queryKey: queryKeys.workspaces.invitationByToken(token),
    queryFn: () => workspaceService.getInvitationByToken(token),
    enabled: Boolean(token),
    staleTime: 5 * 60 * 1000, // 5 minutes
    retry: false, // Don't retry if token is invalid/not found
  })
}
