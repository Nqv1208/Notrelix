import { useLocale } from "@notrelix/i18n";
import {
  workspaceInboxCopy,
  type WorkspaceInboxCopy,
} from "./workspace-inbox.copy";

/** Inbox copy for the active locale (requires the host `I18nProvider`). */
export function useWorkspaceInboxCopy(): WorkspaceInboxCopy {
  const { locale } = useLocale();
  return workspaceInboxCopy(locale);
}
