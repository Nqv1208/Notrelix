import type { ID } from "./ids";

export type BlockType =
  | "paragraph"
  | "heading_1"
  | "heading_2"
  | "heading_3"
  | "bulleted_list"
  | "numbered_list"
  | "todo"
  | "quote"
  | "divider"
  | "code"
  | "callout"
  | "toggle"
  | "image"
  | "table"
  | "board_reference"
  | "page_reference";

export interface BlockProperties {
  text?: string;
  checked?: boolean;
  language?: string;
  url?: string;
  caption?: string;
  color?: string;
  icon?: string;
  title?: string;
  items?: string[];
  rows?: string[][];
  linkedPageId?: ID;
  linkedBoardId?: ID;
  linkedTaskId?: ID;
  mentionIds?: ID[];
  align?: "left" | "center" | "right";
  fontFamily?: "inter" | "poppins" | "serif" | "mono";
  fontSize?: "sm" | "base" | "lg" | "xl";
  bold?: boolean;
  italic?: boolean;
  underline?: boolean;
  strike?: boolean;
  textColor?: "default" | "muted" | "primary" | "accent" | "destructive";
  highlight?: "none" | "muted" | "accent" | "primary";
  commentsCount?: number;
}

export interface Block {
  id: ID;
  pageId: ID;
  type: BlockType;
  properties: BlockProperties;
  position: number;
  parentId: ID | null;
  children?: Block[];
  createdById: ID;
  updatedById: ID;
  createdAt: string;
  updatedAt: string;
}

export interface CreateBlockPayload {
  type: BlockType;
  properties?: BlockProperties;
  /** The sibling the block goes right after; omitted puts it last. */
  afterBlockId?: ID | null;
  parentId?: ID | null;
}

/**
 * Edits one block. A `type` retypes it in place (the block keeps its id,
 * position and parent); `properties` then replaces its content.
 */
export interface UpdateBlockPayload {
  type?: BlockType;
  properties?: BlockProperties;
}

/** Block types whose content is text the author typed. */
const TEXT_LIKE: ReadonlySet<BlockType> = new Set<BlockType>([
  "paragraph",
  "heading_1",
  "heading_2",
  "heading_3",
  "bulleted_list",
  "numbered_list",
  "todo",
  "toggle",
  "quote",
  "callout",
  "code",
]);

/**
 * The content a block has after it is retyped, as the producer converts it
 * (`BlockContentConversion`): text carries over between text-like types, a
 * checked state only into a task line and a language only into code; a change
 * to or from any other type starts empty.
 */
export function convertBlockProperties(
  from: BlockType,
  to: BlockType,
  properties: BlockProperties,
): BlockProperties {
  if (!TEXT_LIKE.has(from) || !TEXT_LIKE.has(to)) return {};
  const converted: BlockProperties = { ...properties };
  if (to !== "todo") delete converted.checked;
  if (to !== "code") delete converted.language;
  return converted;
}

export interface ReorderBlocksInput {
  pageId: ID;
  orderedBlockIds: ID[];
}
