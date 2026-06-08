import { api } from "@/lib/api/api-client"
import { endpoints } from "@/lib/api/endpoints"
import type { CreateWorkspaceViewInput, UpdateWorkspaceViewInput, WorkspaceView } from "../types"
import { parseSettings, stringifySettings } from "../utils/settings"
import { workspaceApi } from "./workspace.api"

export const viewsApi = {
  async reorderViews(workspaceId: string, orderedViewIds: string[]): Promise<void> {
    const workspace = await workspaceApi.getWorkspace(workspaceId)
    const settings = parseSettings(workspace.settings)
    settings.customViewsOrder = orderedViewIds

    await api.patch(endpoints.workspaces.detail(workspaceId), {
      settings: stringifySettings(settings),
    })
  },

  async createView(input: CreateWorkspaceViewInput): Promise<WorkspaceView> {
    const now = new Date().toISOString()
    const view: WorkspaceView = {
      id: `${input.type}-${Date.now()}`,
      workspaceId: input.workspaceId,
      name: input.name,
      type: input.type,
      icon: input.type,
      description: "",
      target: input.target ?? {},
      config: {},
      visibility: "workspace",
      isDefault: false,
      position: Date.now(),
      createdAt: now,
      updatedAt: now,
    }

    const workspace = await workspaceApi.getWorkspace(input.workspaceId)
    const settings = parseSettings(workspace.settings)
    settings.customViews = [...(settings.customViews ?? []), view]

    await api.patch(endpoints.workspaces.detail(input.workspaceId), {
      settings: stringifySettings(settings),
    })

    return view
  },

  async updateView(workspaceId: string, viewId: string, input: UpdateWorkspaceViewInput): Promise<WorkspaceView> {
    const workspace = await workspaceApi.getWorkspace(workspaceId)
    const settings = parseSettings(workspace.settings)
    const customViews = settings.customViews ?? []
    const current = customViews.find((view) => view.id === viewId)

    const updated: WorkspaceView = {
      ...(current ?? createFallbackView(workspaceId, viewId)),
      ...input,
      id: viewId,
      workspaceId,
      config: { ...(current?.config ?? {}), ...input.config },
      updatedAt: new Date().toISOString(),
    }

    settings.customViews = current
      ? customViews.map((view) => (view.id === viewId ? updated : view))
      : [...customViews, updated]

    await api.patch(endpoints.workspaces.detail(workspaceId), {
      settings: stringifySettings(settings),
    })

    return updated
  },
}

function createFallbackView(workspaceId: string, viewId: string): WorkspaceView {
  const now = new Date().toISOString()
  return {
    id: viewId,
    workspaceId,
    name: viewId,
    type: "table",
    icon: "table",
    description: "",
    target: {},
    config: {},
    visibility: "workspace",
    isDefault: false,
    position: Date.now(),
    createdAt: now,
  }
}
