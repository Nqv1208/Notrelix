"use client"

import { useQuery } from "@tanstack/react-query"
import { queryKeys } from "@/lib/query/query-keys"
import { BLOCKS_STALE_TIME } from "../constants"
import { blocksApi } from "../api/blocks-api"

export function usePageBlocks(pageId: string) {
  return useQuery({
    queryKey: queryKeys.pages.blocks(pageId),
    queryFn: () => blocksApi.getByPage(pageId),
    enabled: !!pageId,
    staleTime: BLOCKS_STALE_TIME,
  })
}
