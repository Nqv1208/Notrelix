"use client"

import { useQuery } from "@tanstack/react-query"
import { queryKeys } from "@/lib/query/query-keys"
import { pageActivityApi } from "../../api/page-activity.api"

export function usePageHistory(pageId: string) {
  return useQuery({
    queryKey: queryKeys.pages.history(pageId),
    queryFn: () => pageActivityApi.getHistory(pageId),
    enabled: !!pageId,
    staleTime: 20_000,
  })
}
