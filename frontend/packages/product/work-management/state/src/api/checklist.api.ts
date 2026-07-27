import { createNotrelixClient, endpoints } from "@notrelix/contracts"
import type { ChecklistDtoApi } from "@notrelix/work-management-core"

const api = createNotrelixClient({ baseUrl: "/api/v1" }).api

export interface CreateChecklistInput {
  cardId: string
  title: string
}

export interface UpdateChecklistInput {
  checklistId: string
  title?: string
  position?: number
}

export interface CreateChecklistItemInput {
  checklistId: string
  title: string
}

export interface UpdateChecklistItemInput {
  itemId: string
  title?: string
  isChecked?: boolean
  dueDate?: string | null
  assigneeId?: string | null
}

export const checklistApi = {
  async getChecklists(cardId: string): Promise<ChecklistDtoApi[]> {
    return api.get<ChecklistDtoApi[]>(endpoints.cards.checklists(cardId))
  },

  async createChecklist(input: CreateChecklistInput): Promise<string> {
    return api.post<string>(endpoints.cards.checklists(input.cardId), {
      title: input.title,
    })
  },

  async updateChecklist(input: UpdateChecklistInput): Promise<void> {
    await api.patch<void>(endpoints.checklists.detail(input.checklistId), {
      title: input.title,
      position: input.position,
    })
  },

  async deleteChecklist(checklistId: string): Promise<void> {
    await api.delete<void>(endpoints.checklists.detail(checklistId))
  },

  async createChecklistItem(input: CreateChecklistItemInput): Promise<string> {
    return api.post<string>(endpoints.checklists.items(input.checklistId), {
      title: input.title,
    })
  },

  async updateChecklistItem(input: UpdateChecklistItemInput): Promise<void> {
    await api.patch<void>(endpoints.checklistItems.detail(input.itemId), {
      title: input.title,
      isChecked: input.isChecked,
      dueDate: input.dueDate,
      assigneeId: input.assigneeId,
    })
  },

  async deleteChecklistItem(itemId: string): Promise<void> {
    await api.delete<void>(endpoints.checklistItems.detail(itemId))
  },
}
