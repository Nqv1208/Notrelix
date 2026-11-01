import { useLocale } from "@notrelix/i18n";
import { savedFiltersCopy, type SavedFiltersCopy } from "./saved-filters.copy";

/** Saved-filters copy for the active locale (requires the host `I18nProvider`). */
export function useSavedFiltersCopy(): SavedFiltersCopy {
  const { locale } = useLocale();
  return savedFiltersCopy(locale);
}
