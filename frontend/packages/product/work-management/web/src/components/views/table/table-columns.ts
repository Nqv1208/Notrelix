import {
  AlignLeft,
  Calendar,
  CircleCheck,
  CircleDot,
  Flag,
  Hash,
  Link2,
  List,
  ListChecks,
  MessageCircle,
  Paperclip,
  SquareCheck,
  Tag,
  Type,
  User,
  type LucideIcon,
} from "lucide-react";
import type {
  FieldDefinition,
  FieldType,
  TableSortKey,
} from "@notrelix/work-management-core";

/**
 * The id of a table column; it is also the column's sort key. A custom field
 * column takes the id of its field.
 */
export type TableColumnId = TableSortKey | (string & {});

export interface TableColumnSpec {
  readonly id: TableColumnId;
  /** The custom field a column shows; absent on the built-in columns. */
  readonly field?: FieldDefinition;
  /** Default track width in px (the mockup grid); the view may override it. */
  readonly width: number;
  /** Glyph before the header label; Mã and Cập nhật have none. */
  readonly Icon?: LucideIcon;
  /** The item title column is always shown. */
  readonly locked?: boolean;
}

/**
 * The fifteen columns of the table, in display order. The first nine are
 * shown by default ("Cột 9/15"); start, created, completed, checklist,
 * comments and files are opt-in.
 * Widths are the defaults (the mockup grid) `84 340 140 184 132 96 120 136 120`, plus the
 * 40 px selection column that the table adds in front.
 */
export const TABLE_COLUMNS: readonly TableColumnSpec[] = [
  { id: "key", width: 84 },
  { id: "title", width: 340, Icon: AlignLeft, locked: true },
  { id: "status", width: 140, Icon: CircleCheck },
  { id: "assignee", width: 184, Icon: User },
  { id: "due", width: 132, Icon: Calendar },
  { id: "priority", width: 96, Icon: Flag },
  { id: "label", width: 120, Icon: Tag },
  { id: "progress", width: 136, Icon: ListChecks },
  { id: "checklist", width: 104, Icon: SquareCheck },
  { id: "comments", width: 104, Icon: MessageCircle },
  { id: "files", width: 80, Icon: Paperclip },
  { id: "updated", width: 120 },
  { id: "start", width: 120, Icon: Calendar },
  { id: "created", width: 120, Icon: Calendar },
  { id: "completed", width: 120, Icon: Calendar },
];

/** Width of the selection column in front of the data columns. */
export const TABLE_SELECT_WIDTH = 40;

const FIELD_ICON: Partial<Record<FieldType, LucideIcon>> = {
  text: Type,
  long_text: AlignLeft,
  link: Link2,
  number: Hash,
  checkbox: SquareCheck,
  select: CircleDot,
  multi_select: List,
  date: Calendar,
  person: User,
};

const FIELD_WIDTH: Partial<Record<FieldType, number>> = {
  long_text: 220,
  number: 150,
  checkbox: 130,
  date: 140,
  formula: 170,
  rollup: 170,
};

/** The column of a custom field: its type's glyph and default width. */
export function fieldColumn(field: FieldDefinition): TableColumnSpec {
  const Icon = FIELD_ICON[field.fieldType];
  return {
    id: field.id,
    field,
    width: FIELD_WIDTH[field.fieldType] ?? 160,
    ...(Icon ? { Icon } : {}),
  };
}

/** Every column the table can show: the built-in ones, then the custom fields. */
export function allTableColumns(
  fields: readonly FieldDefinition[] = [],
): TableColumnSpec[] {
  return [...TABLE_COLUMNS, ...fields.map(fieldColumn)];
}

/**
 * The visible columns in display order. The saved `columnOrder` (the view
 * config's) places the columns it names; the key and the title stay in front,
 * and a column the order does not name keeps its default place after them.
 */
export function visibleTableColumns(
  hidden: readonly string[],
  fields: readonly FieldDefinition[] = [],
  order: readonly string[] = [],
): TableColumnSpec[] {
  const columns = allTableColumns(fields).filter(
    (column) => column.locked || !hidden.includes(column.id),
  );
  if (order.length === 0) return columns;
  const isFixed = (column: TableColumnSpec) =>
    column.id === "key" || column.id === "title";
  const rank = (column: TableColumnSpec) => {
    const index = order.indexOf(column.id);
    return index < 0 ? Number.MAX_SAFE_INTEGER : index;
  };
  return [
    ...columns.filter(isFixed),
    ...columns
      .filter((column) => !isFixed(column))
      .sort((a, b) => rank(a) - rank(b)),
  ];
}

/** The ids with `id` moved one place; null when it cannot move. */
export function moveColumnId(
  ids: readonly string[],
  id: string,
  step: -1 | 1,
): string[] | null {
  const index = ids.indexOf(id);
  const target = index + step;
  if (index < 0 || target < 0 || target >= ids.length) return null;
  const next = ids.slice();
  [next[index], next[target]] = [next[target]!, next[index]!];
  return next;
}

/** Narrowest a column can be dragged to. */
export const TABLE_MIN_COLUMN_WIDTH = 72;
/** Widest a column can be dragged to. */
export const TABLE_MAX_COLUMN_WIDTH = 640;
/** Keyboard step of the resize handle; Shift takes the large step. */
export const TABLE_RESIZE_STEP = 8;
export const TABLE_RESIZE_LARGE_STEP = 32;

/** A saved width clamped to what a column can be. */
export function clampColumnWidth(width: number): number {
  return Math.min(
    TABLE_MAX_COLUMN_WIDTH,
    Math.max(TABLE_MIN_COLUMN_WIDTH, Math.round(width)),
  );
}

/** Saved widths by column id (the view config's `columnWidths`). */
export type TableColumnWidths = Readonly<Record<string, number>>;

/** The width of a column: the saved one when valid, else the design width. */
export function columnWidth(
  column: TableColumnSpec,
  widths: TableColumnWidths,
): number {
  const saved = widths[column.id];
  return typeof saved === "number" && Number.isFinite(saved)
    ? clampColumnWidth(saved)
    : column.width;
}

/** `grid-template-columns` for the selection column and the given columns. */
export function tableGridTemplate(
  columns: readonly TableColumnSpec[],
  widths: TableColumnWidths = {},
): string {
  return [
    TABLE_SELECT_WIDTH,
    ...columns.map((column) => columnWidth(column, widths)),
  ]
    .map((width) => `${width}px`)
    .join(" ");
}
