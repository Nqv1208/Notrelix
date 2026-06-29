import { api, type ApiRequestOptions } from "@/lib/api/api-client"
import { endpoints } from "@/lib/api/endpoints"
import type { Block, CreateBlockPayload, ReorderBlocksInput, UpdateBlockPayload } from "../types/block.types"
import type { BlockDtoApi } from "../../shared/types/dto"
import { mapBlock } from "../model/block.mapper"

// eslint-disable-next-line @typescript-eslint/no-explicit-any
function serializeProperties(properties?: Record<string, any>) {
  return JSON.stringify(properties ?? {})
}

export const blockApi = {
  async getByPage(pageId: string, options?: ApiRequestOptions): Promise<Block[]> {
    const blocks = await api.get<BlockDtoApi[]>(endpoints.pages.blocks(pageId), options)
    return blocks.map(mapBlock)
  },

  async create(pageId: string, payload: CreateBlockPayload): Promise<Block> {
    const id = await api.post<string>(endpoints.pages.blocks(pageId), {
      type: payload.type,
      properties: serializeProperties(payload.properties),
      position: payload.position,
      parentId: payload.parentId,
    })
    // react-doctor-disable-next-line react-doctor/server-sequential-independent-await
    const blocks = await this.getByPage(pageId)
    return blocks.find((block) => block.id === id) ?? {
      id,
      pageId,
      type: payload.type,
      properties: payload.properties ?? {},
      position: payload.position ?? 0,
      parentId: payload.parentId ?? null,
      createdById: "",
      updatedById: "",
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    }
  },

  async update(blockId: string, payload: UpdateBlockPayload): Promise<Block> {
    await api.patch<void>(endpoints.blocks.detail(blockId), {
      type: payload.type,
      properties: payload.properties ? serializeProperties(payload.properties) : undefined,
    })
    return {
      id: blockId,
      pageId: "",
      type: payload.type ?? "paragraph",
      properties: payload.properties ?? {},
      position: payload.position ?? 0,
      parentId: payload.parentId ?? null,
      createdById: "",
      updatedById: "",
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    }
  },

  async delete(blockId: string): Promise<void> {
    await api.delete<void>(endpoints.blocks.detail(blockId))
  },

  async reorder(payload: ReorderBlocksInput): Promise<void> {
    await api.post<void>(endpoints.blocks.reorder, {
      pageId: payload.pageId,
      items: payload.orderedBlockIds.map((blockId, index) => ({
        blockId,
        position: index + 1,
      })),
    })
  },

  async batchUpdate(pageId: string, blocks: Array<{ id: string } & UpdateBlockPayload>): Promise<Block[]> {
    await api.post<string[]>(endpoints.blocks.batch(pageId), {
      blocks: blocks.map((block) => ({
        id: block.id,
        type: block.type,
        properties: block.properties ? serializeProperties(block.properties) : undefined,
        position: block.position,
        parentId: block.parentId,
      })),
    })
    return this.getByPage(pageId)
  },
}
