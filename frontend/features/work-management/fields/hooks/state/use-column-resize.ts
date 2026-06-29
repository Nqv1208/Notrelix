"use client"

import { useResizeColumn } from "@/features/work-management/fields/hooks/state/use-resize-column"
import type { ViewConfig } from "@/features/work-management/types"

export function useColumnResize(
  viewConfig: ViewConfig,
  updateViewConfig: (patch: Partial<ViewConfig>) => void
) {
  return useResizeColumn(viewConfig, updateViewConfig)
}
