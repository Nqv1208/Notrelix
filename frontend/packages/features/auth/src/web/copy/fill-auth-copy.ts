/** Fills `{{name}}` placeholders; an unknown placeholder is left visible. */
export function fillAuthCopy(
  message: string,
  values: Readonly<Record<string, string | number>>,
): string {
  return message.replace(/\{\{(\w+)\}\}/g, (match, key: string) =>
    key in values ? String(values[key]) : match,
  );
}

/** Picks the `_one` form for exactly 1 and `_other` otherwise, then fills it. */
export function pluralAuthCopy(
  one: string,
  other: string,
  count: number,
  values: Readonly<Record<string, string | number>> = {},
): string {
  return fillAuthCopy(count === 1 ? one : other, { ...values, count });
}
