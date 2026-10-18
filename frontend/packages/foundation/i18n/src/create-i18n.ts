import { createInstance, type i18n as I18nInstance } from "i18next";
import { initReactI18next } from "react-i18next";
import { findDuplicateNamespaces, type MessageCatalog } from "./catalog";
import { DEFAULT_LOCALE, SUPPORTED_LOCALES, type Locale } from "./locale";

export interface CreateI18nOptions {
  readonly catalogs: readonly MessageCatalog[];
  readonly locale: Locale;
}

/**
 * Creates an isolated i18next instance (never the global singleton) with all
 * catalogs loaded synchronously, so the first render is already translated.
 */
export function createI18n({
  catalogs,
  locale,
}: CreateI18nOptions): I18nInstance {
  const duplicates = findDuplicateNamespaces(catalogs);
  if (duplicates.length > 0) {
    throw new Error(
      `Duplicate i18n namespace(s): ${duplicates.join(", ")}. Each catalog owns one namespace.`,
    );
  }

  const resources = Object.fromEntries(
    SUPPORTED_LOCALES.map((lng) => [
      lng,
      Object.fromEntries(
        catalogs.map((catalog) => [catalog.namespace, catalog.messages[lng]]),
      ),
    ]),
  );

  const instance = createInstance();
  void instance.use(initReactI18next).init({
    resources,
    lng: locale,
    fallbackLng: DEFAULT_LOCALE,
    supportedLngs: [...SUPPORTED_LOCALES],
    ns: catalogs.map((catalog) => catalog.namespace),
    defaultNS: false,
    initAsync: false,
    interpolation: { escapeValue: false },
    returnNull: false,
    react: { useSuspense: false },
  });
  return instance;
}
