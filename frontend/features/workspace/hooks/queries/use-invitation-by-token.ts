"use client"

import { useQuery } from "@tanstack/react-query"
import { queryKeys } from "@/lib/query/query-keys"
import { invitationsApi } from "../../api/invitations.api"

export function useInvitationByToken(token: string) {
  return useQuery({
    queryKey: queryKeys.workspaces.invitationByToken(token),
    queryFn: () => invitationsApi.getInvitationByToken(token),
    enabled: Boolean(token),
    staleTime: 5 * 60 * 1000,
    retry: false,
  })
}
