import { useLocale } from "@notrelix/i18n";
import { mainTableCopy, type MainTableCopy } from "./main-table.copy";

/** Board table copy for the active locale (requires the host `I18nProvider`). */
export function useMainTableCopy(): MainTableCopy {
  const { locale } = useLocale();
  return mainTableCopy(locale);
}
