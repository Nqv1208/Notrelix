import { specOf } from "../field-types/field-type-registry";
import type { FieldDefinition, FieldType } from "../types/field-types";
import { findPriorityField, findStatusField } from "./status-lanes";

/** Field types the table shows as a column of its own. */
const TABLE_FIELD_TYPES: readonly FieldType[] = [
  "text",
  "long_text",
  "link",
  "number",
  "checkbox",
  "select",
  "multi_select",
  "date",
  "person",
  "formula",
  "rollup",
];

/**
 * The board's custom fields in column order: every visible, non-system field of a
 * type the table can show, beyond the item's own properties the table has columns
 * for (Status, Priority, the first Person field = Assignee, the first Date field = Due).
 */
export function tableCustomFields(
  fields: readonly FieldDefinition[],
): FieldDefinition[] {
  const own = new Set(
    [
      findStatusField(fields),
      findPriorityField(fields),
      fields.find((field) => field.fieldType === "person"),
      fields.find((field) => field.fieldType === "date"),
    ].flatMap((field) => (field ? [field.id] : [])),
  );
  return fields
    .filter(
      (field) =>
        !field.isHidden &&
        !field.isSystemField &&
        !own.has(field.id) &&
        TABLE_FIELD_TYPES.includes(field.fieldType),
    )
    .sort((a, b) => a.position - b.position);
}

/** A column the viewer can never edit: computed (Formula, Rollup) or a type with no inline editor. */
export function isReadOnlyFieldType(
  field: Pick<FieldDefinition, "fieldType">,
): boolean {
  return !specOf(field).editable;
}
