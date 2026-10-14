import { useLocale } from "@notrelix/i18n";
import { timelineCopy, type TimelineCopy } from "./timeline.copy";

/** Timeline copy for the active locale (requires the host `I18nProvider`). */
export function useTimelineCopy(): TimelineCopy {
  const { locale } = useLocale();
  return timelineCopy(locale);
}
