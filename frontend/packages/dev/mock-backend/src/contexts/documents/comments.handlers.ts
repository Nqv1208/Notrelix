import { defineMockOperation } from "../../operations/types";
import {
  ok,
  created,
  notFound,
  type MockHttpResult,
} from "../../transport/create-response";
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
          anchor: c.anchor ?? null,
        })),
      );
    },
  }),

  // ─── POST /pages/:pageId/comments ───────────────────────────────────────

  defineMockOperation<
    { pageId: string },
    {
      contentMd?: string | null;
      parentCommentId?: string | null;
      anchor?: {
        blockId: string;
        quote: string;
        start: number;
        end: number;
      } | null;
    },
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
      const page = store.getPage(params.pageId);
      if (!page) return notFound("Page not found");
      const anchor = data.anchor ?? undefined;
      // Mirrors Collaboration: a top-level comment may quote a range of one
      // live block of this page; the quote is exactly as long as its range.
      if (anchor) {
        const invalid =
          data.parentCommentId ||
          !anchor.quote?.trim() ||
          anchor.quote.length > 500 ||
          anchor.start < 0 ||
          anchor.end <= anchor.start ||
          anchor.end > 20000 ||
          anchor.end - anchor.start !== anchor.quote.length ||
          !store
            .getPageBlocks(params.pageId)
            .some((block) => block.id === anchor.blockId);
        if (invalid) {
          return {
            status: 400,
            body: {
              title: "The comment anchor is not valid.",
              status: 400,
              errorCode: "Collaboration_Comment_AnchorInvalid",
            },
          } as unknown as MockHttpResult<string>;
        }
      }
      const comment = store.createPageComment(
        params.pageId,
        "usr-m-00001",
        data.contentMd ?? "",
        undefined,
        anchor,
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
