import type { PageComment } from "@notrelix/docs-core";
import type { CommentDtoApi } from "../dto";

export function mapComment(dto: CommentDtoApi, pageId: string): PageComment {
  return {
    id: dto.id,
    pageId,
    blockId: null,
    authorId: dto.userId,
    body: dto.contentMd,
    mentionIds: [],
    anchor: dto.anchor ?? null,
    resolved: !!dto.resolvedAt,
    createdAt: dto.createdAt,
    updatedAt: dto.createdAt,
  };
}
