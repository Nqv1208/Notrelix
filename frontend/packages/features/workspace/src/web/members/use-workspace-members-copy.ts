import { useLocale } from "@notrelix/i18n";
import {
  workspaceMembersCopy,
  type WorkspaceMembersCopy,
} from "./workspace-members.copy";

/** Members copy for the active locale (requires the host `I18nProvider`). */
export function useWorkspaceMembersCopy(): WorkspaceMembersCopy {
  const { locale } = useLocale();
  return workspaceMembersCopy(locale);
}
