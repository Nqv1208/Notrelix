import { useLocale } from "@notrelix/i18n";
import {
  cardCapabilitiesCopy,
  type CardCapabilitiesCopy,
} from "./capabilities.copy";

/** Capability copy for the active locale (requires the host `I18nProvider`). */
export function useCapabilitiesCopy(): CardCapabilitiesCopy {
  const { locale } = useLocale();
  return cardCapabilitiesCopy(locale);
}
