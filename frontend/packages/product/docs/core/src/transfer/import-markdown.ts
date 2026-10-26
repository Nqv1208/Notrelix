import type { BlockProperties, BlockType, TableCell } from "../types/block";
import {
  isSafeHref,
  normalizeRuns,
  richText,
  type Mark,
  type Run,
} from "../types/rich-text";
import type { ImportedBlock, ImportedPage } from "./import-types";

const PUNCTUATION = /[!-/:-@[-`{-~]/;

interface Context {
  imagesDropped: number;
}

/** Index of the `)` that closes a link target opened just before `from`. */
function closingParen(text: string, from: number): number {
  let depth = 1;
  for (let i = from; i < text.length; i += 1) {
    const char = text[i];
    if (char === "\\") i += 1;
    else if (char === "(") depth += 1;
    else if (char === ")") {
      depth -= 1;
      if (depth === 0) return i;
    }
  }
  return -1;
}

/** Index of the `]` that closes a label opened just before `from`. */
function closingBracket(text: string, from: number): number {
  let depth = 1;
  for (let i = from; i < text.length; i += 1) {
    const char = text[i];
    if (char === "\\") i += 1;
    else if (char === "`") {
      const end = text.indexOf("`", i + 1);
      if (end > 0) i = end;
    } else if (char === "[") depth += 1;
    else if (char === "]") {
      depth -= 1;
      if (depth === 0) return i;
    }
  }
  return -1;
}

/** The closing delimiter of an emphasis run opened at `from`, or -1. */
function findCloser(text: string, delimiter: string, from: number): number {
  for (let i = from; i < text.length; i += 1) {
    const char = text[i];
    if (char === "\\") {
      i += 1;
      continue;
    }
    if (char === "`") {
      const end = text.indexOf("`", i + 1);
      if (end > 0) i = end;
      continue;
    }
    if (!text.startsWith(delimiter, i)) continue;
    // A single `*` or `_` does not close on the first half of a doubled one.
    if (delimiter.length === 1 && text[i + 1] === delimiter) {
      i += 1;
      continue;
    }
    const before = text[i - 1];
    if (i > from && before !== undefined && !/\s/.test(before)) return i;
  }
  return -1;
}

function withMark(runs: Run[], mark: Mark): Run[] {
  return runs.map((run) => ({ text: run.text, marks: [...run.marks, mark] }));
}

/** Inline Markdown as rich-text runs (links, code, bold, italic, strike). */
function parseInline(text: string, context: Context): Run[] {
  const runs: Run[] = [];
  let literal = "";
  const flush = () => {
    if (literal) runs.push({ text: literal, marks: [] });
    literal = "";
  };
  let i = 0;
  while (i < text.length) {
    const char = text[i] as string;
    if (char === "\\") {
      const next = text[i + 1];
      if (next === "\n") {
        literal += "\n";
        i += 2;
      } else if (next !== undefined && PUNCTUATION.test(next)) {
        literal += next;
        i += 2;
      } else {
        literal += char;
        i += 1;
      }
      continue;
    }
    if (char === "`") {
      let ticks = 1;
      while (text[i + ticks] === "`") ticks += 1;
      const fence = "`".repeat(ticks);
      const end = text.indexOf(fence, i + ticks);
      if (end > 0) {
        let code = text.slice(i + ticks, end).replace(/\n/g, " ");
        if (code.length > 2 && code.startsWith(" ") && code.endsWith(" ")) {
          code = code.slice(1, -1);
        }
        flush();
        runs.push({ text: code, marks: [{ type: "code" }] });
        i = end + ticks;
        continue;
      }
      literal += fence;
      i += ticks;
      continue;
    }
    if (char === "!" && text[i + 1] === "[") {
      const close = closingBracket(text, i + 2);
      if (close > 0 && text[close + 1] === "(") {
        const end = closingParen(text, close + 2);
        if (end > 0) {
          context.imagesDropped += 1;
          i = end + 1;
          continue;
        }
      }
    }
    if (char === "[") {
      const close = closingBracket(text, i + 1);
      if (close > 0 && text[close + 1] === "(") {
        const end = closingParen(text, close + 2);
        if (end > 0) {
          const target = text
            .slice(close + 2, end)
            .trim()
            .replace(/^<(.*)>$/, "$1")
            .replace(/\s+"[^"]*"$/, "");
          const inner = parseInline(text.slice(i + 1, close), context);
          flush();
          if (isSafeHref(target)) {
            runs.push(...withMark(inner, { type: "link", href: target }));
          } else runs.push(...inner);
          i = end + 1;
          continue;
        }
      }
    }
    if (char === "<") {
      const end = text.indexOf(">", i + 1);
      const target = end > 0 ? text.slice(i + 1, end) : "";
      if (end > 0 && /^(https?:\/\/|mailto:)[^\s<>]+$/i.test(target)) {
        flush();
        runs.push({
          text: target,
          marks: isSafeHref(target) ? [{ type: "link", href: target }] : [],
        });
        i = end + 1;
        continue;
      }
    }
    const delimiter =
      text.startsWith("**", i) || text.startsWith("__", i)
        ? text.slice(i, i + 2)
        : text.startsWith("~~", i)
          ? "~~"
          : char === "*" || char === "_"
            ? char
            : null;
    if (delimiter) {
      const after = text[i + delimiter.length];
      const before = text[i - 1];
      const intraword =
        delimiter.startsWith("_") && before !== undefined && /\w/.test(before);
      if (after !== undefined && !/\s/.test(after) && !intraword) {
        const end = findCloser(text, delimiter, i + delimiter.length);
        const next = end > 0 ? text[end + delimiter.length] : undefined;
        const wordEnd =
          delimiter.startsWith("_") && next !== undefined && /\w/.test(next);
        if (end > 0 && !wordEnd) {
          const inner = parseInline(
            text.slice(i + delimiter.length, end),
            context,
          );
          const type =
            delimiter === "~~"
              ? "strike"
              : delimiter.length === 2
                ? "bold"
                : "italic";
          flush();
          runs.push(...withMark(inner, { type }));
          i = end + delimiter.length;
          continue;
        }
      }
    }
    literal += char;
    i += 1;
  }
  flush();
  return runs;
}

/** Paragraph text: soft breaks read as a space, hard breaks (two spaces or `\`) as a new line. */
function paragraphRuns(lines: readonly string[], context: Context): Run[] {
  const joined = lines
    .map((line, index) => {
      const last = index === lines.length - 1;
      if (last) return line.trim();
      if (/ {2,}$/.test(line)) return `${line.trim()}\\\n`;
      return /\\$/.test(line) ? `${line.trim()}\n` : `${line.trim()} `;
    })
    .join("");
  return parseInline(joined, context);
}

const FENCE = /^\s{0,3}(`{3,}|~{3,})\s*([^`\s]*)[^`]*$/;
const HEADING = /^\s{0,3}(#{1,6})(?:\s+(.*?))?\s*#*\s*$/;
const RULE = /^\s{0,3}([-*_])(?:\s*\1){2,}\s*$/;
const QUOTE = /^\s{0,3}>\s?(.*)$/;
const BULLET = /^(\s*)[-*+]\s+(.*)$/;
const ORDERED = /^(\s*)\d{1,9}[.)]\s+(.*)$/;
const TABLE_RULE = /^\s*\|?\s*:?-+:?\s*(\|\s*:?-+:?\s*)*\|?\s*$/;
const SETEXT = /^\s{0,3}(=+|-+)\s*$/;

function startsBlock(line: string): boolean {
  return (
    FENCE.test(line) ||
    HEADING.test(line) ||
    RULE.test(line) ||
    QUOTE.test(line) ||
    BULLET.test(line) ||
    ORDERED.test(line)
  );
}

function splitRow(line: string): string[] {
  let row = line.trim();
  if (row.startsWith("|")) row = row.slice(1);
  if (row.endsWith("|") && !row.endsWith("\\|")) row = row.slice(0, -1);
  const cells: string[] = [];
  let current = "";
  for (let i = 0; i < row.length; i += 1) {
    const char = row[i] as string;
    if (char === "\\" && row[i + 1] === "|") {
      current += "|";
      i += 1;
    } else if (char === "|") {
      cells.push(current);
      current = "";
    } else current += char;
  }
  cells.push(current);
  return cells.map((entry) => entry.trim());
}

function block(
  type: BlockType,
  runs: readonly Run[],
  properties: BlockProperties = {},
): ImportedBlock {
  return { type, content: richText(runs), properties };
}

/**
 * Reads Markdown (CommonMark with task lists, strikethrough and tables) into
 * blocks. A first top-level `# heading` becomes the title. Nested list items
 * are flattened to top-level items, images are dropped (and counted), and raw
 * HTML is kept as plain text, never interpreted.
 */
export function parseMarkdown(source: string): ImportedPage {
  const context: Context = { imagesDropped: 0 };
  const lines = source
    .replace(/\r\n?/g, "\n")
    .replace(/^\uFEFF/, "")
    .split("\n");
  const blocks: ImportedBlock[] = [];
  let title: string | null = null;
  let i = 0;
  while (i < lines.length) {
    const line = lines[i] as string;
    if (line.trim() === "") {
      i += 1;
      continue;
    }

    const fence = FENCE.exec(line);
    if (fence) {
      const marker = (fence[1] as string)[0] as string;
      const size = (fence[1] as string).length;
      const language = fence[2] ?? "";
      const code: string[] = [];
      i += 1;
      while (i < lines.length) {
        const closing = new RegExp(`^\\s{0,3}${marker}{${size},}\\s*$`);
        if (closing.test(lines[i] as string)) break;
        code.push(lines[i] as string);
        i += 1;
      }
      i += 1;
      blocks.push(
        block(
          "code",
          code.length ? [{ text: code.join("\n"), marks: [] }] : [],
          language ? { language } : {},
        ),
      );
      continue;
    }

    const heading = HEADING.exec(line);
    if (heading) {
      const level = (heading[1] as string).length;
      const runs = parseInline(heading[2] ?? "", context);
      if (level === 1 && title === null && blocks.length === 0) {
        title =
          runs
            .map((run) => run.text)
            .join("")
            .trim() || null;
        if (title !== null) {
          i += 1;
          continue;
        }
      }
      const type: BlockType =
        level === 1 ? "heading_1" : level === 2 ? "heading_2" : "heading_3";
      blocks.push(block(type, runs));
      i += 1;
      continue;
    }

    if (RULE.test(line)) {
      blocks.push(block("divider", []));
      i += 1;
      continue;
    }

    if (line.includes("|") && TABLE_RULE.test(lines[i + 1] ?? "")) {
      const header = splitRow(line);
      const rows: string[][] = [header];
      i += 2;
      while (
        i < lines.length &&
        (lines[i] as string).trim() !== "" &&
        (lines[i] as string).includes("|")
      ) {
        rows.push(splitRow(lines[i] as string));
        i += 1;
      }
      const width = Math.max(...rows.map((row) => row.length));
      const cells: TableCell[][] = rows.map((row) =>
        Array.from({ length: width }, (_, index) => ({
          runs: parseInline(row[index] ?? "", context),
        })),
      );
      blocks.push(
        block("table", [], {
          table: {
            rows: cells,
            headerRow: true,
            colWidths: Array.from({ length: width }, () => null),
          },
        }),
      );
      continue;
    }

    if (QUOTE.test(line)) {
      const quoted: string[] = [];
      while (i < lines.length && QUOTE.test(lines[i] as string)) {
        quoted.push(
          (QUOTE.exec(lines[i] as string) as RegExpExecArray)[1] ?? "",
        );
        i += 1;
      }
      blocks.push(block("quote", paragraphRuns(quoted, context)));
      continue;
    }

    const bullet = BULLET.exec(line);
    const ordered = bullet ? null : ORDERED.exec(line);
    if (bullet || ordered) {
      const match = (bullet ?? ordered) as RegExpExecArray;
      let text = match[2] ?? "";
      i += 1;
      // Indented lines continue the item.
      while (
        i < lines.length &&
        /^\s{2,}\S/.test(lines[i] as string) &&
        !BULLET.test(lines[i] as string) &&
        !ORDERED.test(lines[i] as string)
      ) {
        text += ` ${(lines[i] as string).trim()}`;
        i += 1;
      }
      if (ordered) {
        blocks.push(block("numbered_list", parseInline(text, context)));
        continue;
      }
      const task = /^\[([ xX])\]\s+(.*)$/.exec(text);
      if (task) {
        blocks.push(
          block("todo", parseInline(task[2] ?? "", context), {
            checked: task[1] !== " ",
          }),
        );
      } else blocks.push(block("bulleted_list", parseInline(text, context)));
      continue;
    }

    const paragraph: string[] = [line];
    i += 1;
    while (
      i < lines.length &&
      (lines[i] as string).trim() !== "" &&
      !startsBlock(lines[i] as string)
    ) {
      const setext = SETEXT.exec(lines[i] as string);
      if (setext) break;
      paragraph.push(lines[i] as string);
      i += 1;
    }
    const underline = SETEXT.exec(lines[i] ?? "");
    if (underline && i < lines.length) {
      blocks.push(
        block(
          (underline[1] as string).startsWith("=") ? "heading_1" : "heading_2",
          paragraphRuns(paragraph, context),
        ),
      );
      i += 1;
      continue;
    }
    blocks.push(block("paragraph", paragraphRuns(paragraph, context)));
  }
  return {
    title,
    blocks: blocks.map((entry) => ({
      ...entry,
      content: richText(normalizeRuns(entry.content.runs)),
    })),
    imagesDropped: context.imagesDropped,
  };
}
