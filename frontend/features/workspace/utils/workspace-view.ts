import type { WorkspaceView, WorkspaceViewType } from "../types"

export function getViewHref(workspaceSlug: string, view: Pick<WorkspaceView, "id" | "type">) {
  const viewParam = view.id === view.type ? view.type : view.id
  return `/${workspaceSlug}?view=${viewParam}`
}

export function isWorkspaceViewType(value: string): value is WorkspaceViewType {
  return ["table", "doc", "kanban", "calendar", "timeline", "dashboard", "form", "gallery", "chart", "gantt"].includes(value)
}
