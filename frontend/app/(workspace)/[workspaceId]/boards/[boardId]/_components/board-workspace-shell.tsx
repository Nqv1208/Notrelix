"use client"

import { useEffect, useMemo } from "react"
import { useRouter } from "next/navigation"
import { useWorkspaceSnapshot, useWorkspaceViews } from "@/features/workspace/hooks"
import type { WorkspaceView } from "@/features/workspace/types"
import { getWorkspaceDocHref, getWorkspaceDocsHref } from "@/features/workspace/utils/workspace-routes"
import { WorkspaceTabbedError, WorkspaceTabbedShell, WorkspaceTabbedSkeleton } from "../../../_components/shell/workspace-tabbed-shell"
import { BoardWorkspaceViewContent } from "./board-workspace-view-content"

type BoardRouteViewType = "table" | "kanban" | "calendar" | "timeline"

export function BoardWorkspaceShell({
  workspaceId,
  boardId,
  requestedView,
}: {
  workspaceId: string
  boardId: string
  requestedView?: string | null
}) {
  const router = useRouter()
  const snapshot = useWorkspaceSnapshot(workspaceId)
  const viewsQuery = useWorkspaceViews(workspaceId)
  const views = useBoardRouteViews(viewsQuery.data ?? [], workspaceId, boardId, requestedView)
  const activeView = useMemo(() => resolveBoardRouteView(views, requestedView), [views, requestedView])

  useEffect(() => {
    if (activeView?.type !== "doc") return
    const href = activeView.target.pageId
      ? getWorkspaceDocHref(workspaceId, activeView.target.pageId)
      : getWorkspaceDocsHref(workspaceId)
    router.replace(href as never)
  }, [activeView, router, workspaceId])

  if (snapshot.isLoading || viewsQuery.isLoading || activeView?.type === "doc") return <WorkspaceTabbedSkeleton />

  if (snapshot.error || viewsQuery.error || !snapshot.data || !activeView) {
    return <WorkspaceTabbedError />
  }

  return (
    <WorkspaceTabbedShell
      workspaceId={workspaceId}
      snapshot={snapshot.data}
      views={views}
      activeView={activeView}
      contentClassName="overflow-hidden"
    >
      <BoardWorkspaceViewContent workspaceId={workspaceId} boardId={boardId} view={activeView} />
    </WorkspaceTabbedShell>
  )
}

function useBoardRouteViews(views: WorkspaceView[], workspaceId: string, boardId: string, requestedView?: string | null) {
  return useMemo(() => {
    let mappedViews = views.map((view) => {
      if (!isBoardView(view)) return view
      return {
        ...view,
        target: {
          ...view.target,
          boardId,
        },
      }
    })

    const requestedBoardType = normalizeBoardRouteViewType(requestedView)
    if (requestedBoardType && !mappedViews.some((view) => view.id === requestedView || view.type === requestedBoardType)) {
      mappedViews = [...mappedViews, createSyntheticBoardView(workspaceId, boardId, requestedBoardType)]
    }

    if (mappedViews.some(isBoardView)) return mappedViews
    return [createSyntheticBoardView(workspaceId, boardId, "table"), ...mappedViews]
  }, [boardId, requestedView, views, workspaceId])
}

function resolveBoardRouteView(views: WorkspaceView[], requestedView?: string | null) {
  const tableView = views.find((view) => view.type === "table")
  const firstBoardView = views.find(isBoardView)

  if (requestedView) {
    const direct = views.find((view) => view.id === requestedView) ?? views.find((view) => view.type === requestedView)
    if (direct?.type === "doc") return direct
    if (direct && isBoardView(direct)) return direct
  }

  return tableView ?? firstBoardView
}

function isBoardView(view: WorkspaceView): view is WorkspaceView & { type: BoardRouteViewType } {
  return isBoardRouteViewType(view.type)
}

function isBoardRouteViewType(type: WorkspaceView["type"]): type is BoardRouteViewType {
  return type === "table" || type === "kanban" || type === "calendar" || type === "timeline"
}

function normalizeBoardRouteViewType(value?: string | null): BoardRouteViewType | undefined {
  if (value === "table" || value === "kanban" || value === "calendar" || value === "timeline") return value
  return undefined
}

function createSyntheticBoardView(workspaceId: string, boardId: string, type: BoardRouteViewType): WorkspaceView {
  const now = new Date().toISOString()
  const labels: Record<BoardRouteViewType, { name: string; icon: string; description: string; position: number }> = {
    table: { name: "Main Table", icon: "Table", description: "Workspace tasks in table form", position: 1 },
    kanban: { name: "Kanban", icon: "Kanban", description: "Board cards grouped by list", position: 2 },
    calendar: { name: "Calendar", icon: "Calendar", description: "Board deadlines in calendar form", position: 3 },
    timeline: { name: "Timeline", icon: "Timeline", description: "Board work across time", position: 4 },
  }
  const label = labels[type]

  return {
    id: type,
    workspaceId,
    name: label.name,
    type,
    icon: label.icon,
    description: label.description,
    target: { boardId },
    config: {},
    visibility: "workspace",
    isDefault: type === "table",
    position: label.position,
    createdAt: now,
  }
}
