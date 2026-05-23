"use client"

import { useCallback } from "react"
import type { SortConfig, ViewConfig } from "../types"

export function useTableSort(
  viewConfig: ViewConfig,
  updateViewConfig: (patch: Partial<ViewConfig>) => void
) {
  const setSort = useCallback(
    (sortBy: SortConfig[]) => updateViewConfig({ sortBy }),
    [updateViewConfig]
  )

  const clearSort = useCallback(() => updateViewConfig({ sortBy: [] }), [updateViewConfig])

  return { sortBy: viewConfig.sortBy, setSort, clearSort }
}
