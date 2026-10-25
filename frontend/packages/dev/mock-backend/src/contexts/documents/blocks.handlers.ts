import { defineMockOperation } from "../../operations/types";
import { ok, created, notFound } from "../../transport/create-response";
type BlockDtoApi = any;

/**
 * Like the producer (`BlockDto`): the block's type by its producer name, its JSON
 * body as `content`, and the position as an opaque key (here the zero-padded
 * number the store orders by). The store keeps the page's own type names.
 */
const PRODUCER_TYPE: Record<string, string> = {
  paragraph: "Text",
  heading_1: "Heading1",
  heading_2: "Heading2",
  heading_3: "Heading3",
  bulleted_list: "BulletList",
  numbered_list: "NumberedList",
  todo: "ToDo",
  toggle: "Toggle",
  quote: "Quote",
  divider: "Divider",
  code: "Code",
  image: "Image",
  embed: "File",
  board_reference: "Board",
  table: "Table",
  callout: "Callout",
  page_reference: "PageReference",
};
const STORE_TYPE: Record<string, string> = Object.fromEntries(
  Object.entries(PRODUCER_TYPE).map(([key, value]) => [value, key]),
);

export function toBlockDto(b: any): BlockDtoApi {
  return {
    id: b.id,
    pageId: b.pageId,
    parentId: b.parentBlockId ?? null,
    type: PRODUCER_TYPE[b.type] ?? "Text",
    content:
      typeof b.properties === "string"
        ? b.properties
        : JSON.stringify(b.properties ?? {}),
    properties: "{}",
    position: String(Math.round(b.position ?? 0)).padStart(12, "0"),
    version: b.version,
    createdAt: b.createdAt,
    updatedAt: b.updatedAt ?? null,
  };
}

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
      return ok<BlockDtoApi[]>(
        store.getPageBlocks(params.pageId).map(toBlockDto),
      );
    },
  }),

  // ─── POST /pages/:pageId/blocks ─────────────────────────────────────────

  defineMockOperation<
    { pageId: string },
    {
      type?: string | null;
      properties?: string | null;
      parentId?: string | null;
      afterBlockId?: string | null;
    },
    string
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
      const siblings = store
        .getPageBlocks(params.pageId)
        .filter((b) => (b.parentBlockId ?? null) === (data.parentId ?? null));
      const after = data.afterBlockId
        ? siblings.findIndex((b) => b.id === data.afterBlockId)
        : siblings.length - 1;
      if (data.afterBlockId && after < 0) return notFound("Block not found");
      const here = siblings[after]?.position ?? 0;
      const next = siblings[after + 1]?.position;
      const block = store.createBlock(params.pageId, {
        type: STORE_TYPE[data.type ?? ""] ?? "paragraph",
        properties: data.properties ?? "{}",
        parentBlockId: data.parentId ?? null,
        position: next === undefined ? here + 1000 : (here + next) / 2,
      });
      store.createPageHistory(
        params.pageId,
        "usr-m-00001",
        "created_block",
        block.type,
      );
      // Like the producer (`ToCreatedResult`): 201 with the new block's id.
      return created<string>(block.id);
    },
  }),

  // ─── PATCH /pages/:pageId/blocks/:blockId ───────────────────────────────

  defineMockOperation<
    { pageId: string; blockId: string },
    { type?: string | null; properties?: string | null },
    void
  >({
    id: "docs.blocks.update",
    contract: {
      kind: "openapi",
      operationId: "Documents.Blocks.UpdateBlock",
    } as any,
    method: "PATCH",
    route: "/pages/:pageId/blocks/:blockId",
    async handle({ params, body, store }) {
      const existing = store
        .getPageBlocks(params.pageId)
        .find((b) => b.id === params.blockId);
      if (!existing) return notFound("Block not found");
      const type = body?.type;
      if (type && STORE_TYPE[type] !== existing.type) {
        return {
          status: 400,
          body: {
            title: "Validation failed",
            errorCode: "validation.failed",
            errors: { _errors: ["A block keeps its type"] },
          },
        };
      }
      if (body?.properties != null) {
        store.updateBlock(params.blockId, { properties: body.properties });
      }
      return ok<void>(undefined);
    },
  }),

  // ─── DELETE /pages/:pageId/blocks/:blockId ──────────────────────────────

  defineMockOperation<{ pageId: string; blockId: string }, never, void>({
    id: "docs.blocks.delete",
    contract: {
      kind: "openapi",
      operationId: "Documents.Blocks.DeleteBlock",
    } as any,
    method: "DELETE",
    route: "/pages/:pageId/blocks/:blockId",
    async handle({ params, store }) {
      const existing = store
        .getPageBlocks(params.pageId)
        .find((b) => b.id === params.blockId);
      if (!existing) return notFound("Block not found");
      store.deleteBlock(params.blockId);
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
