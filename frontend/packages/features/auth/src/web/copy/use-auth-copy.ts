import { useLocale } from "@notrelix/i18n";
import { authCopy, type AuthCopy } from "./auth.copy";

/** Sign-in pages copy for the active locale (requires the host `I18nProvider`). */
export function useAuthCopy(): AuthCopy {
  return authCopy(useLocale().locale);
}
