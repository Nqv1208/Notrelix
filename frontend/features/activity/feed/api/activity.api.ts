import { api } from "@/lib/api/api-client"
import { endpoints } from "@/lib/api/endpoints"
import type { WorkspaceActivityResponseApi, WorkspaceActivityItem } from "../types"

export function mapActivityResponse(response: WorkspaceActivityResponseApi): WorkspaceActivityItem[] {
  return response.data.map((item) => ({
    id: item.id,
    actor: "Workspace",
    action: item.action,
    target: item.resourceTitle ?? "item",
    createdAt: item.createdAt,
  }))
}

export const activityApi = {
  async getActivityLogs(workspaceId: string, page = 1, pageSize = 20): Promise<WorkspaceActivityItem[]> {
    const activity = await api.get<WorkspaceActivityResponseApi>(
      `${endpoints.workspaces.detail(workspaceId)}/activity?page=${page}&pageSize=${pageSize}`
    )
    return mapActivityResponse(activity)
  },
}
