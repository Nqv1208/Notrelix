import { useLocale } from "@notrelix/i18n";
import {
  workspaceShellCopy,
  type WorkspaceShellCopy,
} from "./workspace-shell.copy";

/** Shell copy for the active locale (requires the host `I18nProvider`). */
export function useWorkspaceShellCopy(): WorkspaceShellCopy {
  const { locale } = useLocale();
  return workspaceShellCopy(locale);
}
