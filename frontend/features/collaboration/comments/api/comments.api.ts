import { api } from "@/lib/api/api-client"
import { endpoints } from "@/lib/api/endpoints"

export type ResourceType = "item" | "page"

export type CommentDtoApi = {
  id: string
  userId: string
  contentMd: string
  createdAt: string
}

export type CollaborationComment = {
  id: string
  resourceId: string
  resourceType: ResourceType
  authorId: string
  body: string
  createdAt: string
  updatedAt: string
}

export const commentsApi = {
  async getComments(resourceId: string, resourceType: ResourceType): Promise<CollaborationComment[]> {
    const url = resourceType === "item" 
      ? endpoints.cards.comments(resourceId)
      : endpoints.pages.comments(resourceId)
    const comments = await api.get<CommentDtoApi[]>(url)
    return comments.map((dto) => ({
      id: dto.id,
      resourceId,
      resourceType,
      authorId: dto.userId,
      body: dto.contentMd,
      createdAt: dto.createdAt,
      updatedAt: dto.createdAt
    }))
  },

  async createComment(resourceId: string, resourceType: ResourceType, body: string): Promise<CollaborationComment> {
    const url = resourceType === "item" 
      ? endpoints.cards.comments(resourceId)
      : endpoints.pages.comments(resourceId)
    const id = await api.post<string>(url, { contentMd: body })
    return {
      id,
      resourceId,
      resourceType,
      authorId: "current-user",
      body,
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString()
    }
  },

  async updateComment(commentId: string, body: string): Promise<void> {
    await api.patch<void>(endpoints.comments.detail(commentId), { contentMd: body })
  },

  async deleteComment(commentId: string): Promise<void> {
    await api.delete<void>(endpoints.comments.detail(commentId))
  }
}
