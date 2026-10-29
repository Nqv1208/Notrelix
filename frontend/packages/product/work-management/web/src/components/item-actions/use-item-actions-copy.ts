import { useLocale } from "@notrelix/i18n";
import { en, vi, type ItemActionsCopy } from "./item-actions.copy";

/** Item actions copy for the active locale (requires the host `I18nProvider`). */
export function useItemActionsCopy(): ItemActionsCopy {
  const { locale } = useLocale();
  return locale === "vi" ? vi : en;
}
