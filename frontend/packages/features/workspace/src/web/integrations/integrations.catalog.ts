import { defineCatalog } from "@notrelix/i18n";
import { en, vi } from "./integrations.copy";

/** The `workspace-integrations` catalog (completeness checked by `pnpm check:i18n`). */
export const workspaceIntegrationsCatalog = defineCatalog(
  "workspace-integrations",
  {
    vi,
    en,
  },
);
