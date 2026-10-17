import { useLocale } from "@notrelix/i18n";
import {
  kanbanToolbarCopy,
  type KanbanToolbarCopy,
} from "./kanban-toolbar.copy";

/** Kanban toolbar copy for the active locale (requires the host `I18nProvider`). */
export function useKanbanToolbarCopy(): KanbanToolbarCopy {
  const { locale } = useLocale();
  return kanbanToolbarCopy(locale);
}
