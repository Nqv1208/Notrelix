"use client"

import { useCallback } from "react"
import type { ViewConfig } from "@/features/work-management/types"

export function useResizeColumn(
  viewConfig: ViewConfig,
  updateViewConfig: (patch: Partial<ViewConfig>) => void
) {
  const resizeColumn = useCallback(
    (columnId: string, width: number) => {
      updateViewConfig({
        columnWidths: {
          ...viewConfig.columnWidths,
          [columnId]: Math.round(width),
        },
      })
    },
    [updateViewConfig, viewConfig.columnWidths]
  )

  return { resizeColumn }
}

