import type { WorkspaceView, WorkspaceViewType } from "../types"
import { getWorkspaceViewHref } from "./workspace-routes"

export function getViewHref(workspaceId: string, view: Pick<WorkspaceView, "id" | "type">) {
  return getWorkspaceViewHref(workspaceId, view)
}

export function isWorkspaceViewType(value: string): value is WorkspaceViewType {
  return ["table", "doc", "kanban", "calendar", "timeline", "dashboard", "form", "gallery", "chart", "gantt"].includes(value)
}
