/**
 * Message catalogs.
 *
 * Each package owns its catalogs (one namespace per catalog) next to the UI
 * that uses them, in files named `*.catalog.ts`; the host composes them into
 * the I18nProvider. Keys are nested objects; plural forms use i18next's
 * CLDR suffixes (`key_one`, `key_other`, …).
 *
 * Pure module (no React): safe to import from tooling via
 * `@notrelix/i18n/catalog`.
 */

import { SUPPORTED_LOCALES, type Locale } from "./locale";

export interface Messages {
  readonly [key: string]: string | Messages;
}

export interface MessageCatalog<Namespace extends string = string> {
  readonly namespace: Namespace;
  readonly messages: Readonly<Record<Locale, Messages>>;
}

/** Declares a catalog; `vi` and `en` are both required. */
export function defineCatalog<const Namespace extends string>(
  namespace: Namespace,
  messages: Readonly<Record<Locale, Messages>>,
): MessageCatalog<Namespace> {
  return { namespace, messages };
}

export const PLURAL_SUFFIXES = [
  "zero",
  "one",
  "two",
  "few",
  "many",
  "other",
] as const;

const PLURAL_SUFFIX_PATTERN = new RegExp(`_(${PLURAL_SUFFIXES.join("|")})$`);

/** Flattens nested messages to dotted keys. */
export function flattenMessages(
  messages: Messages,
  prefix = "",
): Record<string, string> {
  const out: Record<string, string> = {};
  for (const [key, value] of Object.entries(messages)) {
    const path = prefix ? `${prefix}.${key}` : key;
    if (typeof value === "string") out[path] = value;
    else Object.assign(out, flattenMessages(value, path));
  }
  return out;
}

export type CatalogIssueKind =
  "missing-key" | "missing-plural-form" | "empty-message";

export interface CatalogIssue {
  readonly namespace: string;
  readonly locale: Locale;
  readonly key: string;
  readonly kind: CatalogIssueKind;
}

function pluralCategories(locale: Locale): readonly string[] {
  return new Intl.PluralRules(locale).resolvedOptions().pluralCategories;
}

/**
 * Completeness check used by CI: every message key exists in every locale;
 * plural keys carry every CLDR category the locale needs (Vietnamese needs
 * only `other`; English needs `one` and `other`); no message is empty.
 */
export function findCatalogIssues(catalog: MessageCatalog): CatalogIssue[] {
  const issues: CatalogIssue[] = [];
  const flat = Object.fromEntries(
    SUPPORTED_LOCALES.map((locale) => [
      locale,
      flattenMessages(catalog.messages[locale] ?? {}),
    ]),
  ) as Record<Locale, Record<string, string>>;

  const plainKeys = new Set<string>();
  const pluralBases = new Set<string>();
  for (const locale of SUPPORTED_LOCALES) {
    for (const key of Object.keys(flat[locale])) {
      if (PLURAL_SUFFIX_PATTERN.test(key)) {
        pluralBases.add(key.replace(PLURAL_SUFFIX_PATTERN, ""));
      } else {
        plainKeys.add(key);
      }
    }
  }

  for (const locale of SUPPORTED_LOCALES) {
    const messages = flat[locale];
    for (const key of [...plainKeys].sort()) {
      if (!(key in messages)) {
        issues.push({
          namespace: catalog.namespace,
          locale,
          key,
          kind: "missing-key",
        });
      }
    }
    for (const base of [...pluralBases].sort()) {
      for (const category of pluralCategories(locale)) {
        const key = `${base}_${category}`;
        if (!(key in messages)) {
          issues.push({
            namespace: catalog.namespace,
            locale,
            key,
            kind: "missing-plural-form",
          });
        }
      }
    }
    for (const [key, value] of Object.entries(messages)) {
      if (value.trim() === "") {
        issues.push({
          namespace: catalog.namespace,
          locale,
          key,
          kind: "empty-message",
        });
      }
    }
  }

  return issues;
}

/** Rejects two catalogs claiming the same namespace. */
export function findDuplicateNamespaces(
  catalogs: readonly MessageCatalog[],
): string[] {
  const seen = new Set<string>();
  const duplicates = new Set<string>();
  for (const catalog of catalogs) {
    if (seen.has(catalog.namespace)) duplicates.add(catalog.namespace);
    seen.add(catalog.namespace);
  }
  return [...duplicates];
}
