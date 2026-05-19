import { mockPageService } from "../mock/mock-page-service"
import type { CreateBlockPayload, ReorderBlocksInput, UpdateBlockPayload } from "../types"

export const blocksApi = {
  // TODO(api):
  // Replace with real block endpoints:
  // GET /api/pages/:pageId/blocks
  // POST /api/pages/:pageId/blocks
  // PATCH /api/blocks/:blockId
  getByPage: (pageId: string) => mockPageService.getBlocks(pageId),
  create: (pageId: string, payload: CreateBlockPayload) => mockPageService.createBlock(pageId, payload),
  update: (blockId: string, payload: UpdateBlockPayload) => mockPageService.updateBlock(blockId, payload),
  delete: (blockId: string) => mockPageService.deleteBlock(blockId),
  reorder: (payload: ReorderBlocksInput) => mockPageService.reorderBlocks(payload.pageId, payload.orderedBlockIds),
  batchUpdate: async (pageId: string, blocks: Array<{ id: string } & UpdateBlockPayload>) => {
    const updated = await Promise.all(blocks.map((block) => mockPageService.updateBlock(block.id, block)))
    return updated.filter((block) => block.pageId === pageId)
  },
}
