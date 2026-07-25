import type { Block, CreateBlockPayload, UpdateBlockPayload, ReorderBlocksInput } from '../types/block';
import type { BlockDtoApi } from '../dto';
import { mapBlock } from '../model/block.mapper';
import type { DocsApiClient, PageApiEndpoints } from './page.api';

export function createBlockApi(
  api: DocsApiClient,
  endpoints: PageApiEndpoints,
) {
  return {
    async getList(pageId: string): Promise<Block[]> {
      const blocks = await api.get<BlockDtoApi[]>(
        endpoints.pages.blocks(pageId),
      );
      return blocks.map(mapBlock);
    },

    async create(pageId: string, payload: CreateBlockPayload): Promise<Block> {
      const block = await api.post<BlockDtoApi>(
        endpoints.pages.blocks(pageId),
        payload,
      );
      return mapBlock(block);
    },

    async update(blockId: string, payload: UpdateBlockPayload): Promise<Block> {
      const block = await api.patch<BlockDtoApi>(
        endpoints.blocks.detail(blockId),
        payload,
      );
      return mapBlock(block);
    },

    async delete(blockId: string): Promise<void> {
      await api.delete<void>(endpoints.blocks.detail(blockId));
    },

    async reorder(payload: ReorderBlocksInput): Promise<void> {
      await api.post<void>(endpoints.blocks.reorder, payload);
    },

    async batchUpdate(pageId: string, payloads: UpdateBlockPayload[]): Promise<Block[]> {
      const blocks = await api.post<BlockDtoApi[]>(
        endpoints.blocks.batch(pageId),
        { blocks: payloads },
      );
      return blocks.map(mapBlock);
    },
  };
}
