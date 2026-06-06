import { api } from "@/lib/api/api-client"
import { endpoints } from "@/lib/api/endpoints"

export type UserNotification = {
  id: string
  workspaceId: string
  workspaceName: string
  userId: string
  actorId: string | null
  actorName: string
  type: string
  payload: string // JSON string containing metadata like invitationId, token, workspaceName etc.
  isRead: boolean
  createdAt: string
}

export const notificationsService = {
  async list(): Promise<UserNotification[]> {
    const res = await api.get<UserNotification[]>(endpoints.notifications.list)
    return res
  },

  async read(id: string): Promise<void> {
    await api.post<void>(endpoints.notifications.read(id), {})
  },

  async readAll(): Promise<void> {
    await api.post<void>(endpoints.notifications.readAll, {})
  },
}
