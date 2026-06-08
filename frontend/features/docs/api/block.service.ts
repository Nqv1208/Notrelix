import { api } from "@/lib/api/api-client"
import { endpoints } from "@/lib/api/endpoints"
import type { Block, BlockProperties, CreateBlockPayload, ReorderBlocksInput, UpdateBlockPayload } from "../types"
import type { BlockDtoApi } from "../types/dto"

function parseProperties(value: BlockDtoApi["properties"]): BlockProperties {
  if (typeof value === "object") return value
  try {
    const parsed = JSON.parse(value)
    return parsed && typeof parsed === "object" ? parsed : {}
  } catch {
    return {}
  }
}

function mapBlock(dto: BlockDtoApi): Block {
  return {
    id: dto.id,
    pageId: dto.pageId,
    type: dto.type,
    properties: parseProperties(dto.properties),
    position: dto.position,
    parentId: dto.parentBlockId ?? null,
    createdById: dto.createdByUserId,
    updatedById: dto.createdByUserId,
    createdAt: dto.createdAt,
    updatedAt: dto.updatedAt ?? dto.createdAt,
  }
}

function serializeProperties(properties?: BlockProperties) {
  return JSON.stringify(properties ?? {})
}

export const blockService = {
  async getByPage(pageId: string): Promise<Block[]> {
    const blocks = await api.get<BlockDtoApi[]>(endpoints.pages.blocks(pageId))
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
