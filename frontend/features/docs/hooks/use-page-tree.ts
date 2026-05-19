"use client"

import { useMemo } from "react"
import { useQuery } from "@tanstack/react-query"
import { queryKeys } from "@/lib/query/query-keys"
import { DOCS_STALE_TIME } from "../constants"
import { pagesApi } from "../api/pages-api"

export function usePageTree(workspaceId: string) {
  return useQuery({
    queryKey: queryKeys.pages.tree(workspaceId),
    queryFn: () => pagesApi.getTree(workspaceId),
    enabled: !!workspaceId,
    staleTime: DOCS_STALE_TIME,
  })
}

export function usePageList(workspaceId: string) {
  return useQuery({
    queryKey: queryKeys.pages.list(workspaceId),
    queryFn: () => pagesApi.getList(workspaceId),
    enabled: !!workspaceId,
    staleTime: DOCS_STALE_TIME,
  })
}

export function useDocsOverview(workspaceId: string) {
  const listQuery = usePageList(workspaceId)
  const overview = useMemo(() => {
    const pages = listQuery.data ?? []
    const published = pages.filter((page) => page.status === "published").length
    const shared = pages.filter((page) => page.isShared).length
    const review = pages.filter((page) => page.status === "review").length
    return { total: pages.length, published, shared, review }
  }, [listQuery.data])

  return { ...listQuery, overview }
}
