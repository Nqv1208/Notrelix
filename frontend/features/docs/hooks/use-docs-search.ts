"use client"

import { useQuery } from "@tanstack/react-query"
import { queryKeys } from "@/lib/query/query-keys"
import { pagesApi } from "../api/pages-api"

export function useDocsSearch(workspaceId: string, query: string) {
  return useQuery({
    queryKey: queryKeys.pages.search(workspaceId, query),
    queryFn: () => pagesApi.search(workspaceId, query),
    enabled: !!workspaceId && query.trim().length >= 2,
    staleTime: 10_000,
  })
}
