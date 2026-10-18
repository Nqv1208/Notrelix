import { useLocale } from "@notrelix/i18n";
import { kanbanLaneCopy, type KanbanLaneCopy } from "./kanban-lane.copy";

/** Kanban lane copy for the active locale (requires the host `I18nProvider`). */
export function useKanbanLaneCopy(): KanbanLaneCopy {
  const { locale } = useLocale();
  return kanbanLaneCopy(locale);
}
