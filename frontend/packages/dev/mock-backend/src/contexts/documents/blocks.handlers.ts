import { defineMockOperation } from "../../operations/types";
import type { MockBlockRecord } from "../../state/records";
import type { MockStore } from "../../state/mock-store";
import {
  ok,
  created,
  notFound,
  type MockHttpResult,
} from "../../transport/create-response";
import {
  BlockValidationError,
  readBlockBody,
  retypeBody,
  validateBody,
  writeBlockBody,
  TEXT_LIKE,
  type JsonObject,
} from "./block-body";
type BlockDtoApi = any;

/**
 * Like the producer (`BlockDto`): the block's type by its producer name, its
 * rich text in `content` and the type's values in `properties` (both JSON
 * objects), its `version`, and the position as an opaque key (here the
 * zero-padded number the store orders by). The store keeps the page's own type
 * names; `block-body.ts` reads what a block holds.
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
  file: "File",
  board_reference: "Board",
  table: "Table",
  callout: "Callout",
  page_reference: "PageReference",
  table_of_contents: "TableOfContents",
};
const STORE_TYPE: Record<string, string> = {
  ...Object.fromEntries(
    Object.entries(PRODUCER_TYPE).map(([key, value]) => [value, key]),
  ),
  // The producer also reads the editor's own names.
  ...Object.fromEntries(Object.keys(PRODUCER_TYPE).map((key) => [key, key])),
};

export function toBlockDto(b: any): BlockDtoApi {
  const body = readBlockBody({
    type: b.type,
    properties:
      typeof b.properties === "string"
        ? b.properties
        : JSON.stringify(b.properties ?? {}),
  });
  return {
    id: b.id,
    pageId: b.pageId,
    parentId: b.parentBlockId ?? null,
    type: PRODUCER_TYPE[b.type] ?? "Text",
    content: body.content,
    properties: body.properties,
    position: String(Math.round(b.position ?? 0)).padStart(12, "0"),
    version: b.version,
    createdAt: b.createdAt,
    updatedAt: b.updatedAt ?? null,
  };
}

function problem(
  status: number,
  errorCode: string,
  title: string,
  extensions?: Record<string, unknown>,
): MockHttpResult<unknown> {
  return {
    status,
    body: {
      title,
      status,
      errorCode,
      ...(extensions ? { extensions } : {}),
    },
  };
}

function invalid(message: string): MockHttpResult<unknown> {
  return problem(400, "validation.failed", message);
}

function versionConflict(current: MockBlockRecord): MockHttpResult<unknown> {
  return problem(
    412,
    "documents.block.version-conflict",
    "The block was changed by someone else.",
    { currentBlock: toBlockDto(current) },
  );
}

/** What a create or edit of one block carries (the producer's block body). */
interface BlockWriteBody {
  id?: string;
  type?: string | null;
  content?: JsonObject | null;
  properties?: JsonObject | null;
  position?: string | null;
  afterBlockId?: string | null;
  parentId?: string | null;
  expectedVersion?: number;
}

class WriteRefused extends Error {
  constructor(readonly result: MockHttpResult<unknown>) {
    super("refused");
  }
}

/** The stored text of a block body; a code block keeps no marks. */
function storedText(
  type: string,
  content: JsonObject,
  properties: JsonObject,
): string {
  let nextContent = content;
  let nextProperties = properties;
  if (type === "code") {
    const runs = Array.isArray(content.runs) ? content.runs : [];
    nextContent = {
      v: 1,
      runs: runs.map((run: { text?: string }) => ({
        text: run.text ?? "",
        marks: [],
      })),
    };
    if (typeof properties.language === "string") {
      nextProperties = {
        ...properties,
        language: properties.language.toLowerCase(),
      };
    }
  }
  return writeBlockBody({ content: nextContent, properties: nextProperties });
}

/** Where a block goes: the producer's `position` / `afterBlockId` / `parentId`. */
function placeBlock(
  store: MockStore,
  pageId: string,
  data: BlockWriteBody,
  current: MockBlockRecord | undefined,
): { parentBlockId: string | null; position: number } | null {
  const sent =
    data.position != null || data.afterBlockId != null || "parentId" in data;
  if (!sent && current) return null;
  const parent =
    data.parentId !== undefined
      ? (data.parentId ?? null)
      : (current?.parentBlockId ?? null);
  const siblings = store
    .getPageBlocks(pageId)
    .filter(
      (b) => (b.parentBlockId ?? null) === parent && b.id !== current?.id,
    );
  if (data.position != null) {
    const key = Number(data.position);
    if (!Number.isFinite(key)) {
      throw new WriteRefused(invalid("position is not a valid key."));
    }
    return { parentBlockId: parent, position: key };
  }
  if (data.afterBlockId != null) {
    const after = siblings.findIndex((b) => b.id === data.afterBlockId);
    if (after < 0) {
      throw new WriteRefused(
        invalid("afterBlockId must be a sibling under that parent."),
      );
    }
    const here = siblings[after]!.position;
    const next = siblings[after + 1]?.position;
    return {
      parentBlockId: parent,
      position: next === undefined ? here + 1000 : (here + next) / 2,
    };
  }
  const last = siblings[siblings.length - 1]?.position;
  return {
    parentBlockId: parent,
    position: last === undefined ? 0 : last + 1000,
  };
}

function resolveType(name: string | null | undefined): string | undefined {
  return name ? STORE_TYPE[name] : undefined;
}

/** Creates a block (client id allowed); refuses a taken id or a bad body. */
function applyCreate(
  store: MockStore,
  pageId: string,
  data: BlockWriteBody,
): MockBlockRecord {
  const type = resolveType(data.type);
  if (!type) {
    throw new WriteRefused(
      invalid(
        data.type ? `Unknown block type '${data.type}'.` : "type is required.",
      ),
    );
  }
  if (data.id && store.getPageBlocks(pageId).some((b) => b.id === data.id)) {
    throw new WriteRefused(
      problem(409, "documents.block.id-taken", "The block id is taken."),
    );
  }
  try {
    validateBody(type, data.content, data.properties);
  } catch (error) {
    if (error instanceof BlockValidationError) {
      throw new WriteRefused(invalid(error.message));
    }
    throw error;
  }
  const place = placeBlock(store, pageId, data, undefined)!;
  return store.createBlock(pageId, {
    ...(data.id ? { id: data.id } : {}),
    type,
    properties: storedText(type, data.content ?? {}, data.properties ?? {}),
    ...place,
  });
}

/** Edits a block against the version the caller read. */
function applyEdit(
  store: MockStore,
  pageId: string,
  existing: MockBlockRecord,
  data: BlockWriteBody,
): MockBlockRecord {
  if (!(typeof data.expectedVersion === "number" && data.expectedVersion > 0)) {
    throw new WriteRefused(invalid("expectedVersion is required."));
  }
  if (data.expectedVersion !== existing.version) {
    throw new WriteRefused(versionConflict(existing));
  }
  let type = existing.type;
  if (data.type) {
    const resolved = resolveType(data.type);
    if (!resolved) {
      throw new WriteRefused(invalid(`Unknown block type '${data.type}'.`));
    }
    type = resolved;
  }
  try {
    validateBody(type, data.content, data.properties);
  } catch (error) {
    if (error instanceof BlockValidationError) {
      throw new WriteRefused(invalid(error.message));
    }
    throw error;
  }
  const body = readBlockBody(existing);
  const kept =
    type === existing.type
      ? { content: body.content, properties: body.properties }
      : retypeBody(existing.type, type, body);
  const sentContent = data.content && Object.keys(data.content).length > 0;
  const content = sentContent ? data.content! : kept.content;
  const properties = data.properties ?? kept.properties;
  const place = placeBlock(store, pageId, data, existing);
  return store.updateBlock(existing.id, {
    type,
    properties: storedText(
      type,
      TEXT_LIKE.has(type) ? content : {},
      properties,
    ),
    ...(place ?? {}),
  })!;
}

/** A block and everything under it. */
function subtree(blocks: readonly MockBlockRecord[], rootId: string): string[] {
  const ids = [rootId];
  for (let i = 0; i < ids.length; i += 1) {
    for (const b of blocks) {
      if (b.parentBlockId === ids[i]) ids.push(b.id);
    }
  }
  return ids;
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

  defineMockOperation<{ pageId: string }, BlockWriteBody, BlockDtoApi>({
    id: "docs.blocks.create",
    contract: {
      kind: "openapi",
      operationId: "Documents.Blocks.CreateBlock",
    } as any,
    method: "POST",
    route: "/pages/:pageId/blocks",
    async handle({ params, body, store }) {
      if (!store.getPage(params.pageId)) return notFound("Page not found");
      try {
        const block = applyCreate(store, params.pageId, body ?? {});
        store.createPageHistory(
          params.pageId,
          "usr-m-00001",
          "created_block",
          block.type,
        );
        // Like the producer: 201 with the stored block.
        return created<BlockDtoApi>(toBlockDto(block));
      } catch (error) {
        if (error instanceof WriteRefused) {
          return error.result as MockHttpResult<BlockDtoApi>;
        }
        throw error;
      }
    },
  }),

  // ─── PATCH /pages/:pageId/blocks/:blockId ───────────────────────────────

  defineMockOperation<
    { pageId: string; blockId: string },
    BlockWriteBody,
    BlockDtoApi
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
      try {
        // A retype keeps the block's id, position and parent.
        const block = applyEdit(store, params.pageId, existing, body ?? {});
        return ok<BlockDtoApi>(toBlockDto(block));
      } catch (error) {
        if (error instanceof WriteRefused) {
          return error.result as MockHttpResult<BlockDtoApi>;
        }
        throw error;
      }
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
    async handle({ params, query, store }) {
      const existing = store
        .getPageBlocks(params.pageId)
        .find((b) => b.id === params.blockId);
      if (!existing) return notFound("Block not found");
      // Like the producer: against the version read, and the blocks under it go too.
      const expected = Number(query.expectedVersion);
      if (!(expected > 0)) {
        return invalid("expectedVersion is required.") as MockHttpResult<void>;
      }
      if (expected !== existing.version) {
        return versionConflict(existing) as MockHttpResult<void>;
      }
      for (const id of subtree(
        store.getPageBlocks(params.pageId),
        existing.id,
      )) {
        store.deleteBlock(id);
      }
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
      blocks?: (BlockWriteBody & {
        id: string;
        op?: "upsert" | "delete";
      })[];
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
    async handle({ params, body, store }) {
      if (!store.getPage(params.pageId)) return notFound("Page not found");
      const ops = body?.blocks ?? [];
      if (ops.length < 1 || ops.length > 500) {
        return invalid("A batch holds 1 to 500 operations.") as MockHttpResult<
          BlockDtoApi[]
        >;
      }
      const ids = ops.map((op) => op.id);
      if (ids.some((id) => !id) || new Set(ids).size !== ids.length) {
        return invalid("Each block id appears once.") as MockHttpResult<
          BlockDtoApi[]
        >;
      }
      // All or nothing: a refused operation puts the page back as it was.
      const snapshot = store
        .getPageBlocks(params.pageId)
        .map((b) => ({ ...b }));
      const saved: MockBlockRecord[] = [];
      try {
        for (const op of ops) {
          const existing = store
            .getPageBlocks(params.pageId)
            .find((b) => b.id === op.id);
          if (op.op === "delete") {
            if (!existing) continue;
            if (!(
              typeof op.expectedVersion === "number" && op.expectedVersion > 0
            )) {
              throw new WriteRefused(invalid("expectedVersion is required."));
            }
            if (op.expectedVersion !== existing.version) {
              throw new WriteRefused(versionConflict(existing));
            }
            for (const id of subtree(
              store.getPageBlocks(params.pageId),
              existing.id,
            )) {
              store.deleteBlock(id);
            }
          } else if (existing) {
            saved.push(applyEdit(store, params.pageId, existing, op));
          } else {
            saved.push(applyCreate(store, params.pageId, op));
          }
        }
      } catch (error) {
        store.restorePageBlocks(params.pageId, snapshot);
        if (error instanceof WriteRefused) {
          return error.result as MockHttpResult<BlockDtoApi[]>;
        }
        throw error;
      }
      return ok<BlockDtoApi[]>(saved.map(toBlockDto));
    },
  }),
];
