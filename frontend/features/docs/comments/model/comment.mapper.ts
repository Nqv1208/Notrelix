import type { PageComment } from "../types/comment.types"
import type { CommentDtoApi } from "../../shared/types/dto"

export function mapComment(dto: CommentDtoApi, pageId: string): PageComment {
  return {
    id: dto.id,
    pageId,
    blockId: null,
    authorId: dto.userId,
    body: dto.contentMd,
    mentionIds: [],
    resolved: false,
    createdAt: dto.createdAt,
    updatedAt: dto.createdAt,
  }
}
