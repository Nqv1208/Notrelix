import type { WorkspaceSummary, WorkspaceView } from "../types"
import { parseSettings } from "./settings"

type ViewTargetItem = {
  id: string
}

export function createDefaultWorkspaceViews(
  workspaceId: string,
  boards: ViewTargetItem[],
  pages: ViewTargetItem[],
  timestamp = new Date().toISOString()
): WorkspaceView[] {
  const firstBoard = boards[0]
  const firstPage = pages[0]
  const views: WorkspaceView[] = []

  if (firstBoard) {
    views.push({
      id: "table",
      workspaceId,
      name: "Main Table",
      type: "table",
      icon: "Table",
      description: "Workspace tasks in table form",
      target: { boardId: firstBoard.id },
      config: {},
      visibility: "workspace",
      isDefault: true,
      position: 1,
      createdAt: timestamp,
    })
    views.push({
      id: "kanban",
      workspaceId,
      name: "Kanban",
      type: "kanban",
      icon: "Kanban",
      description: "Board cards grouped by list",
      target: { boardId: firstBoard.id },
      config: {},
      visibility: "workspace",
      isDefault: false,
      position: 2,
      createdAt: timestamp,
    })
  }

  if (firstPage) {
    views.push({
      id: "docs",
      workspaceId,
      name: "Docs",
      type: "doc",
      icon: "FileText",
      description: "Workspace documentation",
      target: { pageId: firstPage.id },
      config: {},
      visibility: "workspace",
      isDefault: views.length === 0,
      position: 3,
      createdAt: timestamp,
    })
  }

  views.push({
    id: "dashboard",
    workspaceId,
    name: "Dashboard",
    type: "dashboard",
    icon: "Gauge",
    description: "Workspace overview",
    target: {},
    config: {},
    visibility: "workspace",
    isDefault: views.length === 0,
    position: 99,
    createdAt: timestamp,
  })

  return views
}

export function resolveWorkspaceViews(
  workspaceId: string,
  boards: ViewTargetItem[],
  pages: ViewTargetItem[],
  workspace?: WorkspaceSummary,
  timestamp?: string
): WorkspaceView[] {
  const defaultViews = createDefaultWorkspaceViews(workspaceId, boards, pages, timestamp)
  const settings = parseSettings(workspace?.settings)
  const customViews = settings.customViews ?? []
  const orderIds = settings.customViewsOrder ?? []
  const views = [...defaultViews, ...customViews]

  return sortWorkspaceViews(views, orderIds)
}

export function sortWorkspaceViews(views: WorkspaceView[], orderIds: string[]): WorkspaceView[] {
  return [...views].sort((a, b) => {
    const indexA = orderIds.indexOf(a.id)
    const indexB = orderIds.indexOf(b.id)
    if (indexA !== -1 && indexB !== -1) return indexA - indexB
    if (indexA !== -1) return -1
    if (indexB !== -1) return 1
    return (a.position ?? 0) - (b.position ?? 0)
  })
}
