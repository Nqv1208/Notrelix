import { useLocale } from "@notrelix/i18n";
import {
  accountSecurityCopy,
  type AccountSecurityCopy,
} from "./account-security.copy";

/** Security copy for the active locale (requires the host `I18nProvider`). */
export function useAccountSecurityCopy(): AccountSecurityCopy {
  return accountSecurityCopy(useLocale().locale);
}
