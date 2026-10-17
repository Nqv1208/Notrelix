import { useLocale } from "@notrelix/i18n";
import { accountNavCopy, type AccountNavCopy } from "./account-nav.copy";

/** Account navigation copy for the active locale (requires `I18nProvider`). */
export function useAccountNavCopy(): AccountNavCopy {
  return accountNavCopy(useLocale().locale);
}
