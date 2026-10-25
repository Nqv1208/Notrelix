import type { Block, BlockProperties, BlockType } from "@notrelix/docs-core";
import type { BlockDtoApi } from "../dto";

export function parseProperties(
  value: BlockDtoApi["content"] | BlockProperties | undefined,
): BlockProperties {
  if (value && typeof value === "object") return value;
  if (typeof value !== "string") return {};
  try {
    const parsed: unknown = JSON.parse(value);
    return parsed && typeof parsed === "object"
      ? (parsed as BlockProperties)
      : {};
  } catch {
    return {};
  }
}

/**
 * The block types the producer stores (`Documents` `BlockType`) and the name
 * the page uses for each. A file or video block has no editor type; the page
 * reads it as an empty paragraph.
 */
const FROM_PRODUCER: Readonly<Record<string, BlockType>> = {
  Text: "paragraph",
  Heading1: "heading_1",
  Heading2: "heading_2",
  Heading3: "heading_3",
  BulletList: "bulleted_list",
  NumberedList: "numbered_list",
  ToDo: "todo",
  Toggle: "toggle",
  Quote: "quote",
  Divider: "divider",
  Code: "code",
  Image: "image",
  Board: "board_reference",
  Table: "table",
  Callout: "callout",
  PageReference: "page_reference",
};

const TO_PRODUCER: Readonly<Partial<Record<BlockType, string>>> = {
  paragraph: "Text",
  heading_1: "Heading1",
  heading_2: "Heading2",
  heading_3: "Heading3",
  bulleted_list: "BulletList",
  numbered_list: "NumberedList",
  todo: "ToDo",
  toggle: "Toggle",
  quote: "Quote",
  divider: "Divider",
  code: "Code",
  image: "Image",
  board_reference: "Board",
  table: "Table",
  callout: "Callout",
  page_reference: "PageReference",
};

/** The block types a page can create on the producer. */
export const CREATABLE_BLOCK_TYPES: readonly BlockType[] = Object.keys(
  TO_PRODUCER,
) as BlockType[];

export function toProducerBlockType(type: BlockType): string {
  const name = TO_PRODUCER[type];
  if (!name) throw new Error(`Block type ${type} cannot be stored.`);
  return name;
}

/**
 * A block as the page reads it. The producer orders the list and keeps
 * its position as an opaque key, so the page numbers the blocks by their
 * place in the list (`position`).
 */
export function mapBlock(dto: BlockDtoApi, position: number): Block {
  return {
    id: dto.id,
    pageId: dto.pageId,
    type: FROM_PRODUCER[dto.type] ?? "paragraph",
    properties: parseProperties(dto.content),
    position,
    parentId: dto.parentId ?? null,
    createdById: "",
    updatedById: "",
    createdAt: dto.createdAt,
    updatedAt: dto.updatedAt ?? dto.createdAt,
  };
}
