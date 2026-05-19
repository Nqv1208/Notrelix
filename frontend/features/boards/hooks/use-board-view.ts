"use client"

import { useCallback, useMemo, useState } from "react"
import type { ViewConfig, ViewMode } from "../types"

const defaultViewConfig: ViewConfig = {
  groupBy: "list",
  hiddenFields: [],
  columnOrder: [],
  columnWidths: {},
  filters: [],
  sortBy: [],
}

export function useBoardView(boardId: string) {
  const storageKey = `notrelix:board-view:${boardId}`
  const [state, setState] = useState(() => {
    if (typeof window === "undefined") return { viewMode: "table" as ViewMode, viewConfig: defaultViewConfig }
    const raw = window.localStorage.getItem(`notrelix:board-view:${boardId}`)
    if (!raw) return { viewMode: "table" as ViewMode, viewConfig: defaultViewConfig }
    try {
      const parsed = JSON.parse(raw) as { viewMode?: ViewMode; viewConfig?: ViewConfig }
      return {
        viewMode: parsed.viewMode ?? ("table" as ViewMode),
        viewConfig: { ...defaultViewConfig, ...parsed.viewConfig },
      }
    } catch {
      window.localStorage.removeItem(storageKey)
      return { viewMode: "table" as ViewMode, viewConfig: defaultViewConfig }
    }
  })

  const persist = useCallback((nextMode: ViewMode, nextConfig: ViewConfig) => {
    window.localStorage.setItem(storageKey, JSON.stringify({ viewMode: nextMode, viewConfig: nextConfig }))
  }, [storageKey])

  const setViewMode = useCallback((mode: ViewMode) => {
    setState((current) => {
      persist(mode, current.viewConfig)
      return { ...current, viewMode: mode }
    })
  }, [persist])

  const updateViewConfig = useCallback((patch: Partial<ViewConfig>) => {
    setState((current) => {
      const nextConfig = { ...current.viewConfig, ...patch }
      persist(current.viewMode, nextConfig)
      return { ...current, viewConfig: nextConfig }
    })
  }, [persist])

  return useMemo(
    () => ({ viewMode: state.viewMode, viewConfig: state.viewConfig, setViewMode, updateViewConfig }),
    [setViewMode, state.viewConfig, state.viewMode, updateViewConfig]
  )
}
