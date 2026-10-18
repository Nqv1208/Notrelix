/**
 * Shared presentation model for the account page surfaces (profile,
 * appearance, security, notifications). Plain types and helpers only, so the
 * pure surfaces and the fixtures can share them without a runtime.
 */

/** One independently loaded page (loading and error are page-level states). */
export type AccountSection<T> =
  | { readonly status: "loading" }
  | { readonly status: "error" }
  | { readonly status: "ready"; readonly data: T };

/** The outcome of the last write of a page. */
export type AccountSaveStatus =
  | { readonly state: "idle" }
  | { readonly state: "saving" }
  | { readonly state: "saved" }
  | { readonly state: "error" };

type DateInput = Date | number | string;

/**
 * Locale formatters the account surfaces use. Hosts pass `useFormatters()`
 * from `@notrelix/i18n`; fixtures pass `createFormatters(locale)`.
 */
export interface AccountFormatters {
  readonly date: (
    value: DateInput,
    options?: Intl.DateTimeFormatOptions,
  ) => string;
  readonly number: (
    value: number,
    options?: Intl.NumberFormatOptions,
  ) => string;
}

/** Locales the account pages are written for. */
export type AccountLocale = "vi" | "en";

/** Fills `{{name}}` placeholders; an unknown placeholder stays visible. */
export function fillCopy(
  message: string,
  values: Readonly<Record<string, string | number>>,
): string {
  return message.replace(/\{\{(\w+)\}\}/g, (match, key: string) =>
    key in values ? String(values[key]) : match,
  );
}

/** Picks the `_one` form for exactly 1 and `_other` otherwise, then fills it. */
export function pluralCopy(
  forms: { readonly one: string; readonly other: string },
  count: number,
  values: Readonly<Record<string, string | number>> = {},
): string {
  return fillCopy(count === 1 ? forms.one : forms.other, { ...values, count });
}
