"use client"

import { WorkspaceViewContent } from "../../_components/dashboard/workspace-view-content"
import { useWorkspaceTabbedRoute } from "../../_components/shell/workspace-tabbed-shell"

export function DashboardWorkspaceContent() {
  const route = useWorkspaceTabbedRoute()

  if (route.kind !== "dashboard") return null

  return <WorkspaceViewContent workspaceId={route.workspaceId} view={route.activeView} snapshot={route.snapshot} />
}
