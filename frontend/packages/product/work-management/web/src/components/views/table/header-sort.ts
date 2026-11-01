import type {
  FilterField,
  SortLevel,
  TableSort,
  TableSortKey,
} from "@notrelix/work-management-core";

/** The table column a sort field orders; none for a custom field the table has no column for. */
function columnOf(
  field: FilterField,
  customColumnIds: ReadonlySet<string>,
): TableSortKey | (string & {}) | null {
  if (customColumnIds.has(field.id)) return field.id;
  if (field.id === "$title") return "title";
  if (field.id === "$label") return "label";
  if (field.id === "$assignee") return "assignee";
  const { fieldType, semanticRole } = field.field;
  if (semanticRole === "status") return "status";
  if (semanticRole === "priority") return "priority";
  if (fieldType === "person") return "assignee";
  if (fieldType === "date") return "due";
  return null;
}

/** What the header marks for the member's first sort level; null when no column matches it. */
export function headerSortOf(
  level: SortLevel,
  fields: readonly FilterField[],
  customColumnIds: ReadonlySet<string> = new Set(),
): TableSort | null {
  const field = fields.find((f) => f.id === level.fieldId);
  const key = field ? columnOf(field, customColumnIds) : null;
  return key ? { key, direction: level.direction } : null;
}
