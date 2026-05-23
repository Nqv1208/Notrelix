import type { WorkspaceSummary, WorkspaceView } from "../types"

type WorkspaceRouteSource = Pick<WorkspaceSummary, "id"> | string

function getWorkspaceRouteId(workspace: WorkspaceRouteSource) {
  return typeof workspace === "string" ? workspace : workspace.id
}

export function getWorkspaceRootHref(workspace: WorkspaceRouteSource) {
  return `/${getWorkspaceRouteId(workspace)}`
}

export function getWorkspaceViewHref(workspace: WorkspaceRouteSource, view: Pick<WorkspaceView, "id" | "type">) {
  const viewParam = view.id === view.type ? view.type : view.id
  return `${getWorkspaceRootHref(workspace)}?view=${viewParam}`
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
  return `${getWorkspaceBoardsHref(workspace)}/${boardId}`
}
