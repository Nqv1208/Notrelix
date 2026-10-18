import { useLocale } from "@notrelix/i18n";
import {
  workspaceSettingsCopy,
  type WorkspaceSettingsCopy,
} from "./workspace-settings.copy";

/** Settings copy for the active locale (requires the host `I18nProvider`). */
export function useWorkspaceSettingsCopy(): WorkspaceSettingsCopy {
  const { locale } = useLocale();
  return workspaceSettingsCopy(locale);
}
