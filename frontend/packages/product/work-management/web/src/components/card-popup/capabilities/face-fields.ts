import type { Card, FieldDefinition } from "@notrelix/work-management-core";

/** The fields a card face may show: any field that is not the title, status, priority or a person field the face already draws. */
export function faceEligibleFields(
  fields: readonly FieldDefinition[],
): FieldDefinition[] {
  return fields.filter(
    (field) =>
      !field.isHidden &&
      field.semanticRole === undefined &&
      !["linked_page", "progress", "relation", "timeline", "person"].includes(
        field.fieldType,
      ) &&
      !field.isSystemField,
  );
}

function optionLabel(field: FieldDefinition, id: unknown): string | null {
  return field.options.find((option) => option.id === id)?.label ?? null;
}

function shortDate(value: unknown): string | null {
  if (typeof value !== "string") return null;
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return null;
  const pad = (n: number) => String(n).padStart(2, "0");
  return `${pad(date.getUTCDate())}/${pad(date.getUTCMonth() + 1)}`;
}

/** The words a field value shows on the card face; null when the card has no value (an empty field). */
export function faceFieldText(card: Card, field: FieldDefinition): string | null {
  const computed = card.computedValues?.[field.id];
  const value =
    computed !== undefined
      ? computed.state === "ok" || computed.state === "stale"
        ? computed.value
        : null
      : card.fieldValues?.[field.id];
  if (value === null || value === undefined || value === "") return null;
  switch (field.fieldType) {
    case "select":
      return optionLabel(field, value);
    case "multi_select": {
      const labels = Array.isArray(value)
        ? value.map((id) => optionLabel(field, id)).filter((x): x is string => x !== null)
        : [];
      return labels.length > 0 ? labels.join(", ") : null;
    }
    case "date":
      return shortDate(value);
    case "checkbox":
      return value === true ? field.name : null;
    case "vote": {
      const count = (value as { count?: number }).count ?? 0;
      return count > 0 ? String(count) : null;
    }
    default:
      return typeof value === "object" ? null : String(value);
  }
}

/**
 * The face fields of a card in the view's order. A field with a value always shows; an empty one shows as a
 * dash unless it is marked "hide when empty".
 */
export function faceFieldsOf(
  card: Card,
  fields: readonly FieldDefinition[],
  cardFieldIds: readonly string[],
  cardHideEmpty: readonly string[],
): { field: FieldDefinition; text: string }[] {
  const byId = new Map(fields.map((field) => [field.id, field]));
  const shown: { field: FieldDefinition; text: string }[] = [];
  for (const id of cardFieldIds) {
    const field = byId.get(id);
    if (!field) continue;
    const text = faceFieldText(card, field);
    if (text === null && cardHideEmpty.includes(id)) continue;
    shown.push({ field, text: text ?? "—" });
  }
  return shown;
}
