import type {
  BoardSettingsFieldDto,
  BoardSettingsSystemFieldDto,
} from "@notrelix/work-management-core";
import type { BoardSettingsCopy } from "./board-settings.copy";

/** "dd/MM" of an instant in a time zone. */
export function shortDay(iso: string, timeZone: string): string {
  const parts = new Intl.DateTimeFormat("en-GB", {
    day: "2-digit",
    month: "2-digit",
    timeZone,
  }).format(new Date(iso));
  return parts;
}

export function fillCopy(
  message: string,
  values: Readonly<Record<string, string | number>>,
): string {
  return message.replace(/\{\{(\w+)\}\}/g, (match, key: string) =>
    key in values ? String(values[key]) : match,
  );
}

/** Picks the `_one` form for exactly 1, else `_other`, then fills it. */
export function plural(
  one: string,
  other: string,
  count: number,
  values: Readonly<Record<string, string | number>> = {},
): string {
  return fillCopy(count === 1 ? one : other, { ...values, count });
}

export function typeName(
  copy: BoardSettingsCopy,
  type: BoardSettingsFieldDto["type"],
): string {
  return copy.types[type];
}

/** The type with its long wording where the field panel has one. */
export function typeLongName(
  copy: BoardSettingsCopy,
  type: BoardSettingsFieldDto["type"],
): string {
  const long = (copy.typeLong as Readonly<Record<string, string>>)[type];
  return long ?? copy.types[type];
}

export interface SystemFieldRow {
  readonly id: string;
  readonly key: string;
  readonly name: string;
  readonly type: string;
  readonly detail: string;
  readonly lock: BoardSettingsSystemFieldDto["lock"];
  readonly first: BoardSettingsSystemFieldDto;
}

/**
 * The system fields as rows: consecutive locked fields that carry no detail
 * line (the creator, the creation time, the last update) share one row.
 */
export function systemFieldRows(
  copy: BoardSettingsCopy,
  fields: readonly BoardSettingsSystemFieldDto[],
): SystemFieldRow[] {
  const rows: SystemFieldRow[] = [];
  let group: BoardSettingsSystemFieldDto[] = [];
  const flush = () => {
    if (group.length === 0) return;
    const types = [
      ...new Set(group.map((field) => typeName(copy, field.type))),
    ];
    rows.push({
      id: group[0]!.id,
      key: `${group[0]!.key}…`,
      name: group.map((field) => field.name).join(", "),
      type: types.join(", "),
      detail: "",
      lock: "locked",
      first: group[0]!,
    });
    group = [];
  };
  for (const field of fields) {
    if (field.lock === "locked" && field.detail === "") {
      group.push(field);
      continue;
    }
    flush();
    rows.push({
      id: field.id,
      key: field.key,
      name: field.name,
      type: typeName(copy, field.type),
      detail: field.detail,
      lock: field.lock,
      first: field,
    });
  }
  flush();
  return rows;
}

/** Colours a new option takes, in turn. */
export const OPTION_COLORS: readonly string[] = [
  "#1E7BE0",
  "#C42340",
  "#B4500B",
  "#2C5E4F",
  "#5532A8",
  "#9A6A12",
  "#0E7490",
  "#7A8391",
];

/** A field's name with its unit, as the table and the panels list it ("Ước lượng (giờ)"). */
export function fieldLabel(field: {
  readonly name: string;
  readonly unit: string | null;
}): string {
  return field.unit ? `${field.name} (${field.unit})` : field.name;
}
