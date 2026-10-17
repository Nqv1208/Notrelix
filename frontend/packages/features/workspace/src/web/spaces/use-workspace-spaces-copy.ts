import { useLocale } from "@notrelix/i18n";
import {
  workspaceSpacesCopy,
  type WorkspaceSpacesCopy,
} from "./workspace-spaces.copy";

/** Space org tree copy for the active locale (requires the host `I18nProvider`). */
export function useWorkspaceSpacesCopy(): WorkspaceSpacesCopy {
  const { locale } = useLocale();
  return workspaceSpacesCopy(locale);
}
