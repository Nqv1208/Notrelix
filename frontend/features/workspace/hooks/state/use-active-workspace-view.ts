"use client"

import { useMemo } from "react"
import type { WorkspaceView } from "../../types"
import { useWorkspaceViews } from "../queries/use-workspace-views"

const aliases = new Set(["table", "doc", "kanban", "calendar", "timeline", "dashboard"])

export function resolveWorkspaceView(views: WorkspaceView[], requestedView?: string | null) {
  if (requestedView) {
    const direct = views.find((view) => view.id === requestedView)
    if (direct) return direct
    if (aliases.has(requestedView)) {
      const byType = views.find((view) => view.type === requestedView)
      if (byType) return byType
    }
  }
  return views.find((view) => view.isDefault) ?? views[0]
}

export function useActiveWorkspaceView(workspaceId: string, requestedView?: string | null) {
  const query = useWorkspaceViews(workspaceId)
  const activeView = useMemo(() => resolveWorkspaceView(query.data ?? [], requestedView), [query.data, requestedView])

  return {
    views: query.data ?? [],
    activeView,
    isLoading: query.isLoading,
    error: query.error,
  }
}
