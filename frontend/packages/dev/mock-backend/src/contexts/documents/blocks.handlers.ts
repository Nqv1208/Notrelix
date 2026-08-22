import { defineMockOperation } from "../../operations/types";
import { ok, created, notFound } from "../../transport/create-response";
type BlockDtoApi = any;

export const blocksOperations = [
  // ─── GET /pages/:pageId/blocks ──────────────────────────────────────────

  defineMockOperation<{ pageId: string }, never, BlockDtoApi[]>({
    id: "docs.blocks.list",
    contract: {
      kind: "openapi",
      operationId: "Documents.Blocks.ListPageBlocks",
    } as any,
    method: "GET",
    route: "/pages/:pageId/blocks",
    async handle({ params, store }) {
      const page = store.getPage(params.pageId);
      if (!page) return notFound("Page not found");

      const blocks = store.getPageBlocks(params.pageId);
      return ok<BlockDtoApi[]>(
        blocks.map((b) => ({
          id: b.id,
          pageId: b.pageId,
          parentBlockId: b.parentBlockId ?? null,
          type: b.type as any,
          properties: b.properties,
          position: b.position,
          version: b.version,
          createdByUserId: b.createdByUserId,
          createdAt: b.createdAt,
          updatedAt: b.updatedAt ?? null,
        })),
      );
    },
  }),

  // ─── POST /pages/:pageId/blocks ─────────────────────────────────────────

  defineMockOperation<
    { pageId: string },
    {
      type?: string | null;
      properties?: string | null;
      parentBlockId?: string | null;
      position?: number;
    },
    BlockDtoApi
  >({
    id: "docs.blocks.create",
    contract: {
      kind: "openapi",
      operationId: "Documents.Blocks.CreateBlock",
    } as any,
    method: "POST",
    route: "/pages/:pageId/blocks",
    async handle({ params, body, store }) {
      const data = body ?? {};
      const block = store.createBlock(params.pageId, {
        type: data.type ?? "paragraph",
        properties: data.properties ?? "{}",
        parentBlockId: data.parentBlockId ?? null,
      });
      // also create history
      store.createPageHistory(
        params.pageId,
        "usr-m-00001",
        "created_block",
        block.type,
      );

      return created<BlockDtoApi>({
        id: block.id,
        pageId: block.pageId,
        parentBlockId: block.parentBlockId ?? null,
        type: block.type as any,
        properties: block.properties,
        position: block.position,
        version: block.version,
        createdByUserId: block.createdByUserId,
        createdAt: block.createdAt,
        updatedAt: block.updatedAt ?? null,
      });
    },
  }),

  // ─── PATCH /blocks/:blockId ─────────────────────────────────────────────

  defineMockOperation<
    { blockId: string },
    {
      type?: string | null;
      properties?: string | null;
      parentBlockId?: string | null;
      position?: number;
    },
    BlockDtoApi
  >({
    id: "docs.blocks.update",
    contract: {
      kind: "openapi",
      operationId: "Documents.Blocks.UpdateBlock",
    } as any,
    method: "PATCH",
    route: "/blocks/:blockId",
    async handle({ params, body, store }) {
      const data = body ?? {};
      const block = store.updateBlock(params.blockId, data as any);
      if (!block) return notFound("Block not found");

      return ok<BlockDtoApi>({
        id: block.id,
        pageId: block.pageId,
        parentBlockId: block.parentBlockId ?? null,
        type: block.type as any,
        properties: block.properties,
        position: block.position,
        version: block.version,
        createdByUserId: block.createdByUserId,
        createdAt: block.createdAt,
        updatedAt: block.updatedAt ?? null,
      });
    },
  }),

  // ─── DELETE /blocks/:blockId ────────────────────────────────────────────

  defineMockOperation<{ blockId: string }, never, void>({
    id: "docs.blocks.delete",
    contract: {
      kind: "openapi",
      operationId: "Documents.Blocks.DeleteBlock",
    } as any,
    method: "DELETE",
    route: "/blocks/:blockId",
    async handle({ params, store }) {
      const deleted = store.deleteBlock(params.blockId);
      if (!deleted) return notFound("Block not found");
      return ok<void>(undefined);
    },
  }),

  // ─── POST /pages/:pageId/blocks/reorder ─────────────────────────────────

  defineMockOperation<
    never,
    {
      pageId?: string;
      items?:
        | {
            blockId: string;
            parentBlockId?: string | null;
            position?: number;
          }[]
        | null;
    },
    void
  >({
    id: "docs.blocks.reorder",
    contract: {
      kind: "openapi",
      operationId: "Documents.Blocks.ReorderBlocks",
    } as any,
    method: "POST",
    route: "/blocks/reorder",
    async handle({ body, store }) {
      const items = body?.items ?? [];
      for (const item of items) {
        store.updateBlock(item.blockId, {
          parentBlockId: item.parentBlockId,
          position: item.position,
        });
      }
      return ok<void>(undefined);
    },
  }),

  // ─── POST /pages/:pageId/blocks/batch ───────────────────────────────────

  defineMockOperation<
    { pageId: string },
    {
      blocks?:
        | {
            id?: string;
            type?: string | null;
            properties?: string | null;
            parentBlockId?: string | null;
            position?: number;
          }[]
        | null;
    },
    BlockDtoApi[]
  >({
    id: "docs.blocks.batchUpdate",
    contract: {
      kind: "openapi",
      operationId: "Documents.Blocks.BatchUpdateBlocks",
    } as any,
    method: "POST",
    route: "/pages/:pageId/blocks/batch",
    async handle({ params: _params, body, store }) {
      const reqBlocks = body?.blocks ?? [];
      const res: BlockDtoApi[] = [];
      for (const b of reqBlocks) {
        if (!b.id) continue;
        const updated = store.updateBlock(b.id, b as any);
        if (updated) {
          res.push({
            id: updated.id,
            pageId: updated.pageId,
            parentBlockId: updated.parentBlockId ?? null,
            type: updated.type as any,
            properties: updated.properties,
            position: updated.position,
            version: updated.version,
            createdByUserId: updated.createdByUserId,
            createdAt: updated.createdAt,
            updatedAt: updated.updatedAt ?? null,
          });
        }
      }
      return ok<BlockDtoApi[]>(res);
    },
  }),
];
