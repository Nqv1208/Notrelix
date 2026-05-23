import { api } from "@/lib/api/api-client"
import { endpoints } from "@/lib/api/endpoints"
import type { CommentDtoApi } from "../types"
import type { CreateCardUpdateInput } from "../schemas"
import { mapCommentDtoToCardUpdate } from "../utils/board-api-mappers"

export const commentApi = {
  async getCardUpdates(cardId: string) {
    const comments = await api.get<CommentDtoApi[]>(endpoints.cards.comments(cardId))
    return comments.map((comment) => mapCommentDtoToCardUpdate(comment, cardId))
  },

  async createCardUpdate(input: CreateCardUpdateInput) {
    const id = await api.post<string>(endpoints.cards.comments(input.cardId), { contentMd: input.body })
    return {
      id,
      cardId: input.cardId,
      author: {
        id: "current-user",
        userId: "current-user",
        name: "You",
        initials: "Y",
        color: "var(--primary)",
      },
      body: input.body,
      mentionUserIds: input.mentionUserIds,
      attachmentIds: input.attachmentIds,
      createdAt: new Date().toISOString(),
    }
  },

  async updateCardUpdate(updateId: string, body: string): Promise<void> {
    await api.patch<void>(endpoints.comments.detail(updateId), { contentMd: body })
  },

  async deleteCardUpdate(updateId: string): Promise<void> {
    await api.delete<void>(endpoints.comments.detail(updateId))
  },
}
