import {
  NONE_VALUE,
  type CompoundFilter,
  type FilterCondition,
  type FilterField,
  type MemberNames,
} from "@notrelix/work-management-core";
import { fillFiltersCopy, type FiltersCopy } from "./filters.copy";

const DAY = /^(\d{4})-(\d{2})-(\d{2})$/;

/** "2026-10-12" as 12/10/2026 (the canvas format); anything else as it is. */
export function formatDay(value: string): string {
  const match = DAY.exec(value);
  return match ? `${match[3]}/${match[2]}/${match[1]}` : value;
}

function shortDay(value: string): string {
  const match = DAY.exec(value);
  return match ? `${match[3]}/${match[2]}` : value;
}

/** The label of one option value of a field (an option, a person, the "unassigned" token). */
export function valueLabel(
  copy: FiltersCopy,
  field: FilterField,
  memberName: MemberNames,
  value: string,
): string {
  if (value === NONE_VALUE) return copy.filter.unassigned;
  if (field.field.fieldType === "person") return memberName(value);
  const option = field.field.options.find((o) => o.id === value);
  if (option) return option.label;
  if (field.field.fieldType === "date") return formatDay(value);
  return value;
}

/**
 * One readable chip per condition: "Người phụ trách: Chưa giao", "Hạn: 12/10 –
 * 18/10", "Tiêu đề chứa “báo cáo”". A condition on a removed field is named by
 * its stale label.
 */
export function describeCondition(
  copy: FiltersCopy,
  condition: FilterCondition,
  field: FilterField | undefined,
  memberName: MemberNames,
): string {
  if (!field) {
    return fillFiltersCopy(copy.stale.chip, { name: copy.stale.unknownField });
  }
  const op =
    copy.operators[condition.operator as keyof FiltersCopy["operators"]];
  const values = condition.values.map((value) =>
    valueLabel(copy, field, memberName, value),
  );
  if (field.id === "$overdue") return field.name;
  if (condition.operator === "between")
    return `${field.name}: ${shortDay(condition.values[0] ?? "")} – ${shortDay(condition.values[1] ?? "")}`;
  if (condition.operator === "is_empty" && field.field.fieldType === "person")
    return `${field.name}: ${copy.filter.unassigned}`;
  if (
    condition.operator === "has_any" ||
    (condition.operator === "equals" && field.field.fieldType === "select")
  )
    return `${field.name}: ${values.join(", ")}`;
  if (values.length === 0)
    return fillFiltersCopy(copy.filter.chipEmpty, {
      field: field.name,
      operator: op,
    });
  const shown =
    field.field.fieldType === "text" || field.field.fieldType === "link"
      ? `“${values.join(", ")}”`
      : values.join(", ");
  return `${field.name} ${op} ${shown}`;
}

/** The ids of fields a filter names that the board no longer has, with how many conditions use them. */
export function staleConditionCount(
  filter: CompoundFilter,
  fields: readonly FilterField[],
): number {
  const known = new Set(fields.map((f) => f.id));
  return filter.conditions.filter((c) => !known.has(c.fieldId)).length;
}

/** The label of a date bucket key: "Tuần 12/10 – 18/10/2026", "Tháng 10/2026", "Quý 4/2026", "2026" or a day. */
export function bucketLabel(
  copy: FiltersCopy,
  key: string,
  bucket: "day" | "week" | "month" | "quarter" | "year",
): string {
  if (bucket === "day") return formatDay(key);
  if (bucket === "week") {
    const match = DAY.exec(key);
    if (!match) return key;
    const start = new Date(Date.UTC(+match[1]!, +match[2]! - 1, +match[3]!));
    const end = new Date(start.getTime() + 6 * 86_400_000);
    const pad = (n: number) => String(n).padStart(2, "0");
    return fillFiltersCopy(copy.group.week, {
      from: `${pad(start.getUTCDate())}/${pad(start.getUTCMonth() + 1)}`,
      to: `${pad(end.getUTCDate())}/${pad(end.getUTCMonth() + 1)}/${end.getUTCFullYear()}`,
    });
  }
  if (bucket === "month") {
    const [year, month] = key.split("-");
    return fillFiltersCopy(copy.group.month, {
      month: Number(month),
      year: year ?? "",
    });
  }
  if (bucket === "quarter") {
    const [year, quarter] = key.split("-Q");
    return fillFiltersCopy(copy.group.quarter, {
      quarter: quarter ?? "",
      year: year ?? "",
    });
  }
  return key;
}
