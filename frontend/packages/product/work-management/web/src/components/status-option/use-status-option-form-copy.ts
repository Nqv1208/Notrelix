import { useLocale } from "@notrelix/i18n";
import {
  statusOptionFormCopy,
  type StatusOptionFormCopy,
} from "./status-option-form.copy";

/** Status option form copy for the active locale (requires the host `I18nProvider`). */
export function useStatusOptionFormCopy(): StatusOptionFormCopy {
  const { locale } = useLocale();
  return statusOptionFormCopy(locale);
}
