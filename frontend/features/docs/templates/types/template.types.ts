import type { ID } from "../../shared/types/ids.types"
import type { BlockType } from "../../blocks/types/block.types"

export interface PageTemplate {
  id: ID
  name: string
  description: string
  icon: string
  accent: string
  blockTypes: BlockType[]
}
