import type { BlockType } from "../../blocks/types/block.types"

export const DOCS_STALE_TIME = 30_000
export const BLOCKS_STALE_TIME = 10_000

export const BLOCK_LABELS: Record<BlockType, string> = {
  paragraph: "Text",
  heading_1: "Heading 1",
  heading_2: "Heading 2",
  heading_3: "Heading 3",
  bulleted_list: "Bullet list",
  numbered_list: "Numbered list",
  todo: "To-do",
  quote: "Quote",
  divider: "Divider",
  code: "Code",
  callout: "Callout",
  toggle: "Toggle",
  image: "Image",
  embed: "Embed",
  table: "Table",
  board_reference: "Board reference",
  page_reference: "Page reference",
}

export const SLASH_COMMAND_BLOCKS: BlockType[] = [
  "paragraph",
  "heading_1",
  "heading_2",
  "heading_3",
  "bulleted_list",
  "numbered_list",
  "todo",
  "callout",
  "quote",
  "code",
  "table",
  "page_reference",
  "board_reference",
]
