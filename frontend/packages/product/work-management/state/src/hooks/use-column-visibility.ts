"use client"

import { useCallback } from "react"
import type { ViewConfig } from "@notrelix/work-management-core"

export function useColumnVisibility(
  viewConfig: ViewConfig,
  updateViewConfig: (patch: Partial<ViewConfig>) => void
) {
  const hideColumn = useCallback(
    (columnId: string) => {
      if (viewConfig.hiddenFields.includes(columnId)) return
      updateViewConfig({ hiddenFields: [...viewConfig.hiddenFields, columnId] })
    },
    [updateViewConfig, viewConfig.hiddenFields]
  )

  const showColumn = useCallback(
    (columnId: string) => {
      updateViewConfig({ hiddenFields: viewConfig.hiddenFields.filter((id) => id !== columnId) })
    },
    [updateViewConfig, viewConfig.hiddenFields]
  )

  const setHiddenColumns = useCallback(
    (hiddenFields: string[]) => updateViewConfig({ hiddenFields }),
    [updateViewConfig]
  )

  return { hiddenFields: viewConfig.hiddenFields, hideColumn, showColumn, setHiddenColumns }
}
