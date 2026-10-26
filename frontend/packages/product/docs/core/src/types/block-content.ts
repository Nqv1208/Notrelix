import {
  TEXT_BLOCK_TYPES,
  type Block,
  type BlockProperties,
  type BlockType,
} from "./block";
import {
  normalizeRuns,
  plainTextOf,
  richText,
  runsFromPlainText,
  type ColorToken,
  type HighlightToken,
  type Mark,
  type RichTextV1,
  type Run,
} from "./rich-text";

/** A block as the editor holds it: rich text plus the type's own properties. */
export interface BlockDraft {
  id: string;
  type: BlockType;
  content: RichTextV1;
  /** Everything but the authored text (no `text`, no format flags). */
  properties: BlockProperties;
}

/** Properties the flat legacy shape kept inside the block's text content. */
const LEGACY_KEYS = [
  "text",
  "bold",
  "italic",
  "underline",
  "strike",
  "textColor",
  "highlight",
  "fontFamily",
  "fontSize",
  "mentionIds",
] as const satisfies readonly (keyof BlockProperties)[];

const LEGACY_COLOR: Readonly<Record<string, ColorToken>> = {
  default: "default",
  muted: "muted",
  primary: "primary",
  accent: "accent",
  destructive: "danger",
  success: "success",
  warning: "warning",
  danger: "danger",
};

/**
 * A flat legacy block (`{ text, bold, italic, ..., align, checked }`) as rich
 * text v1: the block-level format flags become marks over the whole text.
 */
export function upgradeLegacyProperties(
  type: BlockType,
  legacy: BlockProperties,
): { content: RichTextV1; properties: BlockProperties } {
  const properties: BlockProperties = { ...legacy };
  for (const key of LEGACY_KEYS) delete properties[key];
  if (!TEXT_BLOCK_TYPES.has(type)) {
    return { content: richText(), properties };
  }
  const marks: Mark[] = [];
  if (type !== "code") {
    if (legacy.bold) marks.push({ type: "bold" });
    if (legacy.italic) marks.push({ type: "italic" });
    if (legacy.underline) marks.push({ type: "underline" });
    if (legacy.strike) marks.push({ type: "strike" });
    const color = legacy.textColor ? LEGACY_COLOR[legacy.textColor] : undefined;
    if (color && color !== "default")
      marks.push({ type: "color", token: color });
    const highlight = legacy.highlight;
    if (highlight && highlight !== "none") {
      marks.push({ type: "highlight", token: highlight as HighlightToken });
    }
  }
  const text = legacy.text ?? "";
  return {
    content: richText(text ? [{ text, marks }] : []),
    properties,
  };
}

/** The draft of a stored block, upgrading a legacy shape when needed. */
export function draftOfBlock(block: Block): BlockDraft {
  if (block.content) {
    const properties: BlockProperties = { ...block.properties };
    for (const key of LEGACY_KEYS) delete properties[key];
    return {
      id: block.id,
      type: block.type,
      content: richText(block.content.runs),
      properties,
    };
  }
  return {
    id: block.id,
    type: block.type,
    ...upgradeLegacyProperties(block.type, block.properties),
  };
}

/** The plain text of a stored block, whichever shape it has. */
export function blockPlainText(block: Block): string {
  return block.content
    ? plainTextOf(block.content)
    : (block.properties.text ?? "");
}

export function plainDraftContent(text: string): RichTextV1 {
  return richText(runsFromPlainText(text));
}

function stable(value: unknown): string {
  return JSON.stringify(value, (_key, v: unknown) => {
    if (v && typeof v === "object" && !Array.isArray(v)) {
      return Object.fromEntries(
        Object.entries(v as Record<string, unknown>)
          .filter(([, entry]) => entry !== undefined)
          .sort(([a], [b]) => (a < b ? -1 : 1)),
      );
    }
    return v;
  });
}

function runsOf(runs: readonly Run[]): Run[] {
  return normalizeRuns(runs);
}

export function draftEquals(a: BlockDraft, b: BlockDraft): boolean {
  return (
    a.id === b.id &&
    a.type === b.type &&
    stable(runsOf(a.content.runs)) === stable(runsOf(b.content.runs)) &&
    stable(a.properties) === stable(b.properties)
  );
}

export interface BlockDraftPatch {
  type?: BlockType;
  content?: RichTextV1;
  properties?: BlockProperties;
}

export interface BlockDiff {
  deletes: string[];
  updates: { id: string; patch: BlockDraftPatch; draft: BlockDraft }[];
  /** Blocks that only changed place, in the order they apply. */
  moves: { id: string; afterId: string | null }[];
  /** New blocks in document order, each right after `afterId`. */
  creates: { draft: BlockDraft; afterId: string | null }[];
}

/** Indices of one longest increasing subsequence (they keep their place). */
function longestIncreasing(values: readonly number[]): Set<number> {
  const tails: number[] = [];
  const parent: number[] = new Array<number>(values.length).fill(-1);
  for (let i = 0; i < values.length; i += 1) {
    const value = values[i] as number;
    let lo = 0;
    let hi = tails.length;
    while (lo < hi) {
      const mid = (lo + hi) >> 1;
      if ((values[tails[mid] as number] as number) < value) lo = mid + 1;
      else hi = mid;
    }
    if (lo > 0) parent[i] = tails[lo - 1] as number;
    tails[lo] = i;
  }
  const keep = new Set<number>();
  let at = tails.length ? (tails[tails.length - 1] as number) : -1;
  while (at >= 0) {
    keep.add(at);
    at = parent[at] as number;
  }
  return keep;
}

/**
 * The smallest set of writes that turns `previous` (the last saved snapshot)
 * into `next` (the editor's top-level blocks).
 */
export function diffBlockDrafts(
  previous: readonly BlockDraft[],
  next: readonly BlockDraft[],
): BlockDiff {
  const before = new Map(previous.map((draft) => [draft.id, draft]));
  const after = new Set(next.map((draft) => draft.id));
  const deletes = previous.filter((d) => !after.has(d.id)).map((d) => d.id);

  const updates: BlockDiff["updates"] = [];
  for (const draft of next) {
    const old = before.get(draft.id);
    if (!old || draftEquals(old, draft)) continue;
    const patch: BlockDraftPatch = {};
    if (old.type !== draft.type) patch.type = draft.type;
    if (
      stable(runsOf(old.content.runs)) !== stable(runsOf(draft.content.runs))
    ) {
      patch.content = richText(draft.content.runs);
    }
    if (stable(old.properties) !== stable(draft.properties)) {
      patch.properties = draft.properties;
    }
    updates.push({ id: draft.id, patch, draft });
  }

  // Survivors keep their relative order unless they moved: the longest run of
  // them already in order stays, the rest are moved.
  const survivors = previous.filter((d) => after.has(d.id));
  const rank = new Map(survivors.map((d, index) => [d.id, index]));
  const nextSurvivors = next.filter((d) => rank.has(d.id));
  const stay = longestIncreasing(
    nextSurvivors.map((d) => rank.get(d.id) as number),
  );
  const staying = new Set(
    nextSurvivors.filter((_d, index) => stay.has(index)).map((d) => d.id),
  );

  const moves: BlockDiff["moves"] = [];
  const creates: BlockDiff["creates"] = [];
  next.forEach((draft, index) => {
    const afterId = index > 0 ? (next[index - 1] as BlockDraft).id : null;
    if (!before.has(draft.id)) creates.push({ draft, afterId });
    else if (!staying.has(draft.id)) moves.push({ id: draft.id, afterId });
  });
  return { deletes, updates, moves, creates };
}

export function isEmptyDiff(diff: BlockDiff): boolean {
  return (
    diff.deletes.length === 0 &&
    diff.updates.length === 0 &&
    diff.moves.length === 0 &&
    diff.creates.length === 0
  );
}
