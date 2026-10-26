import { useLocale } from "@notrelix/i18n";
import { en, vi, type ItemCreateCopy } from "./item-create.copy";

/** "Add item" dialog copy for the active locale (requires the host `I18nProvider`). */
export function useItemCreateCopy(): ItemCreateCopy {
  return useLocale().locale === "vi" ? vi : en;
}
