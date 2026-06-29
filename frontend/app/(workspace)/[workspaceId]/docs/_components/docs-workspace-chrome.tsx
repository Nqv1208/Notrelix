"use client"

import type { ReactNode } from "react"
import { useMemo } from "react"
import { useWorkspaceSnapshot, useWorkspaceViews, type WorkspaceView } from "@/features/workspace"
import { WorkspaceTabbedError, WorkspaceTabbedShell, WorkspaceTabbedSkeleton } from "../../_components/shell/workspace-tabbed-shell"

export function DocsWorkspaceChrome({
  workspaceId,
  pageId,
  children,
  contentClassName,
  showToolbar = true,
}: {
  workspaceId: string
  pageId?: string
  children: ReactNode
  contentClassName?: string
  showToolbar?: boolean
}) {
  const snapshot = useWorkspaceSnapshot(workspaceId)
  const viewsQuery = useWorkspaceViews(workspaceId)
  const activeView = useMemo(() => resolveDocView(viewsQuery.data ?? [], workspaceId, pageId), [pageId, viewsQuery.data, workspaceId])
  const views = useMemo(() => ensureViewInTabs(viewsQuery.data ?? [], activeView), [activeView, viewsQuery.data])

  if (snapshot.isLoading || viewsQuery.isLoading) return <WorkspaceTabbedSkeleton />

  if (snapshot.error || viewsQuery.error || !snapshot.data || !activeView) {
    return <WorkspaceTabbedError />
  }

  return (
    <WorkspaceTabbedShell
      workspaceId={workspaceId}
      snapshot={snapshot.data}
      views={views}
      activeView={activeView}
      contentClassName={contentClassName}
      showToolbar={showToolbar}
    >
      {children}
    </WorkspaceTabbedShell>
  )
}

function resolveDocView(views: WorkspaceView[], workspaceId: string, pageId?: string) {
  const matchingPageView = pageId ? views.find((view) => view.type === "doc" && view.target.pageId === pageId) : undefined
  const fallbackDocView = views.find((view) => view.id === "doc" || view.type === "doc")
  const activeView = matchingPageView ?? fallbackDocView ?? createDocView(workspaceId, pageId)

  if (!pageId || activeView.target.pageId === pageId) return activeView

  return {
    ...activeView,
    target: {
      ...activeView.target,
      pageId,
    },
  }
}

function ensureViewInTabs(views: WorkspaceView[], activeView: WorkspaceView) {
  return views.some((view) => view.id === activeView.id) ? views : [...views, activeView]
}

function createDocView(workspaceId: string, pageId?: string): WorkspaceView {
  return {
    id: "doc",
    workspaceId,
    name: "Doc",
    type: "doc",
    icon: "Doc",
    description: "Workspace document view.",
    target: pageId ? { pageId } : {},
    config: {},
    visibility: "workspace",
    isDefault: false,
    position: 99,
    createdAt: new Date().toISOString(),
  }
}
