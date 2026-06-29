"use client"

import { useQuery } from "@tanstack/react-query"
import { queryKeys } from "@/lib/query/query-keys"
import { pageApi } from "../../api/page.api"

export function usePage(pageId: string) {
  return useQuery({
    queryKey: queryKeys.pages.detail(pageId),
    queryFn: ({ signal }) => pageApi.getDetail(pageId, { signal }),
    enabled: !!pageId,
    staleTime: 30_000,
  })
}

export function usePageBreadcrumb(pageId: string) {
  return useQuery({
    queryKey: queryKeys.pages.breadcrumb(pageId),
    queryFn: ({ signal }) => pageApi.getBreadcrumb(pageId, { signal }),
    enabled: !!pageId,
    staleTime: 60_000,
  })
}
