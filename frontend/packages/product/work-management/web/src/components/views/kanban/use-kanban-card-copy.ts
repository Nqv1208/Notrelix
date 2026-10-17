import { useLocale } from "@notrelix/i18n";
import { kanbanCardCopy, type KanbanCardCopy } from "./kanban-card.copy";

/** Kanban card copy for the active locale (requires the host `I18nProvider`). */
export function useKanbanCardCopy(): KanbanCardCopy {
  const { locale } = useLocale();
  return kanbanCardCopy(locale);
}
