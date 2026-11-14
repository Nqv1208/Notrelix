import {
  Calendar,
  CircleChevronDown,
  Hash,
  SquareCheck,
  Type,
  User,
  type LucideIcon,
} from "lucide-react";
import type { FilterField } from "@notrelix/work-management-core";

export type FieldTypeKind =
  "text" | "number" | "select" | "person" | "date" | "checkbox";

/** The family a field belongs to in the sort and group lists (the artboards' small type word). */
export function fieldTypeKind(field: FilterField): FieldTypeKind {
  switch (field.field.fieldType) {
    case "person":
      return "person";
    case "date":
      return "date";
    case "checkbox":
      return "checkbox";
    case "number":
      return "number";
    case "select":
    case "multi_select":
      return "select";
    default:
      return "text";
  }
}

export const fieldTypeIcon: Record<FieldTypeKind, LucideIcon> = {
  text: Type,
  number: Hash,
  select: CircleChevronDown,
  person: User,
  date: Calendar,
  checkbox: SquareCheck,
};
