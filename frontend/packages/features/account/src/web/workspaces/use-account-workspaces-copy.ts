import { useLocale } from "@notrelix/i18n";
import {
  accountWorkspacesCopy,
  type AccountWorkspacesCopy,
} from "./account-workspaces.copy";

/** Copy of the workspaces page for the active locale (needs `I18nProvider`). */
export function useAccountWorkspacesCopy(): AccountWorkspacesCopy {
  return accountWorkspacesCopy(useLocale().locale);
}
