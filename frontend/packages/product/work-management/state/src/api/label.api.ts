import { api } from "@notrelix/contracts"
import { endpoints } from "@notrelix/contracts"
import type { CardLabel } from "@notrelix/work-management-core"

export interface CreateLabelInput {
  boardId: string
  color: string
  name?: string
}

export interface UpdateLabelInput {
  boardId: string
  labelId: string
  color?: string
  name?: string
}

export const labelApi = {
  async getBoardLabels(boardId: string): Promise<CardLabel[]> {
    const labels = await api.get<{ id: string; name: string; color: string }[]>(endpoints.boards.labels(boardId))
    return labels.map((l) => ({ id: l.id, name: l.name, color: l.color }))
  },

  async createLabel(input: CreateLabelInput): Promise<CardLabel> {
    const res = await api.post<{ id: string; name: string; color: string }>(endpoints.boards.labels(input.boardId), {
      color: input.color,
      name: input.name,
    })
    return { id: res.id, name: res.name, color: res.color }
  },

  async updateLabel(input: UpdateLabelInput): Promise<void> {
    await api.patch<void>(endpoints.boards.label(input.boardId, input.labelId), {
      color: input.color,
      name: input.name,
    })
  },

  async deleteLabel(boardId: string, labelId: string): Promise<void> {
    await api.delete<void>(endpoints.boards.label(boardId, labelId))
  },

  async addLabelToCard(cardId: string, labelId: string): Promise<void> {
    await api.post<void>(endpoints.cards.labels(cardId), { labelId })
  },

  async removeLabelFromCard(cardId: string, labelId: string): Promise<void> {
    await api.delete<void>(endpoints.cards.label(cardId, labelId))
  },
}
