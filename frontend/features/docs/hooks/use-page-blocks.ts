"use client"

import { useQuery } from "@tanstack/react-query"
import { queryKeys } from "@/lib/query/query-keys"
import { BLOCKS_STALE_TIME } from "../constants"
import { blockService } from "../api/block.service"

export function usePageBlocks(pageId: string) {
  return useQuery({
    queryKey: queryKeys.pages.blocks(pageId),
    queryFn: () => blockService.getByPage(pageId),
    enabled: !!pageId,
    staleTime: BLOCKS_STALE_TIME,
  })
}
