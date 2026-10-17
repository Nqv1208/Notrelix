import { useLocale } from "@notrelix/i18n";
import {
  workspaceTeamsCopy,
  type WorkspaceTeamsCopy,
} from "./workspace-teams.copy";

/** Teams copy for the active locale (requires the host `I18nProvider`). */
export function useWorkspaceTeamsCopy(): WorkspaceTeamsCopy {
  const { locale } = useLocale();
  return workspaceTeamsCopy(locale);
}
