import type { PageComment, CreateCommentPayload } from "@notrelix/docs-core";
import type { CommentDtoApi } from "../dto";
import { mapComment } from "../model/comment.mapper";
import type { DocsApiClient, PageApiEndpoints } from "./page.api";

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

    async create(
      pageId: string,
      payload: CreateCommentPayload,
    ): Promise<PageComment> {
      // `Collaboration.Comments.CreatePageComment` takes `contentMd`.
      const created = await api.post<CommentDtoApi | string>(
        endpoints.pages.comments(pageId),
        {
          contentMd: payload.body,
          ...(payload.anchor ? { anchor: payload.anchor } : {}),
        },
      );
      const now = new Date().toISOString();
      return typeof created === "string"
        ? {
            id: created,
            pageId,
            blockId: payload.blockId ?? null,
            authorId: "",
            body: payload.body,
            mentionIds: payload.mentionIds ?? [],
            anchor: payload.anchor ?? null,
            resolved: false,
            createdAt: now,
            updatedAt: now,
          }
        : mapComment(created, pageId);
    },

    /** `Collaboration.Comments.Resolve`. */
    async resolve(commentId: string): Promise<void> {
      await api.post<void>(endpoints.comments.resolve(commentId), {});
    },

    /** `Collaboration.Comments.Reopen`: makes a resolved thread active again. */
    async reopen(commentId: string): Promise<void> {
      await api.post<void>(endpoints.comments.reopen(commentId), {});
    },

    async delete(commentId: string): Promise<void> {
      await api.delete<void>(endpoints.comments.detail(commentId));
    },
  };
}
