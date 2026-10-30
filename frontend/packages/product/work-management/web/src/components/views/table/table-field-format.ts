import {
  specOf,
  type ComputedValue,
  type FieldDefinition,
} from "@notrelix/work-management-core";
import type { MainTableCopy } from "./main-table.copy";

/** A person a field can hold. */
export interface TableFieldMember {
  readonly userId: string;
  readonly name: string;
  readonly initials: string;
}

/** "2026-10-15" as "15/10/2026"; anything else as it is. */
export function formatFieldDate(value: unknown): string {
  const text = typeof value === "string" ? value.slice(0, 10) : "";
  const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(text);
  return match ? `${match[3]}/${match[2]}/${match[1]}` : text;
}

/** The ids a Select-like value holds, as a list. */
export function fieldIds(value: unknown): string[] {
  if (Array.isArray(value))
    return value.filter((entry): entry is string => typeof entry === "string");
  return typeof value === "string" && value !== "" ? [value] : [];
}

/** The plain text of a custom field value: what a cell, a tooltip and an editor's draft start from. */
export function fieldText(
  field: FieldDefinition,
  value: unknown,
  members: readonly TableFieldMember[],
): string {
  const spec = specOf(field);
  if (spec.isEmpty(value)) return "";
  if (field.fieldType === "date") return formatFieldDate(value);
  if (field.fieldType === "checkbox") return "";
  return spec.text(value, {
    field,
    memberName: (userId) =>
      members.find((member) => member.userId === userId)?.name ?? userId,
  });
}

/** What a Formula or Rollup result reads as, with the reason for an error. */
export function computedText(
  field: FieldDefinition,
  computed: ComputedValue | undefined,
  copy: MainTableCopy["fields"],
): {
  readonly text: string;
  readonly title: string;
  readonly tone: "value" | "empty" | "error" | "stale";
} {
  const title = `${field.name} · ${copy.lockComputed}`;
  if (!computed || computed.state === "empty")
    return {
      text: copy.empty,
      title: `${title} · ${copy.computedEmpty}`,
      tone: "empty",
    };
  if (computed.state === "error")
    return {
      text: copy.computedError,
      title: `${title} · ${copy.computedErrors[computed.errorCode] ?? copy.computedUnknownError}`,
      tone: "error",
    };
  const shown = Array.isArray(computed.value)
    ? computed.value.join(", ")
    : String(computed.value ?? "");
  return {
    text: shown === "" ? copy.empty : shown,
    title: computed.state === "stale" ? `${title} · ${copy.stale}` : title,
    tone: computed.state === "stale" ? "stale" : "value",
  };
}
