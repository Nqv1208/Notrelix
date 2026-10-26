import {
  HIGHLIGHT_TOKENS,
  parseRichText,
  plainTextOf,
  plainTextOfRuns,
  runsFromPlainText,
  upgradeLegacyProperties,
  type BlockDraft,
  type BlockProperties,
  type BlockType,
  type RichTextV1,
  type TableCell,
  type TableData,
  type Run,
} from "@notrelix/docs-core";
import type { BlockDtoApi } from "../dto";

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
      const caption = plainTextOfRuns(runsOfWire(wire.caption));
      if (caption) properties.caption = caption;
      return properties;
    }
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
      if (properties.caption) {
        wire.caption = runsFromPlainText(properties.caption);
      }
      return wire;
    }
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
  dto:
    | Pick<BlockDtoApi, "content" | "properties">
    | {
        content: unknown;
        properties?: unknown;
      },
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
