/**
 * Documents: reads and writes beyond the generated contract (gap
 * `CTR-GAP-DOCS-PAGE-EXT`, see docs/superpowers/be-blockers-2026-10.md,
 * "BE requirement from FE: Docs").
 *
 * Operations:
 *   docs.pages.permissions   GET  /pages/:pageId/permissions
 *
 * Backlinks (`Documents.Pages.GetPageBacklinks`) are served by the pages
 * handlers' neighbour below: they follow the producer's contract.
 */
import { defineMockOperation } from "../../operations/types";
import { notFound, ok } from "../../transport/create-response";
import {
  INVOICE_BACKLINKS,
  INVOICE_DOC_ID,
  INVOICE_PERMISSIONS,
} from "../../state/mock-docs-invoice";

const GAP = { kind: "gap", gapId: "CTR-GAP-DOCS-PAGE-EXT" } as const;

export const pageExtrasOperations = [
  defineMockOperation<{ pageId: string }, never, unknown>({
    id: "docs.pages.permissions",
    contract: GAP,
    method: "GET",
    route: "/pages/:pageId/permissions",
    async handle({ params, store }) {
      const page = store.getPage(params.pageId);
      if (!page) return notFound("Page not found");
      if (page.id !== INVOICE_DOC_ID) return ok({ members: [] });
      return ok({
        members: INVOICE_PERMISSIONS.map((entry) => ({
          id: entry.id,
          kind: entry.kind,
          userId: entry.userId ?? null,
          name: entry.userId
            ? (store.getUser(entry.userId)?.name ?? entry.name ?? "")
            : (entry.name ?? ""),
          detail: entry.detail,
          outside: entry.outside ?? false,
          creator: entry.creator ?? false,
          level: entry.level,
        })),
      });
    },
  }),

  // `Documents.Pages.GetPageBacklinks`: the pages that name this page, found
  // the way the producer derives them (a page reference block or a page mention).
  defineMockOperation<{ pageId: string }, never, unknown>({
    id: "docs.pages.backlinks",
    contract: {
      kind: "openapi",
      operationId: "Documents.Pages.GetPageBacklinks",
    },
    method: "GET",
    route: "/pages/:pageId/backlinks",
    async handle({ params, store }) {
      const page = store.getPage(params.pageId);
      if (!page || page.trashedAt) return notFound("Page not found");
      const ids = new Set<string>(
        page.id === INVOICE_DOC_ID ? INVOICE_BACKLINKS.docIds : [],
      );
      for (const other of store.getPages(page.workspaceId)) {
        if (other.id === page.id) continue;
        const names = store.getPageBlocks(other.id).some((block) => {
          try {
            const content = JSON.parse(block.properties || "{}") as {
              linkedPageId?: string;
              pageMentionIds?: string[];
            };
            return (
              content.linkedPageId === page.id ||
              (content.pageMentionIds ?? []).includes(page.id)
            );
          } catch {
            return false;
          }
        });
        if (names) ids.add(other.id);
      }
      const docs = [...ids].flatMap((id) => {
        const doc = store.getPage(id);
        if (!doc || doc.trashedAt) return [];
        const spaceId =
          doc.spaceId &&
          store.spaces.accessFor(
            doc.spaceId,
            store.spaceViewer(doc.workspaceId),
          ) !== null
            ? doc.spaceId
            : null;
        return [
          { id: doc.id, title: doc.title, icon: doc.icon ?? "", spaceId },
        ];
      });
      return ok({ docs });
    },
  }),
];
