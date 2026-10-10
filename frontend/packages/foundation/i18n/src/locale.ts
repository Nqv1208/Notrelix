/**
 * Supported locales and detection.
 *
 * Detection order (WX-FND-006): stored preference → browser languages →
 * Vietnamese. Host-neutral: the host passes storage and browser languages in;
 * this module never reads `navigator` or `localStorage` itself.
 */

export const SUPPORTED_LOCALES = ["vi", "en"] as const;
export type Locale = (typeof SUPPORTED_LOCALES)[number];

export const DEFAULT_LOCALE: Locale = "vi";

export const LOCALE_STORAGE_KEY = "notrelix-locale";

/** Minimal storage port (structurally compatible with the runtime KeyValueStorage). */
export interface LocaleStorage {
  getItem(key: string): string | null;
  setItem(key: string, value: string): void;
}

export function isLocale(value: unknown): value is Locale {
  return (
    typeof value === "string" &&
    (SUPPORTED_LOCALES as readonly string[]).includes(value)
  );
}

/** Maps a BCP 47 tag (`en-US`, `vi-VN`, `EN`) to a supported locale. */
export function matchLocale(
  tag: string | null | undefined,
): Locale | undefined {
  if (!tag) return undefined;
  const language = tag.trim().toLowerCase().split(/[-_]/)[0];
  return isLocale(language) ? language : undefined;
}

export interface DetectLocaleInput {
  readonly stored?: string | null;
  readonly browserLocales?: readonly string[];
  readonly fallback?: Locale;
}

export function detectLocale({
  stored,
  browserLocales = [],
  fallback = DEFAULT_LOCALE,
}: DetectLocaleInput): Locale {
  if (isLocale(stored)) return stored;
  for (const tag of browserLocales) {
    const match = matchLocale(tag);
    if (match) return match;
  }
  return fallback;
}

export function readStoredLocale(
  storage: LocaleStorage | undefined,
): string | null {
  if (!storage) return null;
  try {
    return storage.getItem(LOCALE_STORAGE_KEY);
  } catch {
    return null;
  }
}

export function writeStoredLocale(
  storage: LocaleStorage | undefined,
  locale: Locale,
): void {
  try {
    storage?.setItem(LOCALE_STORAGE_KEY, locale);
  } catch {
    // Storage unavailable: the in-memory locale still applies.
  }
}
