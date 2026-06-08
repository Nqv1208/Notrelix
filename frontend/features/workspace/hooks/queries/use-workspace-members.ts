"use client"

import { useQuery } from "@tanstack/react-query"
import { queryKeys } from "@/lib/query/query-keys"
import { membersApi } from "../../api/members.api"

export function useWorkspaceMembers(workspaceId: string) {
  return useQuery({
    queryKey: queryKeys.workspaces.members(workspaceId),
    queryFn: () => membersApi.getMembers(workspaceId),
    enabled: Boolean(workspaceId),
    staleTime: 30_000,
  })
}
