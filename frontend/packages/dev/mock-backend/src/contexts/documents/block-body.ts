/**
 * What a stored block holds, as the producer's `BlockDto` carries it: rich
 * text v1 in `content` (text-like types) and the type's own values in
 * `properties`, both JSON objects. The store keeps one JSON text per block
 * (`MockBlockRecord.properties`): `{ v: 1, runs, ...properties }` once the
 * block was written, or the flat legacy shape the seed data uses
 * (`{ text, bold, ... }`), which reads as the producer's migration upgrades it.
 */

export type JsonObject = Record<string, unknown>;

export interface BlockBody {
  /** `{ v: 1, runs }` for a text-like type, `{}` otherwise. */
  content: JsonObject;
  /** The type's own properties, with the producer's names. */
  properties: JsonObject;
  /** The plain text of the block (search, history). */
  text: string;
}

export const TEXT_LIKE = new Set([
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

const ALIGN = ["align"];
const OWN_KEYS: Record<string, readonly string[]> = {
  todo: ["checked", ...ALIGN],
  code: ["language"],
  callout: ["icon", "tone", ...ALIGN],
  toggle: ["collapsed", ...ALIGN],
  image: ["attachmentId", "width", "alt", "caption"],
  file: ["attachmentId"],
  video: ["attachmentId"],
  board_reference: ["boardId", "viewId"],
  page_reference: ["pageId"],
  table: ["rows", "headerRow", "colWidths"],
  divider: [],
};

/** The property names a block type accepts (closed set, like the producer). */
export function allowedKeys(type: string): readonly string[] {
  return OWN_KEYS[type] ?? ALIGN;
}

interface Run {
  text: string;
  marks: { type: string; [key: string]: unknown }[];
}

const LEGACY_COLORS = new Set([
  "muted",
  "primary",
  "accent",
  "success",
  "warning",
  "danger",
]);

function parse(text: string): JsonObject {
  try {
    const value: unknown = JSON.parse(text || "{}");
    return value && typeof value === "object" && !Array.isArray(value)
      ? (value as JsonObject)
      : {};
  } catch {
    return {};
  }
}

function runsText(runs: unknown): string {
  return Array.isArray(runs)
    ? (runs as Run[]).map((run) => String(run.text ?? "")).join("")
    : "";
}

function pickAllowed(type: string, source: JsonObject): JsonObject {
  const picked: JsonObject = {};
  for (const key of allowedKeys(type)) {
    if (source[key] !== undefined && source[key] !== null) {
      picked[key] = source[key];
    }
  }
  return picked;
}

/** A flat legacy body as the producer's migration upgrades it. */
function upgradeLegacy(type: string, legacy: JsonObject): BlockBody {
  const text = typeof legacy.text === "string" ? legacy.text : "";
  const properties = pickAllowed(type, legacy);
  if (type === "page_reference" && typeof legacy.linkedPageId === "string") {
    properties.pageId = legacy.linkedPageId;
  }
  if (type === "board_reference" && typeof legacy.linkedBoardId === "string") {
    properties.boardId = legacy.linkedBoardId;
  }
  if (!TEXT_LIKE.has(type)) return { content: {}, properties, text: "" };
  const marks: Run["marks"] = [];
  if (type !== "code") {
    for (const flag of ["bold", "italic", "underline", "strike"]) {
      if (legacy[flag] === true) marks.push({ type: flag });
    }
    const color = String(legacy.textColor ?? "");
    if (LEGACY_COLORS.has(color)) marks.push({ type: "color", token: color });
    else if (color === "destructive") {
      marks.push({ type: "color", token: "danger" });
    }
    const highlight = String(legacy.highlight ?? "none");
    if (highlight !== "none")
      marks.push({ type: "highlight", token: highlight });
  }
  return {
    content: { v: 1, runs: text ? [{ text, marks }] : [] },
    properties,
    text,
  };
}

/** The body of a stored block, whichever shape its text has. */
export function readBlockBody(block: {
  type: string;
  properties: string;
}): BlockBody {
  const stored = parse(block.properties);
  if (typeof stored.text === "string" || !Array.isArray(stored.runs)) {
    return upgradeLegacy(block.type, stored);
  }
  const { v: _v, runs, ...rest } = stored;
  void _v;
  return {
    content: TEXT_LIKE.has(block.type) ? { v: 1, runs } : {},
    properties: rest,
    text: runsText(runs),
  };
}

/** The text a block stores for a body written through the API. */
export function writeBlockBody(body: {
  content: JsonObject;
  properties: JsonObject;
}): string {
  return JSON.stringify({
    ...(Array.isArray(body.content.runs)
      ? { v: 1, runs: body.content.runs }
      : {}),
    ...body.properties,
  });
}

export class BlockValidationError extends Error {}

/** The producer's 400 for a body that is not an object or holds unknown keys. */
export function validateBody(
  type: string,
  content: unknown,
  properties: unknown,
): void {
  for (const [name, value] of [
    ["content", content],
    ["properties", properties],
  ] as const) {
    if (
      value !== undefined &&
      value !== null &&
      (typeof value !== "object" || Array.isArray(value))
    ) {
      throw new BlockValidationError(`${name} must be a JSON object.`);
    }
  }
  const body = content as JsonObject | undefined | null;
  if (body && Object.keys(body).length > 0) {
    if (!TEXT_LIKE.has(type)) {
      throw new BlockValidationError(`A ${type} block has no text content.`);
    }
    if (body.v !== 1 || !Array.isArray(body.runs)) {
      throw new BlockValidationError("content must be rich text v1.");
    }
    for (const key of Object.keys(body)) {
      if (key !== "v" && key !== "runs") {
        throw new BlockValidationError(`Unknown content key '${key}'.`);
      }
    }
  }
  const allowed = new Set(allowedKeys(type));
  for (const key of Object.keys((properties as JsonObject | null) ?? {})) {
    if (!allowed.has(key)) {
      throw new BlockValidationError(
        `Unknown property '${key}' for a ${type} block.`,
      );
    }
  }
}

/** A retype keeps the rich text and the properties the new type has. */
export function retypeBody(
  from: string,
  to: string,
  body: BlockBody,
): { content: JsonObject; properties: JsonObject } {
  if (!TEXT_LIKE.has(from) || !TEXT_LIKE.has(to)) {
    return { content: {}, properties: {} };
  }
  return {
    content: body.content,
    properties: pickAllowed(to, body.properties),
  };
}
