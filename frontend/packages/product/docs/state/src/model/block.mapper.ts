import {
  blockFromWire,
  type Block,
  type BlockProperties,
  type BlockType,
} from "@notrelix/docs-core";
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
  file: "File",
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

/** A block as the page reads it (see `blockFromWire`). */
export function mapBlock(dto: BlockDtoApi, position: number): Block {
  return blockFromWire(dto, position);
}
