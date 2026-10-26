/**
 * Rich text v1: how the authored text of a block is stored (the wire contract
 * shared with the Documents producer). A block's `content` is a list of runs,
 * each a piece of text with the marks that apply to it.
 */

export const COLOR_TOKENS = [
  "default",
  "muted",
  "primary",
  "accent",
  "success",
  "warning",
  "danger",
] as const;
export type ColorToken = (typeof COLOR_TOKENS)[number];

export const HIGHLIGHT_TOKENS = [
  "none",
  "muted",
  "accent",
  "primary",
  "success",
  "warning",
  "danger",
] as const;
export type HighlightToken = (typeof HIGHLIGHT_TOKENS)[number];

export type SimpleMarkType =
  | "bold"
  | "italic"
  | "underline"
  | "strike"
  | "code"
  | "superscript"
  | "subscript";

export type Mark =
  | { type: SimpleMarkType }
  | { type: "link"; href: string }
  | { type: "color"; token: ColorToken }
  | { type: "highlight"; token: HighlightToken }
  | { type: "mention"; userId: string }
  | { type: "pageRef"; pageId: string };

export type MarkType = Mark["type"];

export interface Run {
  text: string;
  marks: Mark[];
}

export interface RichTextV1 {
  v: 1;
  runs: Run[];
}

export const MAX_TEXT_LENGTH = 20_000;
export const MAX_HREF_LENGTH = 2048;

const SAFE_PROTOCOLS: ReadonlySet<string> = new Set([
  "http:",
  "https:",
  "mailto:",
]);

/** Links may only point at http, https or mailto addresses. */
export function isSafeHref(href: string): boolean {
  const value = href.trim();
  if (!value || value.length > MAX_HREF_LENGTH) return false;
  try {
    return SAFE_PROTOCOLS.has(new URL(value).protocol);
  } catch {
    return false;
  }
}

const MARK_ORDER: readonly MarkType[] = [
  "bold",
  "code",
  "color",
  "highlight",
  "italic",
  "link",
  "mention",
  "pageRef",
  "strike",
  "subscript",
  "superscript",
  "underline",
];

function isMark(value: unknown): value is Mark {
  if (!value || typeof value !== "object") return false;
  const mark = value as Record<string, unknown>;
  switch (mark.type) {
    case "bold":
    case "italic":
    case "underline":
    case "strike":
    case "code":
    case "superscript":
    case "subscript":
      return true;
    case "link":
      return typeof mark.href === "string" && isSafeHref(mark.href);
    case "color":
      return COLOR_TOKENS.includes(mark.token as ColorToken);
    case "highlight":
      return HIGHLIGHT_TOKENS.includes(mark.token as HighlightToken);
    case "mention":
      return typeof mark.userId === "string";
    case "pageRef":
      return typeof mark.pageId === "string";
    default:
      return false;
  }
}

/**
 * The canonical mark set: unknown or unsafe marks dropped, one mark per type
 * (the last wins), sorted by type, `code` keeps only a link beside it and
 * `superscript` excludes `subscript`.
 */
export function normalizeMarks(marks: readonly Mark[]): Mark[] {
  const byType = new Map<MarkType, Mark>();
  for (const mark of marks) if (isMark(mark)) byType.set(mark.type, mark);
  let result = [...byType.values()];
  if (byType.has("code")) {
    result = result.filter((m) => m.type === "code" || m.type === "link");
  }
  // Raised and lowered text exclude each other; raised wins.
  if (byType.has("superscript")) {
    result = result.filter((m) => m.type !== "subscript");
  }
  return result.sort(
    (a, b) => MARK_ORDER.indexOf(a.type) - MARK_ORDER.indexOf(b.type),
  );
}

function markKey(marks: readonly Mark[]): string {
  return JSON.stringify(marks);
}

/**
 * The canonical runs: marks normalised, empty runs dropped and neighbours with
 * the same marks merged. The producer normalises the same way.
 */
export function normalizeRuns(runs: readonly Run[]): Run[] {
  const out: Run[] = [];
  let lastKey = "";
  for (const run of runs) {
    if (!run.text) continue;
    const marks = normalizeMarks(run.marks ?? []);
    const key = markKey(marks);
    const last = out[out.length - 1];
    if (last && key === lastKey) last.text += run.text;
    else out.push({ text: run.text, marks });
    lastKey = key;
  }
  return out;
}

export function richText(runs: readonly Run[] = []): RichTextV1 {
  return { v: 1, runs: normalizeRuns(runs) };
}

export function runsFromPlainText(text: string): Run[] {
  return text ? [{ text, marks: [] }] : [];
}

export function plainTextOfRuns(runs: readonly Run[]): string {
  return runs.map((run) => run.text).join("");
}

export function plainTextOf(content: RichTextV1 | undefined | null): string {
  return content ? plainTextOfRuns(content.runs) : "";
}

export function isRichTextV1(value: unknown): value is RichTextV1 {
  return (
    !!value &&
    typeof value === "object" &&
    (value as { v?: unknown }).v === 1 &&
    Array.isArray((value as { runs?: unknown }).runs)
  );
}

/** Reads a stored value as rich text, dropping anything the contract forbids. */
export function parseRichText(value: unknown): RichTextV1 | null {
  if (!isRichTextV1(value)) return null;
  const runs: Run[] = [];
  for (const entry of value.runs as unknown[]) {
    if (!entry || typeof entry !== "object") continue;
    const run = entry as { text?: unknown; marks?: unknown };
    if (typeof run.text !== "string") continue;
    runs.push({
      text: run.text,
      marks: Array.isArray(run.marks) ? (run.marks as Mark[]) : [],
    });
  }
  return richText(runs);
}

export function richTextEquals(a: RichTextV1, b: RichTextV1): boolean {
  return (
    JSON.stringify(normalizeRuns(a.runs)) ===
    JSON.stringify(normalizeRuns(b.runs))
  );
}
