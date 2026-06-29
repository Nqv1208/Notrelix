import { api, type ApiRequestOptions } from "@/lib/api/api-client"
import { endpoints } from "@/lib/api/endpoints"
import type {
  CreateWorkspaceDto,
  UpdateWorkspaceDto,
  WorkspaceDtoApi,
} from "../types/dto"
import type { WorkspaceSummary } from "../types"

const workspacePlans = ["free", "pro", "business", "enterprise"] as const

export function normalizePlan(plan: string): WorkspaceSummary["plan"] {
  const normalized = plan.trim().toLowerCase()
  return workspacePlans.includes(normalized as WorkspaceSummary["plan"])
    ? (normalized as WorkspaceSummary["plan"])
    : "free"
}

export function mapWorkspaceDto(workspace: WorkspaceDtoApi): WorkspaceSummary {
  return {
    id: workspace.id,
    slug: workspace.slug,
    name: workspace.name,
    description: workspace.description ?? undefined,
    icon: workspace.iconValue?.trim() || workspace.name.trim().charAt(0).toUpperCase() || "W",
    plan: normalizePlan(workspace.plan),
    memberCount: workspace.memberCount,
    isPersonal: workspace.isPersonal,
    settings: workspace.settings ?? undefined,
  }
}

export const workspaceApi = {
  async listWorkspaces(options?: ApiRequestOptions): Promise<WorkspaceSummary[]> {
    const workspaces = await api.get<WorkspaceDtoApi[]>(endpoints.workspaces.list, options)
    return workspaces.map(mapWorkspaceDto)
  },

  async getWorkspace(workspaceId: string, options?: ApiRequestOptions): Promise<WorkspaceSummary> {
    const workspace = await api.get<WorkspaceDtoApi>(endpoints.workspaces.detail(workspaceId), options)
    return mapWorkspaceDto(workspace)
  },

  async createWorkspace(input: CreateWorkspaceDto): Promise<WorkspaceSummary> {
    const workspace = await api.post<WorkspaceDtoApi>(endpoints.workspaces.list, input)
    return mapWorkspaceDto(workspace)
  },

  async updateWorkspace(workspaceId: string, input: UpdateWorkspaceDto): Promise<WorkspaceSummary> {
    const workspace = await api.patch<WorkspaceDtoApi>(endpoints.workspaces.detail(workspaceId), input)
    return mapWorkspaceDto(workspace)
  },
}
