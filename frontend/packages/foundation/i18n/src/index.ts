// @notrelix/i18n — internationalization foundation (i18next + react-i18next).
// See frontend/docs/decisions/FE-ADR-006-i18n-library.md.

export {
  SUPPORTED_LOCALES,
  DEFAULT_LOCALE,
  LOCALE_STORAGE_KEY,
  isLocale,
  matchLocale,
  detectLocale,
  readStoredLocale,
  writeStoredLocale,
  type Locale,
  type LocaleStorage,
  type DetectLocaleInput,
} from "./locale";

export {
  defineCatalog,
  flattenMessages,
  findCatalogIssues,
  findDuplicateNamespaces,
  PLURAL_SUFFIXES,
  type Messages,
  type MessageCatalog,
  type CatalogIssue,
  type CatalogIssueKind,
} from "./catalog";

export {
  formatDate,
  formatDateTime,
  formatNumber,
  formatPercent,
  formatRelativeTime,
  formatList,
  createFormatters,
  type Formatters,
} from "./format";

export { createI18n, type CreateI18nOptions } from "./create-i18n";

export {
  I18nProvider,
  useLocale,
  useT,
  useFormatters,
  type I18nProviderProps,
  type LocaleContextValue,
} from "./i18n-provider";
