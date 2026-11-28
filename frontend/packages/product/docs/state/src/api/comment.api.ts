import type { PageComment, CreateCommentPayload } from "@notrelix/docs-core";
import type { CommentDtoApi } from "../dto";
import { mapComment } from "../model/comment.mapper";
import type { DocsApiClient, PageApiEndpoints } from "./page.api";

export function createCommentApi(
  api: DocsApiClient,
  endpoints: PageApiEndpoints,
) {
  return {
    /**
     * Every comment of the page, read one page at a time (oldest first) until
     * `nextCursor` runs out. A deleted comment that only keeps its replies is
     * left out: this view does not thread replies.
     */
    async getList(pageId: string): Promise<PageComment[]> {
      const comments: PageComment[] = [];
      let cursor: string | null = null;
      do {
        const query: string = cursor
          ? `?cursor=${encodeURIComponent(cursor)}`
          : "";
        const page = await api.get<{
          items?: CommentDtoApi[] | null;
          nextCursor?: string | null;
        }>(`${endpoints.pages.commentsPage(pageId)}${query}`);
        for (const dto of page.items ?? []) {
          if (!dto.isDeleted) comments.push(mapComment(dto, pageId));
        }
        cursor = page.nextCursor ?? null;
      } while (cursor);
      return comments;
    },

    async create(
      pageId: string,
      payload: CreateCommentPayload,
      options: { readonly idempotencyKey?: string } = {},
    ): Promise<PageComment> {
      // `Collaboration.Comments.CreatePageComment` takes `contentMd`.
      const body = {
        contentMd: payload.body,
        ...(payload.anchor ? { anchor: payload.anchor } : {}),
        ...(payload.parentId ? { parentCommentId: payload.parentId } : {}),
        // The producer mentions only people who can view the page.
        ...(payload.mentionIds?.length
          ? { mentionedUserIds: payload.mentionIds }
          : {}),
      };
      const url = endpoints.pages.comments(pageId);
      const created = options.idempotencyKey
        ? await api.post<CommentDtoApi | string>(url, body, {
            idempotencyKey: options.idempotencyKey,
          })
        : await api.post<CommentDtoApi | string>(url, body);
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
    async resolve(commentId: string, expectedVersion: number): Promise<void> {
      // The version the reader saw; a thread changed since is refused (412, UC-COL-H02).
      await api.post<void>(
        `${endpoints.comments.resolve(commentId)}?expectedVersion=${expectedVersion}`,
        {},
      );
    },

    /** `Collaboration.Comments.Reopen`: makes a resolved thread active again. */
    async reopen(commentId: string, expectedVersion: number): Promise<void> {
      await api.post<void>(
        `${endpoints.comments.reopen(commentId)}?expectedVersion=${expectedVersion}`,
        {},
      );
    },

    /** `Collaboration.Comments.Delete`: the version the reader saw goes in the query. */
    async delete(commentId: string, expectedVersion: number): Promise<void> {
      await api.delete<void>(
        `${endpoints.comments.detail(commentId)}?expectedVersion=${expectedVersion}`,
      );
    },
  };
}
