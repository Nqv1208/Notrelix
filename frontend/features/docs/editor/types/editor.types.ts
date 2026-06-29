import type { ID } from "../../shared/types/ids.types"
import type { BlockType, BlockProperties } from "../../blocks/types/block.types"

export interface EditorSelectionState {
  activeBlockId: ID | null
  hasSelection: boolean
  selectionText: string
  selectionRect: DOMRect | null
}

export interface DocToolbarState {
  activeBlockId: ID | null
  activeBlockType: BlockType
  properties: BlockProperties
}

export interface SlashCommandItem {
  id: string
  type: BlockType
  label: string
  description: string
  keywords: string[]
}
