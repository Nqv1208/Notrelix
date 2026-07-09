"use client"

import { useCallback } from "react"
import type { FilterConfig, ViewConfig } from "@notrelix/work-management-core"

export function useTableFilters(
  viewConfig: ViewConfig,
  updateViewConfig: (patch: Partial<ViewConfig>) => void
) {
  const setFilters = useCallback(
    (filters: FilterConfig[]) => updateViewConfig({ filters }),
    [updateViewConfig]
  )

  const clearFilters = useCallback(() => updateViewConfig({ filters: [] }), [updateViewConfig])

  return { filters: viewConfig.filters, setFilters, clearFilters }
}
