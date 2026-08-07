import type { PageComment, CreateCommentPayload } from '@notrelix/docs-core';
import type { CommentDtoApi } from '../dto';
import { mapComment } from '../model/comment.mapper';
import type { DocsApiClient, PageApiEndpoints } from './page.api';

export function createCommentApi(
  api: DocsApiClient,
  endpoints: PageApiEndpoints,
) {
  return {
    async getList(pageId: string): Promise<PageComment[]> {
      const comments = await api.get<CommentDtoApi[]>(
        endpoints.pages.comments(pageId),
      );
      return comments.map((dto) => mapComment(dto, pageId));
    },

    async create(pageId: string, payload: CreateCommentPayload): Promise<PageComment> {
      const comment = await api.post<CommentDtoApi>(
        endpoints.pages.comments(pageId),
        payload,
      );
      return mapComment(comment, pageId);
    },

    async delete(commentId: string): Promise<void> {
      await api.delete<void>(endpoints.comments.detail(commentId));
    },
  };
}
