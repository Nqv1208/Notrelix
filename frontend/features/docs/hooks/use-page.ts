"use client"

import { useQuery } from "@tanstack/react-query"
import { queryKeys } from "@/lib/query/query-keys"
import { pagesApi } from "../api/pages-api"

export function usePage(pageId: string) {
  return useQuery({
    queryKey: queryKeys.pages.detail(pageId),
    queryFn: () => pagesApi.getDetail(pageId),
    enabled: !!pageId,
    staleTime: 30_000,
  })
}

export function usePageBreadcrumb(pageId: string) {
  return useQuery({
    queryKey: queryKeys.pages.breadcrumb(pageId),
    queryFn: () => pagesApi.getBreadcrumb(pageId),
    enabled: !!pageId,
    staleTime: 60_000,
  })
}
