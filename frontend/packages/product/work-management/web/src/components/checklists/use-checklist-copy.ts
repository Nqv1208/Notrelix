import { useLocale } from "@notrelix/i18n";
import { checklistCopy, type ChecklistCopy } from "./item-checklists.copy";

/** Checklist section copy for the active locale (requires the host `I18nProvider`). */
export function useChecklistCopy(): ChecklistCopy {
  const { locale } = useLocale();
  return checklistCopy(locale);
}
