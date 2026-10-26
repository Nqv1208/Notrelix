import type { Block, TableCell, TableData } from "@notrelix/docs-core";

/** Indent step of a text block, in points. */
export const INDENT_STEP = 20;

/** Line height of a spacing step relative to the block's normal leading. */
export const LINE_SPACING_SCALE = {
  tight: 0.85,
  relaxed: 1.1,
  loose: 1.35,
} as const;

export interface BlockLayout {
  /** Left inset in points (indent level times the step). */
  readonly inset: number;
  readonly align?: "center" | "right" | "justify";
  readonly lineSpacing?: keyof typeof LINE_SPACING_SCALE;
}

const MAX_INDENT = 8;

/** How a block's indent, alignment and line spacing are drawn; null when none is set. */
export function blockLayoutOf(block: Block): BlockLayout | null {
  const { indent, align, lineSpacing } = block.properties;
  const level =
    typeof indent === "number" && Number.isFinite(indent)
      ? Math.min(MAX_INDENT, Math.max(0, Math.floor(indent)))
      : 0;
  const spacing =
    lineSpacing && lineSpacing in LINE_SPACING_SCALE ? lineSpacing : undefined;
  const alignment = align && align !== "left" ? align : undefined;
  if (!level && !spacing && !alignment) return null;
  return {
    inset: level * INDENT_STEP,
    ...(alignment ? { align: alignment } : {}),
    ...(spacing ? { lineSpacing: spacing } : {}),
  };
}

const SCRIPT_SOURCE = "0123456789+-=()";
const SUPERSCRIPT = "⁰¹²³⁴⁵⁶⁷⁸⁹⁺⁻⁼⁽⁾";
const SUBSCRIPT = "₀₁₂₃₄₅₆₇₈₉₊₋₌₍₎";

/**
 * Superscript or subscript text as the Unicode characters that look raised or
 * lowered; null when a character has none, so the caller draws it smaller.
 */
export function scriptCharacters(
  text: string,
  kind: "superscript" | "subscript",
): string | null {
  const table = kind === "superscript" ? SUPERSCRIPT : SUBSCRIPT;
  let out = "";
  for (const char of text) {
    const at = SCRIPT_SOURCE.indexOf(char);
    if (at < 0) return null;
    out += table[at]!;
  }
  return out;
}

/** One position of a table row on its grid. */
export type TableSlot =
  | {
      readonly kind: "cell";
      readonly cell: TableCell;
      readonly colSpan: number;
      readonly rowSpan: number;
    }
  | { readonly kind: "covered" };

/**
 * The table's rows with every grid column accounted for. A merged cell is
 * stored once, in the row it starts; the rows it spans get a `covered` slot
 * in its columns so later cells stay under their own column.
 */
export function layoutTable(table: TableData): TableSlot[][] {
  const covered: number[] = [];
  return table.rows.map((row) => {
    const slots: TableSlot[] = [];
    let column = 0;
    const skipCovered = () => {
      while ((covered[column] ?? 0) > 0) {
        slots.push({ kind: "covered" });
        column += 1;
      }
    };
    for (const cell of row) {
      skipCovered();
      const colSpan = Math.max(1, Math.floor(cell.colSpan ?? 1));
      const rowSpan = Math.max(1, Math.floor(cell.rowSpan ?? 1));
      slots.push({ kind: "cell", cell, colSpan, rowSpan });
      for (let at = column; at < column + colSpan; at += 1) {
        covered[at] = rowSpan;
      }
      column += colSpan;
    }
    skipCovered();
    for (let at = 0; at < covered.length; at += 1) {
      if ((covered[at] ?? 0) > 0) covered[at] = covered[at]! - 1;
    }
    return slots;
  });
}
