import {
  upgradeLegacyProperties,
  type Block,
  type BlockDraft,
  type CreateBlockPayload,
  type ReorderBlocksInput,
  type UpdateBlockPayload,
} from "@notrelix/docs-core";
import type { BatchBlockItemApi, BlockDtoApi } from "../dto";
import { mapBlock, toProducerBlockType } from "../model/block.mapper";
import { encodeContent, encodeProperties } from "../model/block-wire";
import type { DocsApiClient, PageApiEndpoints } from "./page.api";

/** The page numbers blocks by their place in the producer's ordered list. */
function numbered(blocks: readonly BlockDtoApi[]): Block[] {
  return blocks.map((dto, index) => mapBlock(dto, (index + 1) * 1000));
}

/** The draft a page-side payload (`type` plus flat properties) stands for. */
function draftOfPayload(
  type: BlockDraft["type"],
  properties: Block["properties"] | undefined,
): Pick<BlockDraft, "type" | "content" | "properties"> {
  return { type, ...upgradeLegacyProperties(type, properties ?? {}) };
}

export function createBlockApi(
  api: DocsApiClient,
  endpoints: PageApiEndpoints,
) {
  return {
    /** The blocks in the producer's order, numbered by their place in it. */
    async getList(pageId: string): Promise<Block[]> {
      return numbered(
        await api.get<BlockDtoApi[]>(endpoints.pages.blocks(pageId)),
      );
    },

    /** `Documents.Blocks.CreateBlock` answers the stored block. */
    async create(pageId: string, payload: CreateBlockPayload): Promise<Block> {
      const draft = draftOfPayload(payload.type, payload.properties);
      const dto = await api.post<BlockDtoApi>(endpoints.pages.blocks(pageId), {
        type: toProducerBlockType(payload.type),
        content: encodeContent(draft),
        properties: encodeProperties(draft),
        ...(payload.afterBlockId ? { afterBlockId: payload.afterBlockId } : {}),
        ...(payload.parentId ? { parentId: payload.parentId } : {}),
      });
      return mapBlock(dto, 0);
    },

    /**
     * `Documents.Blocks.UpdateBlock`: a `type` retypes the block in place (it
     * keeps its id, position and parent), `properties` rewrites its content.
     * The producer needs the version the caller read (`expectedVersion`).
     */
    async update(
      pageId: string,
      blockId: string,
      payload: UpdateBlockPayload,
    ): Promise<Block> {
      const type = payload.type;
      const draft = payload.properties
        ? draftOfPayload(type ?? "paragraph", payload.properties)
        : null;
      const dto = await api.patch<BlockDtoApi>(
        endpoints.blocks.detail(pageId, blockId),
        {
          expectedVersion: payload.expectedVersion,
          ...(type ? { type: toProducerBlockType(type) } : {}),
          ...(draft && type
            ? {
                content: encodeContent(draft),
                properties: encodeProperties(draft),
              }
            : {}),
        },
      );
      return mapBlock(dto, 0);
    },

    async delete(pageId: string, blockId: string): Promise<void> {
      await api.delete<void>(endpoints.blocks.detail(pageId, blockId));
    },

    /**
     * `Documents.Blocks.BatchUpdateBlocks`: the operations apply in order in
     * one transaction (all or nothing); answers the upserted blocks.
     */
    async batch(
      pageId: string,
      items: readonly BatchBlockItemApi[],
    ): Promise<BlockDtoApi[]> {
      return api.post<BlockDtoApi[]>(endpoints.blocks.batch(pageId), {
        blocks: items,
      });
    },

    // TRANSITION: no screen reorders blocks; the editor's saver places blocks
    // through `batch`.
    async reorder(payload: ReorderBlocksInput): Promise<void> {
      await api.post<void>(endpoints.blocks.reorder, payload);
    },
  };
}
