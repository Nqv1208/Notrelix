import { useLocale } from "@notrelix/i18n";
import {
  accountTokensCopy,
  type AccountTokensCopy,
} from "./account-tokens.copy";

/** API token copy for the active locale (requires the host `I18nProvider`). */
export function useAccountTokensCopy(): AccountTokensCopy {
  return accountTokensCopy(useLocale().locale);
}
