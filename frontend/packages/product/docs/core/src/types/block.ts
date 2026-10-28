import type { ID } from "./ids";
import type { HighlightToken, RichTextV1, Run } from "./rich-text";

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
  | "file"
  | "table"
  | "board_reference"
  | "page_reference"
  | "table_of_contents";

/** One cell of a table block: rich-text runs and an optional background token. */
export interface TableCell {
  runs: Run[];
  bg?: HighlightToken;
  /** Columns / rows the cell covers when merged (absent = 1). */
  colSpan?: number;
  rowSpan?: number;
}

export interface TableData {
  rows: TableCell[][];
  /** The first row is a header. */
  headerRow: boolean;
  /** Pixel width per column; `null` leaves the column to the table. */
  colWidths: (number | null)[];
}

export const TABLE_MIN_COL_WIDTH = 40;
export const TABLE_MAX_COL_WIDTH = 2000;

/** A new table of empty cells with a header row. */
export function emptyTable(rows = 3, columns = 3): TableData {
  return {
    rows: Array.from({ length: rows }, () =>
      Array.from({ length: columns }, (): TableCell => ({ runs: [] })),
    ),
    headerRow: true,
    colWidths: Array.from({ length: columns }, () => null),
  };
}

export interface BlockProperties {
  text?: string;
  checked?: boolean;
  language?: string;
  url?: string;
  /** An image's caption as plain text (the text of `captionRuns`). */
  caption?: string;
  /** An image's caption with its bold, italic, code and link marks. */
  captionRuns?: Run[];
  color?: string;
  icon?: string;
  title?: string;
  items?: string[];
  /** A table block's cells, header flag and column widths. */
  table?: TableData;
  linkedPageId?: ID;
  linkedBoardId?: ID;
  /** The board view a board reference opens. */
  viewId?: ID;
  linkedTaskId?: ID;
  mentionIds?: ID[];
  align?: "left" | "center" | "right" | "justify";
  /** Indent level of a text block, 1..8 (absent = 0). */
  indent?: number;
  /** Line spacing of a text block (absent = normal). */
  lineSpacing?: "tight" | "relaxed" | "loose";
  /** A callout's tone. */
  tone?: "info" | "success" | "warning" | "danger" | "neutral";
  /** A toggle's collapsed state. */
  collapsed?: boolean;
  attachmentId?: ID;
  width?: number;
  alt?: string;
  fontFamily?: "inter" | "poppins" | "serif" | "mono";
  fontSize?: "sm" | "base" | "lg" | "xl";
  bold?: boolean;
  italic?: boolean;
  underline?: boolean;
  strike?: boolean;
  textColor?:
    | "default"
    | "muted"
    | "primary"
    | "accent"
    | "destructive"
    | "success"
    | "warning"
    | "danger";
  highlight?:
    "none" | "muted" | "accent" | "primary" | "success" | "warning" | "danger";
  commentsCount?: number;
}

export interface Block {
  id: ID;
  pageId: ID;
  type: BlockType;
  /**
   * The authored text as rich text v1. Absent on a block read from a legacy
   * producer shape; `blockContent` then upgrades `properties` on demand.
   * `properties.text` stays the plain text of it for read-only consumers.
   */
  content?: RichTextV1;
  properties: BlockProperties;
  /** The block version the producer sent, for `expectedVersion` writes. */
  version?: number;
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
  /** The block version the caller read; the producer refuses a stale write. */
  expectedVersion?: number;
}

/** Block types whose content is text the author typed. */
export const TEXT_BLOCK_TYPES: ReadonlySet<BlockType> = new Set<BlockType>([
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
  if (!TEXT_BLOCK_TYPES.has(from) || !TEXT_BLOCK_TYPES.has(to)) return {};
  const converted: BlockProperties = { ...properties };
  if (to !== "todo") delete converted.checked;
  if (to !== "code") delete converted.language;
  return converted;
}

export interface ReorderBlocksInput {
  pageId: ID;
  orderedBlockIds: ID[];
}
