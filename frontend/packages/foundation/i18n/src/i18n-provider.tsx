import * as React from "react";
import { I18nextProvider, useTranslation } from "react-i18next";
import type { MessageCatalog } from "./catalog";
import { createI18n } from "./create-i18n";
import { createFormatters, type Formatters } from "./format";
import {
  SUPPORTED_LOCALES,
  detectLocale,
  isLocale,
  readStoredLocale,
  writeStoredLocale,
  type Locale,
  type LocaleStorage,
} from "./locale";

export interface I18nProviderProps {
  readonly children: React.ReactNode;
  /** Catalogs composed by the host (one namespace each). */
  readonly catalogs: readonly MessageCatalog[];
  /** Host storage port for the locale preference. */
  readonly storage?: LocaleStorage;
  /** Browser/device language tags, most preferred first (host-provided). */
  readonly browserLocales?: readonly string[];
  /** Forces the initial locale (tests, previews). Skips detection. */
  readonly initialLocale?: Locale;
  /** Host hook, e.g. to set `<html lang>`. Called on mount and on change. */
  readonly onLocaleChange?: (locale: Locale) => void;
}

export interface LocaleContextValue {
  readonly locale: Locale;
  readonly setLocale: (locale: Locale) => void;
  readonly locales: readonly Locale[];
}

const LocaleContext = React.createContext<LocaleContextValue | null>(null);

export function I18nProvider({
  children,
  catalogs,
  storage,
  browserLocales,
  initialLocale,
  onLocaleChange,
}: I18nProviderProps) {
  const [initial] = React.useState<Locale>(
    () =>
      initialLocale ??
      detectLocale({ stored: readStoredLocale(storage), browserLocales }),
  );
  const [instance] = React.useState(() =>
    createI18n({ catalogs, locale: initial }),
  );
  const [locale, setLocaleState] = React.useState<Locale>(initial);

  const onLocaleChangeRef = React.useRef(onLocaleChange);
  onLocaleChangeRef.current = onLocaleChange;

  React.useEffect(() => {
    onLocaleChangeRef.current?.(locale);
  }, [locale]);

  const setLocale = React.useCallback(
    (next: Locale) => {
      if (!isLocale(next)) return;
      void instance.changeLanguage(next);
      writeStoredLocale(storage, next);
      setLocaleState(next);
    },
    [instance, storage],
  );

  const value = React.useMemo<LocaleContextValue>(
    () => ({ locale, setLocale, locales: SUPPORTED_LOCALES }),
    [locale, setLocale],
  );

  return (
    <I18nextProvider i18n={instance}>
      <LocaleContext.Provider value={value}>{children}</LocaleContext.Provider>
    </I18nextProvider>
  );
}

function useLocaleContext(hook: string): LocaleContextValue {
  const context = React.useContext(LocaleContext);
  if (!context) throw new Error(`${hook} must be used inside <I18nProvider>.`);
  return context;
}

/** Current locale, the setter, and supported locales. */
export function useLocale(): LocaleContextValue {
  return useLocaleContext("useLocale");
}

/**
 * Translation function for one catalog namespace.
 *
 *   const { t } = useT(appearanceCatalog);
 *   t("mode.legend");
 *   t("items", { count: 3 });   // plural: items_one / items_other
 */
export function useT(catalog: MessageCatalog | string) {
  useLocaleContext("useT");
  const namespace = typeof catalog === "string" ? catalog : catalog.namespace;
  const { t } = useTranslation(namespace);
  return { t };
}

/** `Intl` formatters bound to the current locale. */
export function useFormatters(): Formatters {
  const { locale } = useLocaleContext("useFormatters");
  return React.useMemo(() => createFormatters(locale), [locale]);
}
