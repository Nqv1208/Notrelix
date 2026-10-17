import { useLocale } from "@notrelix/i18n";
import {
  workspaceLibraryCopy,
  type WorkspaceLibraryCopy,
} from "./workspace-library.copy";

/** Directory copy for the active locale (requires the host `I18nProvider`). */
export function useWorkspaceLibraryCopy(): WorkspaceLibraryCopy {
  const { locale } = useLocale();
  return workspaceLibraryCopy(locale);
}
