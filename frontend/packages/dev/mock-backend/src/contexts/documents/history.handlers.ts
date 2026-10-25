import { defineMockOperation } from "../../operations/types";
import { ok, notFound } from "../../transport/create-response";
import { INVOICE_DOC_ID, invoiceSnapshot } from "../../state/mock-docs-invoice";
import { toBlockDto } from "./blocks.handlers";
type HistoryDtoApi = any;

export const historyOperations = [
  // ─── GET /pages/:pageId/history ─────────────────────────────────────────

  defineMockOperation<{ pageId: string }, never, HistoryDtoApi[]>({
    id: "docs.history.list",
    contract: {
      kind: "openapi",
      operationId: "Documents.Pages.GetPageHistory",
    } as any,
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
          ...(h.versionName ? { versionName: h.versionName } : {}),
          createdAt: h.createdAt,
        })),
      );
    },
  }),

  // ─── GET /pages/:pageId/history/:entryId/snapshot ───────────────────────
  // Gap `CTR-GAP-DOCS-PAGE-EXT`: the blocks as they stood after the entry.

  defineMockOperation<{ pageId: string; entryId: string }, never, unknown>({
    id: "docs.history.snapshot",
    contract: { kind: "gap", gapId: "CTR-GAP-DOCS-PAGE-EXT" },
    method: "GET",
    route: "/pages/:pageId/history/:entryId/snapshot",
    async handle({ params, store }) {
      const page = store.getPage(params.pageId);
      if (!page) return notFound("Page not found");
      const entries = [...store.getPageHistory(params.pageId)].sort((a, b) =>
        b.createdAt.localeCompare(a.createdAt),
      );
      const index = entries.findIndex((entry) => entry.id === params.entryId);
      if (index < 0) return notFound("Version not found");
      const blocks = store.getPageBlocks(params.pageId);
      const snapshot =
        params.pageId === INVOICE_DOC_ID ? invoiceSnapshot(index) : null;
      return ok(
        blocks.flatMap((b, position) => {
          if (snapshot?.without.has(position)) return [];
          const override = snapshot?.text.get(position);
          const properties =
            override === undefined
              ? b.properties
              : JSON.stringify({
                  ...(JSON.parse(b.properties || "{}") as object),
                  text: override,
                });
          return [toBlockDto({ ...b, properties })];
        }),
      );
    },
  }),
];
