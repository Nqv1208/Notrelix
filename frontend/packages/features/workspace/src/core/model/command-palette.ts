/**
 * Command palette model (Final-Command): how the typed text is read, how a
 * term matches a label and how the matches are highlighted. Pure and
 * framework-neutral; the surface decides what to draw, the host decides which
 * entries exist.
 */

/** The kinds of result the palette lists, in the order a search shows them. */
export const PALETTE_KINDS = [
  "work",
  "doc",
  "board",
  "person",
  "command",
] as const;
export type PaletteKind = (typeof PALETTE_KINDS)[number];

export type PaletteFilter = "all" | PaletteKind;
export const PALETTE_FILTERS: readonly PaletteFilter[] = [
  "all",
  ...PALETTE_KINDS,
];

/**
 * What the typed text asks for: everything ("search"), only commands (`>`),
 * only people (`@`) or only Boards (`#`).
 */
export type PaletteScope = "search" | "command" | "person" | "board";

export interface ParsedPaletteQuery {
  readonly scope: PaletteScope;
  /** The prefix character the member typed ("" for a plain search). */
  readonly prefix: "" | ">" | "@" | "#";
  /** The text after the prefix, trimmed. */
  readonly term: string;
}

const PREFIX_SCOPE: Readonly<Record<string, PaletteScope>> = {
  ">": "command",
  "@": "person",
  "#": "board",
};

/** Reads the typed text: a leading `>`, `@` or `#` narrows the scope. */
export function parsePaletteQuery(raw: string): ParsedPaletteQuery {
  const text = raw.trimStart();
  const first = text.charAt(0);
  const scope = PREFIX_SCOPE[first];
  if (scope) {
    return {
      scope,
      prefix: first as ">" | "@" | "#",
      term: text.slice(1).trim(),
    };
  }
  return { scope: "search", prefix: "", term: text.trim() };
}

/** One UTF-16 unit without diacritics, lower case; `đ` becomes `d`. */
function foldUnit(unit: string): string {
  const folded = unit
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .replace(/đ/gi, "d")
    .toLowerCase();
  // A unit that folds to another length keeps itself, so offsets hold.
  return folded.length === 1 ? folded : unit;
}

/**
 * Folds a text (in NFC form) to a string of the same length, one folded unit
 * per original unit, so a match found in the folded text maps back to the
 * same range of the original.
 */
export function foldKeepingLength(text: string): string {
  let out = "";
  for (let at = 0; at < text.length; at += 1) {
    out += foldUnit(text.charAt(at));
  }
  return out;
}

/** Whether `text` contains `term`, ignoring case and diacritics. */
export function matchesTerm(text: string, term: string): boolean {
  if (!term) return true;
  return foldKeepingLength(text.normalize("NFC")).includes(
    foldKeepingLength(term.normalize("NFC")),
  );
}

export interface HighlightSegment {
  readonly text: string;
  readonly hit: boolean;
}

/**
 * Splits `text` into plain and matching segments for `term` (ignoring case
 * and diacritics). Every occurrence is a hit; an empty term has none.
 */
export function highlightSegments(
  source: string,
  term: string,
): readonly HighlightSegment[] {
  const text = source.normalize("NFC");
  const needle = foldKeepingLength(term.trim().normalize("NFC"));
  if (!needle) return [{ text, hit: false }];
  const haystack = foldKeepingLength(text);
  const segments: HighlightSegment[] = [];
  let from = 0;
  while (from < text.length) {
    const at = haystack.indexOf(needle, from);
    if (at < 0) break;
    if (at > from) segments.push({ text: text.slice(from, at), hit: false });
    segments.push({ text: text.slice(at, at + needle.length), hit: true });
    from = at + needle.length;
  }
  if (from < text.length) segments.push({ text: text.slice(from), hit: false });
  return segments.length > 0 ? segments : [{ text, hit: false }];
}

/**
 * The result counts per filter tab for a list of kinds: "all" is the total
 * and a kind with no result is absent.
 */
export function paletteCounts(
  kinds: readonly PaletteKind[],
): Readonly<Partial<Record<PaletteFilter, number>>> {
  const counts: Partial<Record<PaletteFilter, number>> = {};
  for (const kind of kinds) counts[kind] = (counts[kind] ?? 0) + 1;
  if (kinds.length > 0) counts.all = kinds.length;
  return counts;
}

/** The next filter tab, wrapping; `step` is 1 (Tab) or -1 (Shift+Tab). */
export function nextPaletteFilter(
  current: PaletteFilter,
  step: 1 | -1,
): PaletteFilter {
  const at = PALETTE_FILTERS.indexOf(current);
  const next = (at + step + PALETTE_FILTERS.length) % PALETTE_FILTERS.length;
  return PALETTE_FILTERS[next] ?? "all";
}

/**
 * The index of the option that stays selected after a move: wraps around the
 * list; `-1` for an empty list.
 */
export function moveSelection(
  current: number,
  count: number,
  step: 1 | -1,
): number {
  if (count <= 0) return -1;
  if (current < 0) return step === 1 ? 0 : count - 1;
  return (current + step + count) % count;
}
