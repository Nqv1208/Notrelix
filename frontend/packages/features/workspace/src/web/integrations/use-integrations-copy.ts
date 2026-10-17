import { useLocale } from "@notrelix/i18n";
import { integrationsCopy, type IntegrationsCopy } from "./integrations.copy";

/** Integrations copy for the active locale (requires the host `I18nProvider`). */
export function useIntegrationsCopy(): IntegrationsCopy {
  return integrationsCopy(useLocale().locale);
}
