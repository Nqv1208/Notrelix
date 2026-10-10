import { defineCatalog } from "@notrelix/i18n";
import { en, vi } from "./workspace-settings.copy";

/** The `workspace-settings` catalog (completeness checked by `pnpm check:i18n`). */
export const workspaceSettingsCatalog = defineCatalog("workspace-settings", {
  vi,
  en,
});
