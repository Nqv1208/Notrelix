"use client"

import { useQuery } from "@tanstack/react-query"
import { queryKeys } from "@/lib/query/query-keys"
import { pageService } from "../api/page.service"

export function usePageHistory(pageId: string) {
  return useQuery({
    queryKey: queryKeys.pages.history(pageId),
    queryFn: () => pageService.getHistory(pageId),
    enabled: !!pageId,
    staleTime: 20_000,
  })
}
