import { api } from "@/lib/api/api-client"
import { endpoints } from "@/lib/api/endpoints"
import type { CreateCommentPayload, PageComment } from "../types/comment.types"
import type { CommentDtoApi } from "../../shared/types/dto"
import { mapComment } from "../model/comment.mapper"

export const pageCommentsApi = {
  async getComments(pageId: string): Promise<PageComment[]> {
    const comments = await api.get<CommentDtoApi[]>(endpoints.pages.comments(pageId))
    return comments.map((comment) => mapComment(comment, pageId))
  },

  async createComment(payload: CreateCommentPayload): Promise<PageComment> {
    const id = await api.post<string>(endpoints.pages.comments(payload.pageId), { contentMd: payload.body })
    return {
      id,
      pageId: payload.pageId,
      blockId: payload.blockId ?? null,
      authorId: "current-user",
      body: payload.body,
      mentionIds: payload.mentionIds ?? [],
      resolved: false,
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    }
  },
}
