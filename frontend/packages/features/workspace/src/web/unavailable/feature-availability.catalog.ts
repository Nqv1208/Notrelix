import { defineCatalog } from "@notrelix/i18n";
import { en, vi } from "./feature-availability.copy";

/** The `workspace-feature-availability` catalog (completeness checked by `pnpm check:i18n`). */
export const workspaceFeatureAvailabilityCatalog = defineCatalog(
  "workspace-feature-availability",
  { vi, en },
);
