import type { Block } from "../types/block";
import { draftOfBlock } from "../types/block-content";
import {
  isSafeHref,
  normalizeMarks,
  plainTextOfRuns,
  type Run,
} from "../types/rich-text";
import { orderedBlocks } from "./ordered-blocks";

export interface MarkdownExportOptions {
  /** Maps the text of every run before it is written (for example to resolve a legacy token). */
  readonly mapText?: (text: string) => string;
  /** Lines to write for a board reference (the export cannot read boards itself). */
  readonly boardReferenceLines?: (block: Block) => readonly string[];
}

const ESCAPED = /[\\`*_[\]~<>]/g;

function escapeText(text: string): string {
  return text.replace(ESCAPED, (char) => `\\${char}`);
}

function codeSpan(text: string): string {
  const longest = Math.max(
    0,
    ...(text.match(/`+/g) ?? []).map((run) => run.length),
  );
  const fence = "`".repeat(longest + 1);
  const pad = text.startsWith("`") || text.endsWith("`") ? " " : "";
  return `${fence}${pad}${text}${pad}${fence}`;
}

function hrefOf(href: string): string {
  return href.replace(/ /g, "%20").replace(/\(/g, "%28").replace(/\)/g, "%29");
}

function runMarkdown(run: Run, mapText?: (text: string) => string): string {
  const text = mapText ? mapText(run.text) : run.text;
  if (!text) return "";
  const marks = normalizeMarks(run.marks);
  const has = (type: string) => marks.some((mark) => mark.type === type);
  const link = marks.find((mark) => mark.type === "link");
  // Emphasis cannot touch whitespace in Markdown: wrap the trimmed core only.
  const match = /^(\s*)([\s\S]*?)(\s*)$/.exec(text) as RegExpExecArray;
  const lead = match[1] ?? "";
  const core = match[2] ?? "";
  const trail = match[3] ?? "";
  if (!core) return text;
  let inner = has("code")
    ? codeSpan(core)
    : escapeText(core).replace(/\n/g, "  \n");
  if (has("bold")) inner = `**${inner}**`;
  if (has("italic")) inner = `*${inner}*`;
  if (has("strike")) inner = `~~${inner}~~`;
  if (has("superscript")) inner = `<sup>${inner}</sup>`;
  if (has("subscript")) inner = `<sub>${inner}</sub>`;
  if (link && link.type === "link" && isSafeHref(link.href)) {
    inner = `[${inner}](${hrefOf(link.href)})`;
  }
  return `${lead}${inner}${trail}`;
}

function inline(
  runs: readonly Run[],
  mapText?: (text: string) => string,
): string {
  return runs.map((run) => runMarkdown(run, mapText)).join("");
}

/** A block start that would read as other Markdown syntax is escaped. */
function paragraphText(text: string): string {
  return text.replace(/^(\s*)(#{1,6}\s|[-+]\s|\d+[.)]\s)/, "$1\\$2");
}

function cell(text: string): string {
  return text.replace(/\n/g, " ").replace(/[\\|]/g, "\\$&");
}

/**
 * The page as Markdown: the title as `#`, headings 1 to 3 as `#` to `###`,
 * lists, tasks, quotes, code, tables and inline marks (bold, italic, strike,
 * code, links). Images, files and videos are not part of the text and are not
 * written (a caption stays as a line).
 */
export function exportMarkdown(
  title: string,
  blocks: readonly Block[],
  options: MarkdownExportOptions = {},
): string {
  const { mapText } = options;
  const out: string[] = [`# ${escapeText(title.trim())}`, ""];
  const counters = new Map<number, number>();
  let openList: "bullet" | "numbered" | null = null;
  for (const { block, depth } of orderedBlocks(blocks)) {
    const draft = draftOfBlock(block);
    const runs = draft.content.runs;
    const text = inline(runs, mapText);
    const indent = "  ".repeat(depth);
    if (block.type !== "numbered_list") {
      counters.clear();
    }
    const listKind =
      block.type === "bulleted_list" || block.type === "todo"
        ? "bullet"
        : block.type === "numbered_list"
          ? "numbered"
          : null;
    // A list ends with a blank line, or the next paragraph would continue its last item.
    if (openList && openList !== listKind) out.push("");
    openList = listKind;
    switch (block.type) {
      case "heading_1":
        out.push(`# ${text}`, "");
        break;
      case "heading_2":
        out.push(`## ${text}`, "");
        break;
      case "heading_3":
        out.push(`### ${text}`, "");
        break;
      case "bulleted_list":
        out.push(`${indent}- ${text}`);
        break;
      case "numbered_list": {
        const next = (counters.get(depth) ?? 0) + 1;
        counters.set(depth, next);
        for (const key of [...counters.keys()]) {
          if (key > depth) counters.delete(key);
        }
        out.push(`${indent}${next}. ${text}`);
        break;
      }
      case "todo":
        out.push(
          `${indent}- [${block.properties.checked ? "x" : " "}] ${text}`,
        );
        break;
      case "quote":
      case "callout":
        out.push(...text.split("\n").map((line) => `> ${line.trimEnd()}`), "");
        break;
      case "code": {
        const code = plainTextOfRuns(runs);
        const fence = "`".repeat(
          Math.max(3, ...(code.match(/`+/g) ?? []).map((m) => m.length + 1)),
        );
        out.push(`${fence}${block.properties.language ?? ""}`, code, fence, "");
        break;
      }
      case "divider":
        out.push("---", "");
        break;
      case "table": {
        const table = block.properties.table;
        if (!table || table.rows.length === 0) break;
        const rows = table.rows.map((row) =>
          row.map((entry) => cell(inline(entry.runs, mapText))),
        );
        const width = Math.max(...rows.map((row) => row.length));
        const pad = (row: string[]) =>
          Array.from({ length: width }, (_, index) => row[index] ?? "");
        const header = table.headerRow ? pad(rows[0] as string[]) : pad([]);
        const body = table.headerRow ? rows.slice(1) : rows;
        out.push(
          `| ${header.join(" | ")} |`,
          `| ${header.map(() => "---").join(" | ")} |`,
          ...body.map((row) => `| ${pad(row).join(" | ")} |`),
          "",
        );
        break;
      }
      case "board_reference": {
        const lines = options.boardReferenceLines?.(block) ?? [];
        if (lines.length) out.push(...lines, "");
        break;
      }
      case "image":
      case "file": {
        const caption = block.properties.caption?.trim();
        if (caption) out.push(`*${escapeText(caption)}*`, "");
        break;
      }
      default:
        if (text) out.push(paragraphText(text), "");
    }
  }
  return `${out
    .join("\n")
    .replace(/\n{3,}/g, "\n\n")
    .trim()}\n`;
}
