import { defineMockOperation, ok, notFound } from "../../operations/types";
import type { HistoryDtoApi } from "../../../../product/docs/state/src/dto";

export const historyOperations = [
  // ─── GET /pages/:pageId/history ─────────────────────────────────────────

  defineMockOperation<{ pageId: string }, never, HistoryDtoApi[]>({
    id: "docs.history.list",
    contract: { kind: "openapi", operationId: "Documents.Pages.GetPageHistory" } as any,
    method: "GET",
    route: "/pages/:pageId/history",
    async handle({ params, store }) {
      const page = store.getPage(params.pageId);
      if (!page) return notFound("Page not found");

      const history = store.getPageHistory(params.pageId);
      return ok<HistoryDtoApi[]>(
        history.map((h) => ({
          id: h.id,
          actorId: h.actorId,
          action: h.action,
          resourceTitle: h.resourceTitle,
          createdAt: h.createdAt,
        }))
      );
    },
  }),
];
