import type { WorkspaceSummary, WorkspaceView, WorkspaceViewType } from "../types"

type WorkspaceRouteSource = Pick<WorkspaceSummary, "id"> | string
type WorkspaceViewRouteSource = Pick<WorkspaceView, "id" | "type" | "target">
type BoardWorkspaceViewType = Extract<WorkspaceViewType, "table" | "kanban" | "calendar" | "timeline">
type WorkspaceViewHrefOptions = {
  currentBoardId?: string
}

const BOARD_VIEW_TYPES = new Set<WorkspaceViewType>(["table", "kanban", "calendar", "timeline"])

export type WorkspaceTabbedRoute =
  | {
      kind: "board"
      workspaceId: string
      boardId: string
      activeViewType: BoardWorkspaceViewType
      viewParam?: string
      contentClassName: "overflow-hidden"
      showToolbar: true
    }
  | {
      kind: "dashboard"
      workspaceId: string
      activeViewType: "dashboard"
      contentClassName?: undefined
      showToolbar: true
    }
  | {
      kind: "docs"
      workspaceId: string
      activeViewType: "doc"
      pageId?: string
      contentClassName?: "overflow-hidden"
      showToolbar: boolean
    }

function getWorkspaceRouteId(workspace: WorkspaceRouteSource) {
  return typeof workspace === "string" ? workspace : workspace.id
}

export function isBoardWorkspaceView(type: WorkspaceViewType): type is BoardWorkspaceViewType {
  return BOARD_VIEW_TYPES.has(type)
}

export function normalizeBoardWorkspaceViewType(value?: string | null): BoardWorkspaceViewType | undefined {
  return isBoardWorkspaceView(value as WorkspaceViewType) ? (value as BoardWorkspaceViewType) : undefined
}

export function getWorkspaceRootHref(workspace: WorkspaceRouteSource) {
  return `/${getWorkspaceRouteId(workspace)}`
}

export function getWorkspaceViewHref(
  workspace: WorkspaceRouteSource,
  view: WorkspaceViewRouteSource,
  options: WorkspaceViewHrefOptions = {}
) {
  if (isBoardWorkspaceView(view.type)) {
    const boardId = options.currentBoardId ?? view.target.boardId
    if (boardId) return getWorkspaceBoardViewHref(workspace, boardId, view.type)
  }

  if (view.type === "doc") {
    return view.target.pageId ? getWorkspaceDocHref(workspace, view.target.pageId) : getWorkspaceDocsHref(workspace)
  }

  if (view.type === "dashboard") {
    return getWorkspaceDashboardHref(workspace)
  }

  const viewParam = view.id === view.type ? view.type : view.id
  return `${getWorkspaceRootHref(workspace)}?view=${viewParam}`
}

export function getWorkspaceDashboardHref(workspace: WorkspaceRouteSource) {
  return `${getWorkspaceRootHref(workspace)}/dashboard`
}

export function getWorkspaceDocsHref(workspace: WorkspaceRouteSource) {
  return `${getWorkspaceRootHref(workspace)}/docs`
}

export function getWorkspaceDocHref(workspace: WorkspaceRouteSource, pageId: string) {
  return `${getWorkspaceDocsHref(workspace)}/${pageId}`
}

export function getWorkspaceBoardsHref(workspace: WorkspaceRouteSource) {
  return `${getWorkspaceRootHref(workspace)}/boards`
}

export function getWorkspaceBoardHref(workspace: WorkspaceRouteSource, boardId: string) {
  return getWorkspaceBoardViewHref(workspace, boardId, "table")
}

export function getWorkspaceBoardBaseHref(workspace: WorkspaceRouteSource, boardId: string) {
  return `${getWorkspaceBoardsHref(workspace)}/${boardId}`
}

export function getWorkspaceBoardViewHref(
  workspace: WorkspaceRouteSource,
  boardId: string,
  view: BoardWorkspaceViewType = "table"
) {
  return `${getWorkspaceBoardBaseHref(workspace, boardId)}?view=${view}`
}

export function resolveWorkspaceTabbedRoute(
  pathname: string,
  workspaceId: string,
  searchParams: URLSearchParams | string | Record<string, string | string[] | undefined>
): WorkspaceTabbedRoute | null {
  const segments = pathname.split("?")[0]?.split("/").filter(Boolean) ?? []
  if (segments[0] !== workspaceId) return null

  const section = segments[1]

  if (section === "boards") {
    const boardId = segments[2]
    if (!boardId) return null

    const viewParam = getSearchParam(searchParams, "view")
    return {
      kind: "board",
      workspaceId,
      boardId,
      activeViewType: normalizeBoardWorkspaceViewType(viewParam) ?? "table",
      viewParam,
      contentClassName: "overflow-hidden",
      showToolbar: true,
    }
  }

  if (section === "dashboard") {
    return {
      kind: "dashboard",
      workspaceId,
      activeViewType: "dashboard",
      showToolbar: true,
    }
  }

  if (section === "docs") {
    const pageId = segments[2]
    const isEditorRoute = Boolean(pageId) && segments.length === 3
    return {
      kind: "docs",
      workspaceId,
      activeViewType: "doc",
      pageId,
      contentClassName: isEditorRoute ? "overflow-hidden" : undefined,
      showToolbar: isEditorRoute,
    }
  }

  return null
}

export function resolveWorkspaceTabbedActiveView(
  views: WorkspaceView[],
  route: WorkspaceTabbedRoute,
  timestamp = new Date().toISOString()
): WorkspaceView {
  if (route.kind === "board") {
    const directView = route.viewParam
      ? views.find((view): view is WorkspaceView & { type: BoardWorkspaceViewType } => (
          view.id === route.viewParam && isBoardWorkspaceView(view.type)
        ))
      : undefined
    const activeType = directView?.type ?? route.activeViewType
    const activeView = directView ?? views.find((view): view is WorkspaceView & { type: BoardWorkspaceViewType } => (
      view.type === activeType && isBoardWorkspaceView(view.type)
    ))

    return activeView
      ? withBoardTarget(activeView, route.boardId)
      : createSyntheticBoardView(route.workspaceId, route.boardId, activeType, timestamp)
  }

  if (route.kind === "dashboard") {
    return (
      views.find((view) => view.id === "dashboard" || view.type === "dashboard") ??
      createSyntheticDashboardView(route.workspaceId, timestamp)
    )
  }

  const matchingPageView = route.pageId
    ? views.find((view) => view.type === "doc" && view.target.pageId === route.pageId)
    : undefined
  const fallbackDocView = views.find((view) => view.id === "doc" || view.type === "doc")
  const activeView = matchingPageView ?? fallbackDocView

  if (!activeView) return createSyntheticDocView(route.workspaceId, route.pageId, timestamp)
  if (!route.pageId || activeView.target.pageId === route.pageId) return activeView

  return {
    ...activeView,
    target: {
      ...activeView.target,
      pageId: route.pageId,
    },
  }
}

export function getWorkspaceTabbedViews(views: WorkspaceView[], activeView: WorkspaceView) {
  return views.some((view) => view.id === activeView.id) ? views : [...views, activeView]
}

function getSearchParam(
  searchParams: URLSearchParams | string | Record<string, string | string[] | undefined>,
  key: string
) {
  if (typeof searchParams === "string") {
    return new URLSearchParams(searchParams).get(key) ?? undefined
  }

  if (searchParams instanceof URLSearchParams) {
    return searchParams.get(key) ?? undefined
  }

  const value = searchParams[key]
  return Array.isArray(value) ? value[0] : value
}

function withBoardTarget(view: WorkspaceView, boardId: string): WorkspaceView {
  if (view.target.boardId === boardId) return view

  return {
    ...view,
    target: {
      ...view.target,
      boardId,
    },
  }
}

function createSyntheticBoardView(
  workspaceId: string,
  boardId: string,
  type: BoardWorkspaceViewType,
  timestamp: string
): WorkspaceView {
  const labels: Record<BoardWorkspaceViewType, { name: string; icon: string; description: string; position: number }> = {
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
    createdAt: timestamp,
  }
}

function createSyntheticDashboardView(workspaceId: string, timestamp: string): WorkspaceView {
  return {
    id: "dashboard",
    workspaceId,
    name: "Dashboard",
    type: "dashboard",
    icon: "Dashboard",
    description: "Workspace pulse with team workload, activity, and delivery signals.",
    target: {},
    config: {},
    visibility: "workspace",
    isDefault: false,
    position: 99,
    createdAt: timestamp,
  }
}

function createSyntheticDocView(workspaceId: string, pageId: string | undefined, timestamp: string): WorkspaceView {
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
    createdAt: timestamp,
  }
}
