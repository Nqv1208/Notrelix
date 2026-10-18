import {
  AlignLeft,
  Calendar,
  CircleCheck,
  Flag,
  ListChecks,
  Tag,
  User,
  type LucideIcon,
} from "lucide-react";
import type { TableSortKey } from "@notrelix/work-management-core";

/** The id of a table column; it is also the column's sort key. */
export type TableColumnId = TableSortKey;

export interface TableColumnSpec {
  readonly id: TableColumnId;
  /** Default track width in px (the mockup grid); the view may override it. */
  readonly width: number;
  /** Glyph before the header label; Mã and Cập nhật have none. */
  readonly Icon?: LucideIcon;
  /** The item title column is always shown. */
  readonly locked?: boolean;
}

/**
 * The twelve columns of the table, in display order. The first nine are
 * shown by default ("Cột 9/12"); start, created and completed are opt-in.
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
  { id: "updated", width: 120 },
  { id: "start", width: 120, Icon: Calendar },
  { id: "created", width: 120, Icon: Calendar },
  { id: "completed", width: 120, Icon: Calendar },
];

/** Width of the selection column in front of the data columns. */
export const TABLE_SELECT_WIDTH = 40;

export function visibleTableColumns(
  hidden: readonly string[],
): TableColumnSpec[] {
  return TABLE_COLUMNS.filter(
    (column) => column.locked || !hidden.includes(column.id),
  );
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
