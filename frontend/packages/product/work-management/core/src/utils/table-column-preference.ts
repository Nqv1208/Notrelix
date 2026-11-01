/**
 * A member's own table columns: which are hidden, their order and their
 * widths. Kept on the board preference as an opaque JSON object (the server
 * checks only that it is an object of at most 4 KB). Every key is optional: a
 * key the member never changed falls back to the shared view's.
 */
export interface TableColumnPreference {
  readonly hidden?: readonly string[];
  readonly order?: readonly string[];
  readonly widths?: Readonly<Record<string, number>>;
}

export const TABLE_COLUMN_PREFERENCE_VERSION = 1;

/** The server refuses more than this many UTF-8 bytes. */
export const TABLE_COLUMN_PREFERENCE_MAX_BYTES = 4096;

const strings = (value: unknown): string[] | undefined =>
  Array.isArray(value) && value.every((entry) => typeof entry === "string")
    ? [...new Set(value as string[])]
    : undefined;

/** Reads what the server holds; anything that is not the expected shape is ignored. */
export function parseTableColumnPreference(
  raw: string | null | undefined,
): TableColumnPreference {
  if (!raw) return {};
  try {
    const value: unknown = JSON.parse(raw);
    if (typeof value !== "object" || value === null || Array.isArray(value))
      return {};
    const { hidden, order, widths } = value as Record<string, unknown>;
    const hiddenIds = strings(hidden);
    const orderIds = strings(order);
    const widthEntries =
      typeof widths === "object" && widths !== null && !Array.isArray(widths)
        ? Object.entries(widths).filter(
            (entry): entry is [string, number] =>
              typeof entry[1] === "number" && Number.isFinite(entry[1]),
          )
        : undefined;
    return {
      ...(hiddenIds ? { hidden: hiddenIds } : {}),
      ...(orderIds ? { order: orderIds } : {}),
      ...(widthEntries ? { widths: Object.fromEntries(widthEntries) } : {}),
    };
  } catch {
    return {};
  }
}

/** The JSON the server stores; an empty preference serializes to "" (the server clears it). */
export function serializeTableColumnPreference(
  preference: TableColumnPreference,
): string {
  if (
    preference.hidden === undefined &&
    preference.order === undefined &&
    preference.widths === undefined
  )
    return "";
  return JSON.stringify({
    v: TABLE_COLUMN_PREFERENCE_VERSION,
    ...(preference.hidden ? { hidden: preference.hidden } : {}),
    ...(preference.order ? { order: preference.order } : {}),
    ...(preference.widths ? { widths: preference.widths } : {}),
  });
}

export function hasTableColumnPreference(
  preference: TableColumnPreference | undefined,
): boolean {
  return (
    preference !== undefined &&
    (preference.hidden !== undefined ||
      preference.order !== undefined ||
      preference.widths !== undefined)
  );
}
