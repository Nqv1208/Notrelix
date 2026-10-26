import type { JSONContent } from "@tiptap/core";
import {
  HIGHLIGHT_TOKENS,
  TABLE_MAX_COL_WIDTH,
  TABLE_MIN_COL_WIDTH,
  emptyTable,
  normalizeRuns,
  plainTextOfRuns,
  richText,
  type BlockDraft,
  type BlockProperties,
  type BlockType,
  type Mark,
  type Run,
  type TableData,
} from "@notrelix/docs-core";

/**
 * Maps the page's blocks to the editor document and back. Each top-level
 * node is one block: its `blockId` attribute is the block id, its type decides
 * the node, the inline content is the block's runs. Properties the editor
 * does not edit travel in the node's `rest` attribute, so a save never drops
 * them. Pure data in, pure data out: no editor instance is needed.
 */

const TASK_TOKEN = /\[\[task:([^\]\s]+)\]\]/g;

/** Node name and the properties the node edits itself, per block type. */
const NODE_OF: Readonly<Record<BlockType, string>> = {
  paragraph: "paragraph",
  heading_1: "heading",
  heading_2: "heading",
  heading_3: "heading",
  bulleted_list: "bulletedItem",
  numbered_list: "numberedItem",
  todo: "todoItem",
  quote: "quote",
  callout: "callout",
  code: "codeBlock",
  toggle: "toggle",
  divider: "horizontalRule",
  board_reference: "boardReference",
  page_reference: "pageReference",
  image: "unsupportedBlock",
  table: "table",
};

const OWN_KEYS: Readonly<Partial<Record<BlockType, readonly string[]>>> = {
  todo: ["checked"],
  code: ["language"],
  callout: ["icon", "tone"],
  toggle: ["collapsed"],
  board_reference: ["linkedBoardId", "title", "caption", "items"],
  page_reference: ["linkedPageId"],
};

/** Types whose text takes alignment. */
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

const ATOM_TYPES: ReadonlySet<BlockType> = new Set<BlockType>([
  "divider",
  "board_reference",
  "page_reference",
  "image",
]);

export function newBlockId(): string {
  return globalThis.crypto.randomUUID();
}

function markToJson(mark: Mark): { type: string; attrs?: object } {
  switch (mark.type) {
    case "link":
      return { type: "link", attrs: { href: mark.href } };
    case "color":
      return { type: "colorToken", attrs: { token: mark.token } };
    case "highlight":
      return { type: "highlightToken", attrs: { token: mark.token } };
    case "mention":
      return { type: "mentionUser", attrs: { userId: mark.userId } };
    case "pageRef":
      return { type: "pageRef", attrs: { pageId: mark.pageId } };
    default:
      return { type: mark.type };
  }
}

function markFromJson(json: {
  type: string;
  attrs?: Record<string, unknown>;
}): Mark | null {
  const attrs = json.attrs ?? {};
  switch (json.type) {
    case "bold":
    case "italic":
    case "underline":
    case "strike":
    case "code":
      return { type: json.type };
    case "link":
      return typeof attrs.href === "string"
        ? { type: "link", href: attrs.href }
        : null;
    case "colorToken":
      return { type: "color", token: attrs.token as never };
    case "highlightToken":
      return { type: "highlight", token: attrs.token as never };
    case "mentionUser":
      return { type: "mention", userId: String(attrs.userId) };
    case "pageRef":
      return { type: "pageRef", pageId: String(attrs.pageId) };
    default:
      return null;
  }
}

/** Runs as inline editor nodes: hard breaks for `\n`, task tokens as chips. */
export function runsToInline(runs: readonly Run[]): JSONContent[] {
  const out: JSONContent[] = [];
  for (const run of runs) {
    const marks = run.marks.map(markToJson);
    const withMarks = (node: JSONContent): JSONContent =>
      marks.length ? { ...node, marks } : node;
    run.text.split("\n").forEach((line, lineIndex) => {
      if (lineIndex > 0) out.push({ type: "hardBreak" });
      let cursor = 0;
      for (const match of line.matchAll(TASK_TOKEN)) {
        const at = match.index ?? 0;
        if (at > cursor) {
          out.push(withMarks({ type: "text", text: line.slice(cursor, at) }));
        }
        out.push({ type: "taskRef", attrs: { taskId: match[1] } });
        cursor = at + match[0].length;
      }
      if (cursor < line.length) {
        out.push(withMarks({ type: "text", text: line.slice(cursor) }));
      }
    });
  }
  return out;
}

/** Inline editor nodes back to normalised runs. */
export function inlineToRuns(content: readonly JSONContent[] = []): Run[] {
  const runs: Run[] = [];
  for (const node of content) {
    if (node.type === "hardBreak") {
      runs.push({ text: "\n", marks: [] });
    } else if (node.type === "taskRef") {
      runs.push({ text: `[[task:${String(node.attrs?.taskId)}]]`, marks: [] });
    } else if (node.type === "text" && node.text) {
      const marks = (node.marks ?? [])
        .map((m) => markFromJson(m as never))
        .filter((m): m is Mark => m !== null);
      runs.push({ text: node.text, marks });
    }
  }
  return normalizeRuns(runs);
}

function runsToCell(runs: readonly Run[]): JSONContent {
  const content = runsToInline(runs);
  return {
    type: "paragraph",
    ...(content.length ? { content } : {}),
  };
}

/** A table block as the editor's table: header cells on the first row when flagged. */
export function tableToNode(table: TableData): JSONContent[] {
  const columns = Math.max(1, ...table.rows.map((row) => row.length));
  const source = table.rows.length ? table.rows : emptyTable(1, 1).rows;
  return source.map((row, rowIndex) => ({
    type: "tableRow",
    content: Array.from({ length: columns }, (_, column) => {
      const cell = row[column];
      const width = table.colWidths[column];
      return {
        type: table.headerRow && rowIndex === 0 ? "tableHeader" : "tableCell",
        attrs: {
          colwidth: typeof width === "number" ? [width] : null,
          bg: cell?.bg ?? null,
        },
        content: [runsToCell(cell?.runs ?? [])],
      };
    }),
  }));
}

function cellToRuns(cell: JSONContent): Run[] {
  const runs: Run[] = [];
  (cell.content ?? []).forEach((paragraph, index) => {
    if (index > 0) runs.push({ text: "\n", marks: [] });
    runs.push(...inlineToRuns(paragraph.content));
  });
  return normalizeRuns(runs);
}

/** The editor's table back to the block's cells, header flag and column widths. */
export function nodeToTable(node: JSONContent): TableData {
  const rows = node.content ?? [];
  const columns = Math.max(0, ...rows.map((row) => row.content?.length ?? 0));
  const widths: (number | null)[] = Array.from({ length: columns }, () => null);
  const table: TableData = {
    rows: rows.map((row) => {
      const cells = (row.content ?? []).map((cell, column) => {
        const width = (
          cell.attrs?.colwidth as number[] | null | undefined
        )?.[0];
        if (typeof width === "number" && width > 0 && widths[column] === null) {
          widths[column] = Math.min(
            TABLE_MAX_COL_WIDTH,
            Math.max(TABLE_MIN_COL_WIDTH, Math.round(width)),
          );
        }
        const bg = HIGHLIGHT_TOKENS.find((token) => token === cell.attrs?.bg);
        return {
          runs: cellToRuns(cell),
          ...(bg && bg !== "none" ? { bg } : {}),
        };
      });
      while (cells.length < columns) cells.push({ runs: [] });
      return cells;
    }),
    headerRow: rows[0]?.content?.[0]?.type === "tableHeader",
    colWidths: widths,
  };
  return table;
}

export function draftToNode(draft: BlockDraft): JSONContent {
  const props = draft.properties;
  const own = new Set(OWN_KEYS[draft.type] ?? []);
  const rest: Record<string, unknown> = {};
  for (const [key, value] of Object.entries(props)) {
    if (key === "align" && ALIGNABLE.has(draft.type)) continue;
    if (key === "table" && draft.type === "table") continue;
    if (!own.has(key) && value !== undefined) rest[key] = value;
  }
  const attrs: Record<string, unknown> = {
    blockId: draft.id,
    rest: Object.keys(rest).length ? rest : null,
  };
  if (ALIGNABLE.has(draft.type)) attrs.align = props.align ?? null;
  for (const key of own) attrs[key] = (props as Record<string, unknown>)[key];

  const name = NODE_OF[draft.type];
  if (draft.type.startsWith("heading_")) {
    attrs.level = Number(draft.type.slice(-1));
  }
  if (name === "unsupportedBlock") {
    attrs.raw = JSON.stringify({
      type: draft.type,
      content: draft.content,
      properties: draft.properties,
    });
    return { type: name, attrs };
  }
  if (ATOM_TYPES.has(draft.type)) return { type: name, attrs };
  if (draft.type === "table") {
    return {
      type: name,
      attrs,
      content: tableToNode(props.table ?? emptyTable(1, 1)),
    };
  }
  if (draft.type === "code") {
    const text = plainTextOfRuns(draft.content.runs);
    return {
      type: name,
      attrs,
      ...(text ? { content: [{ type: "text", text }] } : {}),
    };
  }
  const content = runsToInline(draft.content.runs);
  return { type: name, attrs, ...(content.length ? { content } : {}) };
}

/** The editor document for a page's blocks (never empty: one paragraph). */
export function draftsToDoc(drafts: readonly BlockDraft[]): JSONContent {
  const content = drafts.map(draftToNode);
  return {
    type: "doc",
    content: content.length
      ? content
      : [{ type: "paragraph", attrs: { blockId: newBlockId() } }],
  };
}

function blockTypeOf(node: JSONContent): BlockType {
  const attrs = node.attrs ?? {};
  switch (node.type) {
    case "heading":
      return `heading_${Math.min(3, Math.max(1, Number(attrs.level) || 1))}` as BlockType;
    case "bulletedItem":
      return "bulleted_list";
    case "numberedItem":
      return "numbered_list";
    case "todoItem":
      return "todo";
    case "quote":
      return "quote";
    case "callout":
      return "callout";
    case "codeBlock":
      return "code";
    case "toggle":
      return "toggle";
    case "horizontalRule":
      return "divider";
    case "boardReference":
      return "board_reference";
    case "pageReference":
      return "page_reference";
    case "table":
      return "table";
    default:
      return "paragraph";
  }
}

export function nodeToDraft(node: JSONContent): BlockDraft {
  const attrs = (node.attrs ?? {}) as Record<string, unknown>;
  const id = typeof attrs.blockId === "string" ? attrs.blockId : newBlockId();
  if (node.type === "unsupportedBlock") {
    const raw = JSON.parse(String(attrs.raw)) as {
      type: BlockType;
      content: BlockDraft["content"];
      properties: BlockProperties;
    };
    return {
      id,
      type: raw.type,
      content: raw.content,
      properties: raw.properties,
    };
  }
  const type = blockTypeOf(node);
  const properties: Record<string, unknown> = {
    ...((attrs.rest as Record<string, unknown> | null) ?? {}),
  };
  for (const key of OWN_KEYS[type] ?? []) {
    const value = attrs[key];
    if (value !== undefined && value !== null && value !== "") {
      properties[key] = value;
    }
  }
  if (type === "todo") properties.checked = Boolean(attrs.checked);
  if (type === "toggle" && !attrs.collapsed) delete properties.collapsed;
  if (ALIGNABLE.has(type) && attrs.align && attrs.align !== "left") {
    properties.align = attrs.align;
  }
  if (ATOM_TYPES.has(type)) {
    return { id, type, content: richText(), properties };
  }
  if (type === "table") {
    return {
      id,
      type,
      content: richText(),
      properties: { ...properties, table: nodeToTable(node) },
    };
  }
  if (type === "code") {
    const text = (node.content ?? []).map((child) => child.text ?? "").join("");
    return {
      id,
      type,
      content: richText(text ? [{ text, marks: [] }] : []),
      properties,
    };
  }
  return {
    id,
    type,
    content: richText(inlineToRuns(node.content)),
    properties,
  };
}

/** The blocks of an editor document, in order. */
export function docToDrafts(doc: JSONContent): BlockDraft[] {
  return (doc.content ?? []).map(nodeToDraft);
}
