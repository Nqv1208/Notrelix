import { useLocale } from "@notrelix/i18n";
import { en, vi, type CardPopupCopy } from "./card-popup.copy";

/** Card popup copy for the active locale (requires the host `I18nProvider`). */
export function useCardPopupCopy(): CardPopupCopy {
  const { locale } = useLocale();
  return locale === "vi" ? vi : en;
}
