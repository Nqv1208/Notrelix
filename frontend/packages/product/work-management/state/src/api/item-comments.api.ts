import { createNotrelixClient, endpoints } from "@notrelix/contracts"
import type { CommentDtoApi } from "@notrelix/work-management-core"
import type { CreateCardUpdateInput } from "@notrelix/work-management-core"
import { mapCommentDtoToCardUpdate } from "@notrelix/work-management-core"

const api = createNotrelixClient({ baseUrl: "/api/v1" }).api

export const commentApi = {
  async getCardUpdates(cardId: string) {
    const comments = await api.get<CommentDtoApi[]>(endpoints.cards.comments(cardId))
    return comments.map((comment: any) => mapCommentDtoToCardUpdate(comment, cardId))
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
