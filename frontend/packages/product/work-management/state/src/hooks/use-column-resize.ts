"use client"

import { useResizeColumn } from "./use-resize-column"
import type { ViewConfig } from "@notrelix/work-management-core"

export function useColumnResize(
  viewConfig: ViewConfig,
  updateViewConfig: (patch: Partial<ViewConfig>) => void
) {
  return useResizeColumn(viewConfig, updateViewConfig)
}
