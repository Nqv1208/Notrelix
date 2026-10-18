import { useLocale } from "@notrelix/i18n";
import { itemSheetCopy, type ItemSheetCopy } from "./item-sheet.copy";

/** Item side sheet copy for the active locale (requires the host `I18nProvider`). */
export function useItemSheetCopy(): ItemSheetCopy {
  const { locale } = useLocale();
  return itemSheetCopy(locale);
}
