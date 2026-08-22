import { defineMockOperation } from "../../operations/types";
import { ok, created, notFound } from "../../transport/create-response";
type CommentDtoApi = any;

export const commentsOperations = [
  // ─── GET /pages/:pageId/comments ────────────────────────────────────────

  defineMockOperation<{ pageId: string }, never, CommentDtoApi[]>({
    id: "docs.comments.list",
    contract: {
      kind: "openapi",
      operationId: "Collaboration.Comments.GetPageComments",
    } as any,
    method: "GET",
    route: "/pages/:pageId/comments",
    async handle({ params, store }) {
      const page = store.getPage(params.pageId);
      if (!page) return notFound("Page not found");

      const comments = store.getPageComments(params.pageId);
      return ok<CommentDtoApi[]>(
        comments.map((c) => ({
          id: c.id,
          userId: c.userId,
          contentMd: c.contentMd,
          createdAt: c.createdAt,
          isEdited: !!c.updatedAt,
        })),
      );
    },
  }),

  // ─── POST /pages/:pageId/comments ───────────────────────────────────────

  defineMockOperation<
    { pageId: string },
    { contentMd?: string | null; parentCommentId?: string | null },
    string
  >({
    id: "docs.comments.create",
    contract: {
      kind: "openapi",
      operationId: "Collaboration.Comments.CreatePageComment",
    } as any,
    method: "POST",
    route: "/pages/:pageId/comments",
    async handle({ params, body, store }) {
      const data = body ?? {};
      const comment = store.createPageComment(
        params.pageId,
        "usr-m-00001",
        data.contentMd ?? "",
      );
      return created<string>(comment.id);
    },
  }),

  // ─── POST /comments/:commentId/resolve ──────────────────────────────────

  defineMockOperation<{ commentId: string }, never, void>({
    id: "docs.comments.resolve",
    contract: {
      kind: "openapi",
      operationId: "Collaboration.Comments.Resolve",
    } as any,
    method: "POST",
    route: "/comments/:commentId/resolve",
    async handle({ params, store }) {
      const deleted = store.deletePageComment(params.commentId);
      if (!deleted) return notFound("Comment not found");
      return ok<void>(undefined);
    },
  }),
];
