import { useLocale } from "@notrelix/i18n";
import {
  accountProfileCopy,
  type AccountProfileCopy,
} from "./account-profile.copy";

/** Profile copy for the active locale (requires the host `I18nProvider`). */
export function useAccountProfileCopy(): AccountProfileCopy {
  return accountProfileCopy(useLocale().locale);
}
