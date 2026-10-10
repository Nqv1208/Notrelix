/**
 * Locale-aware formatting via `Intl` (no extra dependency). Formatters are
 * cached per locale + options because Intl constructors are expensive.
 */

import type { Locale } from "./locale";

const cache = new Map<string, unknown>();

function cached<T>(key: string, create: () => T): T {
  let value = cache.get(key) as T | undefined;
  if (!value) {
    value = create();
    cache.set(key, value);
  }
  return value;
}

type DateInput = Date | number | string;

function toDate(value: DateInput): Date {
  return value instanceof Date ? value : new Date(value);
}

export function formatDate(
  locale: Locale,
  value: DateInput,
  options: Intl.DateTimeFormatOptions = { dateStyle: "medium" },
): string {
  return cached(
    `dt|${locale}|${JSON.stringify(options)}`,
    () => new Intl.DateTimeFormat(locale, options),
  ).format(toDate(value));
}

export function formatDateTime(
  locale: Locale,
  value: DateInput,
  options: Intl.DateTimeFormatOptions = {
    dateStyle: "medium",
    timeStyle: "short",
  },
): string {
  return formatDate(locale, value, options);
}

export function formatNumber(
  locale: Locale,
  value: number,
  options: Intl.NumberFormatOptions = {},
): string {
  return cached(
    `nf|${locale}|${JSON.stringify(options)}`,
    () => new Intl.NumberFormat(locale, options),
  ).format(value);
}

export function formatPercent(
  locale: Locale,
  ratio: number,
  options: Intl.NumberFormatOptions = { maximumFractionDigits: 0 },
): string {
  return formatNumber(locale, ratio, { ...options, style: "percent" });
}

const RELATIVE_UNITS: ReadonlyArray<[Intl.RelativeTimeFormatUnit, number]> = [
  ["year", 365 * 24 * 3600],
  ["month", 30 * 24 * 3600],
  ["week", 7 * 24 * 3600],
  ["day", 24 * 3600],
  ["hour", 3600],
  ["minute", 60],
  ["second", 1],
];

/** "3 days ago" / "3 ngày trước", choosing the largest whole unit. */
export function formatRelativeTime(
  locale: Locale,
  value: DateInput,
  now: DateInput = Date.now(),
  options: Intl.RelativeTimeFormatOptions = { numeric: "auto" },
): string {
  const seconds = (toDate(value).getTime() - toDate(now).getTime()) / 1000;
  const formatter = cached(
    `rt|${locale}|${JSON.stringify(options)}`,
    () => new Intl.RelativeTimeFormat(locale, options),
  );
  for (const [unit, size] of RELATIVE_UNITS) {
    if (Math.abs(seconds) >= size || unit === "second") {
      return formatter.format(Math.round(seconds / size), unit);
    }
  }
  return formatter.format(0, "second");
}

export function formatList(
  locale: Locale,
  items: readonly string[],
  options: Intl.ListFormatOptions = { type: "conjunction" },
): string {
  return cached(
    `lf|${locale}|${JSON.stringify(options)}`,
    () => new Intl.ListFormat(locale, options),
  ).format(items);
}

/** Formatters bound to one locale (returned by `useFormatters`). */
export function createFormatters(locale: Locale) {
  return {
    locale,
    date: (value: DateInput, options?: Intl.DateTimeFormatOptions) =>
      formatDate(locale, value, options),
    dateTime: (value: DateInput, options?: Intl.DateTimeFormatOptions) =>
      formatDateTime(locale, value, options),
    number: (value: number, options?: Intl.NumberFormatOptions) =>
      formatNumber(locale, value, options),
    percent: (ratio: number, options?: Intl.NumberFormatOptions) =>
      formatPercent(locale, ratio, options),
    relativeTime: (value: DateInput, now?: DateInput) =>
      formatRelativeTime(locale, value, now),
    list: (items: readonly string[], options?: Intl.ListFormatOptions) =>
      formatList(locale, items, options),
  } as const;
}

export type Formatters = ReturnType<typeof createFormatters>;
