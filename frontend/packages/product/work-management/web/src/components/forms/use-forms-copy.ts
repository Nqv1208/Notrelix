import { useLocale } from "@notrelix/i18n";
import { formsCopy, type FormsCopy } from "./forms.copy";

/** Forms copy for the active locale (requires the host `I18nProvider`). */
export function useFormsCopy(): FormsCopy {
  const { locale } = useLocale();
  return formsCopy(locale);
}
