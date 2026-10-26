import type { BlockProperties, BlockType, TableData } from "../types/block";
import {
  MAX_TEXT_LENGTH,
  normalizeRuns,
  richText,
  type Run,
} from "../types/rich-text";
import {
  MAX_IMPORT_BLOCKS,
  type ImportedBlock,
  type ImportedPage,
} from "./import-types";

const TEXT_TYPES: ReadonlySet<BlockType> = new Set<BlockType>([
  "paragraph",
  "heading_1",
  "heading_2",
  "heading_3",
  "bulleted_list",
  "numbered_list",
  "todo",
  "quote",
  "code",
]);
const IMPORTABLE: ReadonlySet<BlockType> = new Set<BlockType>([
  ...TEXT_TYPES,
  "divider",
  "table",
]);

/** The runs cut into pieces of at most `limit` characters, marks kept. */
function chunkRuns(runs: readonly Run[], limit: number): Run[][] {
  const chunks: Run[][] = [];
  let current: Run[] = [];
  let size = 0;
  for (const run of runs) {
    let text = run.text;
    while (text.length > 0) {
      const take = text.slice(0, limit - size);
      current.push({ text: take, marks: run.marks });
      size += take.length;
      text = text.slice(take.length);
      if (size >= limit) {
        chunks.push(current);
        current = [];
        size = 0;
      }
    }
  }
  if (current.length) chunks.push(current);
  return chunks;
}

function cleanTable(table: TableData | undefined): TableData | null {
  if (!table || table.rows.length === 0) return null;
  const rows = table.rows.map((row) =>
    row.map((entry) => ({
      runs: normalizeRuns(entry.runs).map((run) => ({
        text: run.text.slice(0, MAX_TEXT_LENGTH),
        marks: run.marks,
      })),
    })),
  );
  const width = Math.max(...rows.map((row) => row.length));
  if (width === 0) return null;
  return {
    rows: rows.map((row) => [
      ...row,
      ...Array.from({ length: width - row.length }, () => ({ runs: [] })),
    ]),
    headerRow: table.headerRow,
    colWidths: Array.from({ length: width }, () => null),
  };
}

/**
 * Holds imported blocks to the rich text v1 contract: only importable types,
 * marks normalised (unsafe links dropped), empty text blocks removed and text
 * longer than a block may hold continued in following paragraphs. Returns
 * `null` when the file has more blocks than a page takes.
 */
export function sanitizeImport(page: ImportedPage): ImportedPage | null {
  const blocks: ImportedBlock[] = [];
  for (const entry of page.blocks) {
    if (!IMPORTABLE.has(entry.type)) continue;
    if (entry.type === "divider") {
      blocks.push({ type: "divider", content: richText(), properties: {} });
      continue;
    }
    if (entry.type === "table") {
      const table = cleanTable(entry.properties.table);
      if (table) {
        blocks.push({
          type: "table",
          content: richText(),
          properties: { table },
        });
      }
      continue;
    }
    const runs = normalizeRuns(entry.content.runs);
    if (runs.length === 0) continue;
    const properties: BlockProperties = {};
    if (entry.type === "todo") properties.checked = !!entry.properties.checked;
    if (entry.type === "code" && entry.properties.language) {
      properties.language = entry.properties.language.slice(0, 50);
    }
    const [first, ...rest] = chunkRuns(runs, MAX_TEXT_LENGTH);
    blocks.push({
      type: entry.type,
      content: richText(first ?? []),
      properties,
    });
    for (const chunk of rest) {
      blocks.push({
        type: entry.type === "code" ? "code" : "paragraph",
        content: richText(chunk),
        properties: {},
      });
    }
  }
  if (blocks.length > MAX_IMPORT_BLOCKS) return null;
  return { ...page, blocks };
}
