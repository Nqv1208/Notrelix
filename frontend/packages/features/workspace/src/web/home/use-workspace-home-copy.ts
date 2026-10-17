import { useLocale } from "@notrelix/i18n";
import {
  workspaceHomeCopy,
  type WorkspaceHomeCopy,
} from "./workspace-home.copy";

/** Home copy for the active locale (requires the host `I18nProvider`). */
export function useWorkspaceHomeCopy(): WorkspaceHomeCopy {
  const { locale } = useLocale();
  return workspaceHomeCopy(locale);
}
