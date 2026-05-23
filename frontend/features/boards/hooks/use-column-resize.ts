"use client"

import { useResizeColumn } from "./use-resize-column"
import type { ViewConfig } from "../types"

export function useColumnResize(
  viewConfig: ViewConfig,
  updateViewConfig: (patch: Partial<ViewConfig>) => void
) {
  return useResizeColumn(viewConfig, updateViewConfig)
}
