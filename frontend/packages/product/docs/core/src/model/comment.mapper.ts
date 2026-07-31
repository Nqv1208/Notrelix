import type { PageComment } from '../types/comment';
import type { CommentDtoApi } from '../dto';

export function mapComment(
  dto: CommentDtoApi,
  pageId: string,
): PageComment {
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
  };
}
