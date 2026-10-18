import { useLocale } from "@notrelix/i18n";
import {
  accountAppearanceCopy,
  type AccountAppearanceCopy,
} from "./account-appearance.copy";

/** Appearance copy for the active locale (requires the host `I18nProvider`). */
export function useAccountAppearanceCopy(): AccountAppearanceCopy {
  return accountAppearanceCopy(useLocale().locale);
}
