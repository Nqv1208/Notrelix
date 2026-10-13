import { useLocale } from "@notrelix/i18n";
import { calendarCopy, type CalendarCopy } from "./calendar.copy";

/** Calendar copy for the active locale (requires the host `I18nProvider`). */
export function useCalendarCopy(): CalendarCopy {
  const { locale } = useLocale();
  return calendarCopy(locale);
}
