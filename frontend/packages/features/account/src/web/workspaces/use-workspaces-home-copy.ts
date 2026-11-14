import { useLocale } from "@notrelix/i18n";
import {
  workspacesHomeCopy,
  type WorkspacesHomeCopy,
} from "./workspaces-home.copy";

/** Copy of the account-level Home for the active locale (needs `I18nProvider`). */
export function useWorkspacesHomeCopy(): WorkspacesHomeCopy {
  return workspacesHomeCopy(useLocale().locale);
}
