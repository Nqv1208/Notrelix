import { useLocale } from "@notrelix/i18n";
import { filtersCopy, type FiltersCopy } from "./filters.copy";

/** Filter, sort, group and swimlane copy for the active locale (requires the host `I18nProvider`). */
export function useFiltersCopy(): FiltersCopy {
  const { locale } = useLocale();
  return filtersCopy(locale);
}
