import { useLocale } from "@notrelix/i18n";
import { en, vi, type ItemActionsCopy } from "./item-actions.copy";

/** Item actions copy for a locale tag, for pure surfaces that already carry their own copy locale. */
export function itemActionsCopyFor(locale: string): ItemActionsCopy {
  return locale === "vi" ? vi : en;
}

/** Item actions copy for the active locale (requires the host `I18nProvider`). */
export function useItemActionsCopy(): ItemActionsCopy {
  const { locale } = useLocale();
  return itemActionsCopyFor(locale);
}
