import { defineCatalog } from "@notrelix/i18n";
import { en, vi } from "./onboarding.copy";

/** The `workspace-onboarding` catalog (completeness checked by `pnpm check:i18n`). */
export const workspaceOnboardingCatalog = defineCatalog(
  "workspace-onboarding",
  { vi, en },
);
