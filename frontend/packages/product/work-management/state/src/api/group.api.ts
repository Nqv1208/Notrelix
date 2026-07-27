import { createNotrelixClient, endpoints } from "@notrelix/contracts"
import type { BoardGroup } from "@notrelix/work-management-core"

const api = createNotrelixClient({ baseUrl: "/api/v1" }).api

export type CreateGroupInput = {
  boardId: string
  title: string
  position?: number
  color?: string
}

export type UpdateGroupInput = {
  groupId: string
  title?: string
  color?: string
}

export const groupApi = {
  async createGroup(input: CreateGroupInput): Promise<string> {
    return api.post<string>(endpoints.lists.byBoard(input.boardId), {
      title: input.title,
      position: input.position,
      color: input.color,
    })
  },

  async updateGroup(input: UpdateGroupInput): Promise<void> {
    await api.patch<void>(endpoints.lists.detail(input.groupId), {
      title: input.title,
      color: input.color,
    })
  },

  async deleteGroup(groupId: string): Promise<void> {
    await api.delete<void>(endpoints.lists.detail(groupId))
  },

  async duplicateGroup(groupId: string): Promise<string> {
    return api.post<string>(endpoints.lists.duplicate(groupId))
  },

  async reorderGroups(boardId: string, groups: Pick<BoardGroup, "id" | "position">[]): Promise<void> {
    await api.post<void>(endpoints.lists.reorder(boardId), {
      items: groups.map((group) => ({ id: group.id, newPosition: group.position })),
    })
  },
}
