import { useLocale } from "@notrelix/i18n";
import { itemPageCopy, type ItemPageCopy } from "./item-page.copy";

/** Item page copy for the active locale (requires the host `I18nProvider`). */
export function useItemPageCopy(): ItemPageCopy {
  return itemPageCopy(useLocale().locale);
}
