import { mockDelay } from "./mock-delay"
import { mockWorkspaceSnapshot, workspaceViewTemplates } from "./mock-data"
import type { CreateWorkspaceViewInput, UpdateWorkspaceViewInput, WorkspaceSnapshot, WorkspaceView } from "../types"

let snapshot: WorkspaceSnapshot = structuredClone(mockWorkspaceSnapshot)

function cloneSnapshot(): WorkspaceSnapshot {
  return structuredClone(snapshot)
}

function iconFor(type: CreateWorkspaceViewInput["type"]) {
  return workspaceViewTemplates.find((template) => template.type === type)?.icon ?? "▦"
}

export const mockWorkspaceService = {
  // TODO(api):
  // Replace mockWorkspaceService with real API integration.
  // Endpoint: GET /api/workspaces/{slug}
  // Hook: useWorkspace
  async getWorkspace(slug: string) {
    await mockDelay()
    const current = cloneSnapshot()
    return current.workspace.slug === slug ? current.workspace : { ...current.workspace, slug }
  },

  // TODO(api):
  // Replace mockWorkspaceService with real API integration.
  // Endpoint: GET /api/workspaces/{slug}/snapshot
  // Hook: useWorkspaceSnapshot
  async getSnapshot(slug: string) {
    await mockDelay()
    const current = cloneSnapshot()
    return current.workspace.slug === slug ? current : { ...current, workspace: { ...current.workspace, slug } }
  },

  // TODO(api):
  // Replace mockWorkspaceService with real API integration.
  // Endpoint: GET /api/workspaces/{slug}/views
  // Hook: useWorkspaceViews
  async getViews(slug: string) {
    await mockDelay()
    return cloneSnapshot().views
      .filter((view) => view.workspaceSlug === snapshot.workspace.slug || view.workspaceSlug === slug)
      .map((view) => ({ ...view, workspaceSlug: slug }))
      .sort((a, b) => a.position - b.position)
  },

  // TODO(api):
  // Replace mockWorkspaceService with real API integration.
  // Endpoint: POST /api/workspaces/{slug}/views
  // Hook: useCreateWorkspaceView
  async createView(input: CreateWorkspaceViewInput): Promise<WorkspaceView> {
    await mockDelay()
    const template = workspaceViewTemplates.find((item) => item.type === input.type)
    const newView: WorkspaceView = {
      id: `${input.type}-${Date.now()}`,
      workspaceId: snapshot.workspace.id,
      workspaceSlug: input.workspaceSlug,
      name: input.name,
      type: input.type,
      icon: iconFor(input.type),
      description: template?.description ?? "Custom workspace view.",
      target: input.target ?? {},
      config: {},
      visibility: "workspace",
      isDefault: false,
      position: snapshot.views.length + 1,
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    }
    snapshot = { ...snapshot, views: [...snapshot.views, newView] }
    return { ...newView }
  },

  // TODO(api):
  // Replace mockWorkspaceService with real API integration.
  // Endpoint: PATCH /api/workspaces/{slug}/views/{viewId}
  // Hook: useUpdateWorkspaceView
  async updateView(slug: string, viewId: string, input: UpdateWorkspaceViewInput) {
    await mockDelay()
    let updated: WorkspaceView | undefined
    snapshot = {
      ...snapshot,
      views: snapshot.views.map((view) => {
        if (view.id !== viewId) return view
        updated = {
          ...view,
          workspaceSlug: slug,
          name: input.name ?? view.name,
          icon: input.icon ?? view.icon,
          config: { ...view.config, ...input.config },
          position: input.position ?? view.position,
          updatedAt: new Date().toISOString(),
        }
        return updated
      }),
    }
    if (!updated) throw new Error("Workspace view not found")
    return { ...updated }
  },
}
