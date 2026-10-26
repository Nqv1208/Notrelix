import {
  HIGHLIGHT_TOKENS,
  parseRichText,
  plainTextOf,
  plainTextOfRuns,
  runsFromPlainText,
  type RichTextV1,
  type Run,
} from "./rich-text";
import { sanitizeCaptionRuns } from "./attachment-rules";
import { upgradeLegacyProperties, type BlockDraft } from "./block-content";
import type {
  Block,
  BlockProperties,
  BlockType,
  TableCell,
  TableData,
} from "./block";

/** A block as the producer sends it (`Documents` `BlockDto`). */
export type BlockWireDto = {
  id: string;
  pageId: string;
  parentId?: string | null;
  /** The producer's block type name (`Text`, `Heading1`, `ToDo`, ...). */
  type: string;
  content: unknown;
  properties?: unknown;
  version: number;
  createdAt: string;
  updatedAt?: string | null;
};

/**
 * How a block's authored text and properties travel to and from the
 * producer: `content` is rich text v1 (`{ v: 1, runs }`) for a text-like block
 * and `{}` otherwise, `properties` holds the type's own values as a JSON
 * object. Both are objects on the wire, never JSON text.
 */
function asObject(value: unknown): Record<string, unknown> | null {
  if (value && typeof value === "object" && !Array.isArray(value)) {
    return value as Record<string, unknown>;
  }
  if (typeof value !== "string") return null;
  try {
    const parsed: unknown = JSON.parse(value);
    return parsed && typeof parsed === "object" && !Array.isArray(parsed)
      ? (parsed as Record<string, unknown>)
      : null;
  } catch {
    return null;
  }
}

const SIMPLE_KEYS: Readonly<Partial<Record<BlockType, readonly string[]>>> = {
  todo: ["checked"],
  code: ["language"],
  callout: ["icon", "tone"],
  toggle: ["collapsed"],
};

const ALIGNABLE: ReadonlySet<BlockType> = new Set<BlockType>([
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
]);

function pick(
  source: Record<string, unknown>,
  keys: readonly string[],
): Record<string, unknown> {
  const picked: Record<string, unknown> = {};
  for (const key of keys) {
    if (source[key] !== undefined && source[key] !== null) {
      picked[key] = source[key];
    }
  }
  return picked;
}

function keysOf(type: BlockType): readonly string[] {
  return [
    ...(SIMPLE_KEYS[type] ?? []),
    ...(ALIGNABLE.has(type) ? ["align"] : []),
  ];
}

function runsOfWire(value: unknown): Run[] {
  return Array.isArray(value) ? (value as Run[]) : [];
}

function cellRuns(value: unknown): Run[] {
  return parseRichText({ v: 1, runs: runsOfWire(value) })?.runs ?? [];
}

/** A table block's wire properties as the editor's table. */
export function tableFromWire(wire: Record<string, unknown>): TableData {
  const rows: TableCell[][] = (Array.isArray(wire.rows) ? wire.rows : []).map(
    (row: unknown) =>
      (Array.isArray(row) ? row : []).map((raw: unknown): TableCell => {
        const cell = (raw ?? {}) as { runs?: unknown; bg?: unknown };
        const bg = HIGHLIGHT_TOKENS.find((token) => token === cell.bg);
        return {
          runs: cellRuns(cell.runs),
          ...(bg && bg !== "none" ? { bg } : {}),
        };
      }),
  );
  const columns = Math.max(0, ...rows.map((row) => row.length));
  const widths = Array.isArray(wire.colWidths) ? wire.colWidths : [];
  return {
    rows,
    headerRow: wire.headerRow === true,
    colWidths: Array.from({ length: columns }, (_, index) => {
      const width = widths[index];
      return typeof width === "number" ? width : null;
    }),
  };
}

/** The editor's table as the producer's table properties. */
export function tableToWire(table: TableData): Record<string, unknown> {
  const columns = Math.max(0, ...table.rows.map((row) => row.length));
  return {
    rows: table.rows.map((row) =>
      row.map((cell) => ({
        runs: cell.runs,
        ...(cell.bg && cell.bg !== "none" ? { bg: cell.bg } : {}),
      })),
    ),
    headerRow: table.headerRow,
    colWidths: Array.from(
      { length: columns },
      (_, index) => table.colWidths[index] ?? null,
    ),
  };
}

/** The page's properties of a block as the producer sends them. */
export function propertiesFromWire(
  type: BlockType,
  wire: Record<string, unknown>,
): BlockProperties {
  switch (type) {
    case "image": {
      const properties: BlockProperties = {
        ...(pick(wire, ["attachmentId", "width", "alt"]) as BlockProperties),
      };
      const runs = sanitizeCaptionRuns(runsOfWire(wire.caption));
      const caption = plainTextOfRuns(runs);
      if (caption) properties.caption = caption;
      // Plain captions stay a string; only a marked one carries its runs.
      if (runs.some((run) => run.marks.length > 0))
        properties.captionRuns = runs;
      return properties;
    }
    case "file":
      return typeof wire.attachmentId === "string"
        ? { attachmentId: wire.attachmentId }
        : {};
    case "board_reference":
      return {
        ...(typeof wire.boardId === "string"
          ? { linkedBoardId: wire.boardId }
          : {}),
        ...(typeof wire.viewId === "string" ? { viewId: wire.viewId } : {}),
      };
    case "page_reference":
      return typeof wire.pageId === "string"
        ? { linkedPageId: wire.pageId }
        : {};
    case "table":
      return { table: tableFromWire(wire) };
    case "divider":
      return {};
    default:
      return pick(wire, keysOf(type)) as BlockProperties;
  }
}

/** The producer's `properties` for a block's page-side properties. */
export function propertiesToWire(
  type: BlockType,
  properties: BlockProperties,
): Record<string, unknown> {
  const source = properties as Record<string, unknown>;
  switch (type) {
    case "image": {
      const wire = pick(source, ["attachmentId", "width", "alt"]);
      const runs = sanitizeCaptionRuns(properties.captionRuns ?? []);
      if (runs.length > 0) wire.caption = runs;
      else if (properties.caption) {
        wire.caption = runsFromPlainText(properties.caption);
      }
      return wire;
    }
    case "file":
      return properties.attachmentId
        ? { attachmentId: properties.attachmentId }
        : {};
    case "board_reference":
      return {
        ...(properties.linkedBoardId
          ? { boardId: properties.linkedBoardId }
          : {}),
        ...(properties.viewId ? { viewId: properties.viewId } : {}),
      };
    case "page_reference":
      return properties.linkedPageId ? { pageId: properties.linkedPageId } : {};
    case "table":
      return properties.table ? tableToWire(properties.table) : {};
    case "divider":
      return {};
    default:
      return pick(source, keysOf(type));
  }
}

export interface DecodedBlockBody {
  content: RichTextV1;
  /** The type's properties plus `text`, the plain text read-only views use. */
  properties: BlockProperties;
}

export function decodeBlockBody(
  type: BlockType,
  dto: { content: unknown; properties?: unknown },
): DecodedBlockBody {
  const body = asObject(dto.content) ?? {};
  const wire = asObject(dto.properties) ?? {};
  const rich = parseRichText(body);
  if (rich) {
    return {
      content: rich,
      properties: {
        ...propertiesFromWire(type, wire),
        text: plainTextOf(rich),
      },
    };
  }
  // A producer that has not upgraded the block yet still sends the flat
  // legacy body in `content`.
  const upgraded = upgradeLegacyProperties(type, body as BlockProperties);
  return {
    content: upgraded.content,
    properties: {
      ...upgraded.properties,
      ...propertiesFromWire(type, wire),
      text: plainTextOf(upgraded.content),
    },
  };
}

/** The producer's `content` for a draft: rich text for a text-like block. */
export function encodeContent(
  draft: Pick<BlockDraft, "type" | "content">,
): Record<string, unknown> {
  return textLike(draft.type) ? { v: 1, runs: draft.content.runs } : {};
}

function textLike(type: BlockType): boolean {
  return (
    type !== "divider" &&
    type !== "image" &&
    type !== "file" &&
    type !== "table" &&
    type !== "board_reference" &&
    type !== "page_reference"
  );
}

export function encodeProperties(
  draft: Pick<BlockDraft, "type" | "properties">,
): Record<string, unknown> {
  return propertiesToWire(draft.type, draft.properties);
}

/**
 * The block types the producer stores (`Documents` `BlockType`) and the name
 * the page uses for each. A video block has no editor type; the page
 * reads it as an empty paragraph.
 */
export const FROM_PRODUCER_BLOCK_TYPE: Readonly<Record<string, BlockType>> = {
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
  File: "file",
  Board: "board_reference",
  Table: "table",
  Callout: "callout",
  PageReference: "page_reference",
};

/**
 * A block as a page reads it. The producer orders the list and keeps its
 * position as an opaque key, so the reader numbers the blocks by their place
 * in the list (`position`).
 */
export function blockFromWire(dto: BlockWireDto, position: number): Block {
  const type = FROM_PRODUCER_BLOCK_TYPE[dto.type] ?? "paragraph";
  const body = decodeBlockBody(type, dto);
  return {
    id: dto.id,
    pageId: dto.pageId,
    type,
    content: body.content,
    properties: body.properties,
    version: dto.version,
    position,
    parentId: dto.parentId ?? null,
    createdById: "",
    updatedById: "",
    createdAt: dto.createdAt,
    updatedAt: dto.updatedAt ?? dto.createdAt,
  };
}

/** The blocks of a producer's ordered list, numbered by their place in it. */
export function blocksFromWire(dtos: readonly BlockWireDto[]): Block[] {
  return dtos.map((dto, index) => blockFromWire(dto, (index + 1) * 1000));
}
