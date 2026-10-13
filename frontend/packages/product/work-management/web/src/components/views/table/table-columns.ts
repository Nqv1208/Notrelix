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
  /** Track width in px (the mockup grid). */
  readonly width: number;
  /** Glyph before the header label; Mã and Cập nhật have none. */
  readonly Icon?: LucideIcon;
  /** The item title column is always shown. */
  readonly locked?: boolean;
}

/**
 * The twelve columns of the table, in display order. The first nine are
 * shown by default ("Cột 9/12"); start, created and completed are opt-in.
 * Widths are the mockup grid `84 340 140 184 132 96 120 136 120` plus the
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

/** `grid-template-columns` for the selection column and the given columns. */
export function tableGridTemplate(columns: readonly TableColumnSpec[]): string {
  return [TABLE_SELECT_WIDTH, ...columns.map((column) => column.width)]
    .map((width) => `${width}px`)
    .join(" ");
}

/**
 * Left offsets of the sticky columns: the selection column at 0, the key
 * after it and the title after the key (or after the selection column when
 * the key is hidden).
 */
export function stickyOffsets(columns: readonly TableColumnSpec[]): {
  key: number;
  title: number;
} {
  const key = TABLE_SELECT_WIDTH;
  const keyColumn = columns.find((column) => column.id === "key");
  return { key, title: key + (keyColumn ? keyColumn.width : 0) };
}
