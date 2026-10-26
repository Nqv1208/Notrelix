/**
 * Documents: reads and writes beyond the generated contract (gap
 * `CTR-GAP-DOCS-PAGE-EXT`, see docs/superpowers/be-blockers-2026-10.md,
 * "BE requirement from FE: Docs").
 *
 * Operations:
 *   docs.pages.backlinks     GET  /pages/:pageId/backlinks
 *   docs.blocks.convertTask  POST /pages/:pageId/blocks/:blockId/convert-to-task
 */
import { defineMockOperation } from "../../operations/types";
import {
  created,
  notFound,
  ok,
  validationError,
} from "../../transport/create-response";
import {
  INVOICE_BACKLINKS,
  INVOICE_DOC_ID,
  INVOICE_PERMISSIONS,
} from "../../state/mock-docs-invoice";
import {
  erpItemId,
  ERP_STATUS_COLUMN_ID,
  erpStatusOptionId,
} from "../../state/mock-erp-board";

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

  defineMockOperation<{ pageId: string }, never, unknown>({
    id: "docs.pages.backlinks",
    contract: GAP,
    method: "GET",
    route: "/pages/:pageId/backlinks",
    async handle({ params, store }) {
      const page = store.getPage(params.pageId);
      if (!page) return notFound("Page not found");
      if (page.id !== INVOICE_DOC_ID) return ok({ docs: [], tasks: [] });
      const docs = INVOICE_BACKLINKS.docIds.flatMap((id) => {
        const doc = store.getPage(id);
        if (!doc) return [];
        const space = doc.spaceId ? store.spaces.getSpace(doc.spaceId) : null;
        return [
          { id: doc.id, title: doc.title, spaceName: space?.name ?? null },
        ];
      });
      const tasks = INVOICE_BACKLINKS.itemKeys.flatMap((key) => {
        const card = store.getCard(erpItemId(key));
        return card
          ? [{ id: card.id, key: card.itemKey ?? key, title: card.title }]
          : [];
      });
      return ok({ docs, tasks });
    },
  }),

  defineMockOperation<{ pageId: string; blockId: string }, never, unknown>({
    id: "docs.blocks.convertTask",
    contract: GAP,
    method: "POST",
    route: "/pages/:pageId/blocks/:blockId/convert-to-task",
    async handle({ params, store }) {
      const blocks = store.getPageBlocks(params.pageId);
      const block = blocks.find((entry) => entry.id === params.blockId);
      if (!block) return notFound("Block not found");
      const properties = JSON.parse(block.properties || "{}") as Record<
        string,
        unknown
      >;
      const text = String(properties.text ?? "").trim();
      if (block.type !== "todo" || !text || properties.linkedTaskId)
        return validationError({ blockId: ["not convertible"] });
      // The item goes to the board the page's task table reads from.
      const reference = blocks
        .map(
          (entry) =>
            JSON.parse(entry.properties || "{}") as Record<string, unknown>,
        )
        .find((entry) => typeof entry.linkedBoardId === "string");
      const boardId = reference?.linkedBoardId as string | undefined;
      const list = boardId ? store.getLists(boardId)[0] : undefined;
      if (!boardId || !list) return validationError({ board: ["none"] });
      const card = store.createCard(boardId, list.id, { title: text });
      store.updateFieldValue(
        card.id,
        ERP_STATUS_COLUMN_ID,
        erpStatusOptionId("todo"),
      );
      store.updateBlock(block.id, {
        properties: JSON.stringify({ ...properties, linkedTaskId: card.id }),
      });
      return created({
        id: card.id,
        key: card.itemKey ?? card.id,
        title: card.title,
      });
    },
  }),
];
