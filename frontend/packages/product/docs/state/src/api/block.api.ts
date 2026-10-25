import type {
  Block,
  CreateBlockPayload,
  UpdateBlockPayload,
  ReorderBlocksInput,
} from "@notrelix/docs-core";
import type { BlockDtoApi } from "../dto";
import { mapBlock, toProducerBlockType } from "../model/block.mapper";
import type { DocsApiClient, PageApiEndpoints } from "./page.api";

export function createBlockApi(
  api: DocsApiClient,
  endpoints: PageApiEndpoints,
) {
  return {
    /** The blocks in the producer's order, numbered by their place in it. */
    async getList(pageId: string): Promise<Block[]> {
      const blocks = await api.get<BlockDtoApi[]>(
        endpoints.pages.blocks(pageId),
      );
      return blocks.map((dto, index) => mapBlock(dto, (index + 1) * 1000));
    },

    /** `Documents.Blocks.CreateBlock` answers the new block's id. */
    async create(pageId: string, payload: CreateBlockPayload): Promise<Block> {
      const properties = payload.properties ?? {};
      const id = await api.post<string>(endpoints.pages.blocks(pageId), {
        type: toProducerBlockType(payload.type),
        properties: JSON.stringify(properties),
        ...(payload.afterBlockId ? { afterBlockId: payload.afterBlockId } : {}),
        ...(payload.parentId ? { parentId: payload.parentId } : {}),
      });
      const now = new Date().toISOString();
      return {
        id,
        pageId,
        type: payload.type,
        properties,
        position: 0,
        parentId: payload.parentId ?? null,
        createdById: "",
        updatedById: "",
        createdAt: now,
        updatedAt: now,
      };
    },

    /** `Documents.Blocks.UpdateBlock` rewrites the block's content. */
    async update(
      pageId: string,
      blockId: string,
      payload: UpdateBlockPayload,
    ): Promise<void> {
      await api.patch<void>(endpoints.blocks.detail(pageId, blockId), {
        ...(payload.properties
          ? { properties: JSON.stringify(payload.properties) }
          : {}),
      });
    },

    async delete(pageId: string, blockId: string): Promise<void> {
      await api.delete<void>(endpoints.blocks.detail(pageId, blockId));
    },

    // TRANSITION: no screen reorders blocks yet; the producer's reorder and
    // batch writes take opaque position keys, so these two keep the page's
    // numeric shape until a screen needs them.
    async reorder(payload: ReorderBlocksInput): Promise<void> {
      await api.post<void>(endpoints.blocks.reorder, payload);
    },

    async batchUpdate(
      pageId: string,
      payloads: UpdateBlockPayload[],
    ): Promise<Block[]> {
      const blocks = await api.post<BlockDtoApi[]>(
        endpoints.blocks.batch(pageId),
        { blocks: payloads },
      );
      return blocks.map((dto, index) => mapBlock(dto, (index + 1) * 1000));
    },
  };
}
