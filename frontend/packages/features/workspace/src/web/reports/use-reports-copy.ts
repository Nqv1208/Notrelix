import { useLocale } from "@notrelix/i18n";
import { reportsCopy, type ReportsCopy } from "./reports.copy";

/** Reports copy for the active locale (requires the host `I18nProvider`). */
export function useReportsCopy(): ReportsCopy {
  const { locale } = useLocale();
  return reportsCopy(locale);
}
