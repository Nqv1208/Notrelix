"use client"

import { useQuery } from "@tanstack/react-query"
import { queryKeys } from "@/lib/query/query-keys"
import { pagesApi } from "../api/pages-api"

export function usePageHistory(pageId: string) {
  return useQuery({
    queryKey: queryKeys.pages.history(pageId),
    queryFn: () => pagesApi.getHistory(pageId),
    enabled: !!pageId,
    staleTime: 20_000,
  })
}
