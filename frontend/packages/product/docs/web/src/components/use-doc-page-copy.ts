import { useLocale } from "@notrelix/i18n";
import { docPageCopy, type DocPageCopy } from "./doc-page.copy";

/** Document page copy for the active locale (requires the host `I18nProvider`). */
export function useDocPageCopy(): DocPageCopy {
  const { locale } = useLocale();
  return docPageCopy(locale);
}
