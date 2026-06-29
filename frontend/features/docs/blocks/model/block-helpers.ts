import type { Block, BlockType, CreateBlockPayload } from "../types/block.types"

/** Placeholder text for each block type (shown when block is empty) */
export function getBlockPlaceholder(type: BlockType): string {
  const map: Record<BlockType, string> = {
    paragraph: "Type '/' for commands...",
    heading_1: "Heading 1",
    heading_2: "Heading 2",
    heading_3: "Heading 3",
    bulleted_list: "List item",
    numbered_list: "List item",
    todo: "To-do",
    toggle: "Toggle",
    quote: "Quote",
    callout: "Callout",
    divider: "",
    image: "Add an image URL",
    embed: "Paste an embed URL",
    code: "Write code here...",
    table: "",
    board_reference: "Link a board",
    page_reference: "Link a page",
  }
  return map[type]
}

/** Check if a block type supports text content editing */
export function isTextBlock(type: BlockType): boolean {
  return [
    "paragraph", "heading_1", "heading_2", "heading_3",
    "bulleted_list", "numbered_list", "todo", "toggle",
    "quote", "callout", "code",
  ].includes(type)
}

/** Display label for a block type (slash command menu) */
export function getBlockLabel(type: BlockType): string {
  const map: Record<BlockType, string> = {
    paragraph: "Text",
    heading_1: "Heading 1",
    heading_2: "Heading 2",
    heading_3: "Heading 3",
    bulleted_list: "Bulleted List",
    numbered_list: "Numbered List",
    todo: "To-do List",
    toggle: "Toggle",
    quote: "Quote",
    callout: "Callout",
    divider: "Divider",
    image: "Image",
    embed: "Embed",
    code: "Code Block",
    table: "Table",
    board_reference: "Board Reference",
    page_reference: "Page Reference",
  }
  return map[type]
}

/** Create a default block payload */
export function createDefaultBlock(
  type: BlockType,
  position: number,
  parentId: string | null = null
): CreateBlockPayload {
  return {
    type,
    properties: type !== "divider" ? { text: "" } : {},
    position,
    parentId,
  }
}

/** Flatten a nested block tree to a flat ordered array */
export function flattenBlocks(blocks: Block[]): Block[] {
  const result: Block[] = []
  function traverse(items: Block[]) {
    for (const block of items) {
      result.push(block)
      if (block.children?.length) traverse(block.children)
    }
  }
  traverse(blocks)
  return result
}

/** Get the text content of a block for search/preview */
export function getBlockText(block: Block): string {
  return block.properties.text ?? ""
}
