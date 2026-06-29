import type { Block, BlockProperties } from "../types/block.types"
import type { BlockDtoApi } from "../../shared/types/dto"

export function parseProperties(value: BlockDtoApi["properties"]): BlockProperties {
  if (typeof value === "object") return value
  try {
    const parsed = JSON.parse(value)
    return parsed && typeof parsed === "object" ? parsed : {}
  } catch {
    return {}
  }
}

export function mapBlock(dto: BlockDtoApi): Block {
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
