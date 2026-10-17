import { useLocale } from "@notrelix/i18n";
import {
  workspaceCommandCopy,
  type WorkspaceCommandCopy,
} from "./workspace-command.copy";

/** Command palette copy for the active locale (requires the host `I18nProvider`). */
export function useWorkspaceCommandCopy(): WorkspaceCommandCopy {
  const { locale } = useLocale();
  return workspaceCommandCopy(locale);
}
